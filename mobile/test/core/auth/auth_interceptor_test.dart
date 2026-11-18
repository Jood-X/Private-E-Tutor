import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:etutor_app/core/auth/auth_interceptor.dart';
import 'package:etutor_app/core/auth/auth_tokens.dart';
import 'package:etutor_app/core/auth/token_store.dart';
import 'package:flutter_test/flutter_test.dart';

const _old = AuthTokens(accessToken: 'old-access', refreshToken: 'old-refresh');
const _fresh = AuthTokens(accessToken: 'new-access', refreshToken: 'new-refresh');

/// Fake server: 401 for the old access token, 200 for the new one; records Authorization headers.
class _Server implements HttpClientAdapter {
  final List<String?> authorizations = [];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    final authorization = options.headers['Authorization'] as String?;
    authorizations.add(authorization);
    final ok = authorization == 'Bearer ${_fresh.accessToken}';
    return ResponseBody.fromString(
      jsonEncode({'success': ok, 'data': ok ? 'ok' : null, 'errors': null, 'traceId': 't'}),
      ok ? 200 : 401,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }

  @override
  void close({bool force = false}) {}
}

class _Harness {
  _Harness(RefreshOutcome outcome) {
    dio = Dio(BaseOptions(baseUrl: 'http://api.test'))..httpClientAdapter = server;
    dio.interceptors.add(
      AuthInterceptor(
        tokenStore: store,
        retryClient: dio,
        refresh: (token) async {
          refreshCalls.add(token);
          await Future<void>.delayed(const Duration(milliseconds: 10)); // let concurrent 401s pile up
          return outcome;
        },
        onSessionExpired: () => expiredCalls++,
      ),
    );
  }

  final server = _Server();
  final store = InMemoryTokenStore();
  late final Dio dio;
  final List<String> refreshCalls = [];
  int expiredCalls = 0;
}

void main() {
  test('refreshes once for concurrent 401s, then retries each request with the new token', () async {
    final harness = _Harness(const Refreshed(_fresh));
    await harness.store.save(_old);

    final responses = await Future.wait([
      harness.dio.get<Object?>('/api/v1/me'),
      harness.dio.get<Object?>('/api/v1/me'),
      harness.dio.get<Object?>('/api/v1/me'),
    ]);

    expect(responses.map((response) => response.statusCode), everyElement(200));
    expect(harness.refreshCalls, [_old.refreshToken], reason: 'single-use refresh token must be used once');
    expect(harness.store.current, _fresh);
  });

  test('a rejected refresh clears the session and reports it', () async {
    final harness = _Harness(const RefreshRejected());
    await harness.store.save(_old);

    await expectLater(harness.dio.get<Object?>('/api/v1/me'), throwsA(isA<DioException>()));

    expect(harness.store.current, isNull);
    expect(harness.expiredCalls, 1);
  });

  test('an unreachable refresh keeps the session (only the request fails)', () async {
    final harness = _Harness(const RefreshUnavailable());
    await harness.store.save(_old);

    await expectLater(harness.dio.get<Object?>('/api/v1/me'), throwsA(isA<DioException>()));

    expect(harness.store.current, _old);
    expect(harness.expiredCalls, 0);
  });

  test('auth endpoints never carry a token and never trigger a refresh', () async {
    final harness = _Harness(const Refreshed(_fresh));
    await harness.store.save(_old);

    await expectLater(harness.dio.post<Object?>('/api/v1/auth/login'), throwsA(isA<DioException>()));

    expect(harness.server.authorizations, [null]);
    expect(harness.refreshCalls, isEmpty);
  });
}
