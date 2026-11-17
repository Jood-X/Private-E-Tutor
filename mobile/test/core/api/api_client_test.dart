import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:etutor_app/core/api/api_client.dart';
import 'package:etutor_app/core/api/api_failure.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:flutter_test/flutter_test.dart';

typedef _Handler = Future<ResponseBody> Function(RequestOptions options);

class _FakeAdapter implements HttpClientAdapter {
  _FakeAdapter(this.handler);

  final _Handler handler;

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) =>
      handler(options);

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object body, int status, {String traceId = 'trace-1'}) => ResponseBody.fromString(
  jsonEncode(body),
  status,
  headers: {
    Headers.contentTypeHeader: [Headers.jsonContentType],
    ApiClient.traceIdHeader: [traceId],
  },
);

ApiClient _client(_Handler handler) {
  final dio = Dio(BaseOptions(baseUrl: 'http://api.test'))..httpClientAdapter = _FakeAdapter(handler);
  return ApiClient(dio);
}

List<String> _decodeNames(Object? data) =>
    (data! as List<Object?>).cast<Map<String, dynamic>>().map((item) => item['name'] as String).toList();

void main() {
  group('ApiClient', () {
    test('returns decoded data for a successful envelope', () async {
      final client = _client(
        (_) async => _json({
          'success': true,
          'message': null,
          'data': [
            {'name': 'Jordan'},
          ],
          'errors': null,
          'traceId': 't',
        }, 200),
      );

      final result = await client.get('/countries', _decodeNames);

      expect(result, isA<ApiSuccess<List<String>>>());
      expect((result as ApiSuccess<List<String>>).value, ['Jordan']);
    });

    test('maps an error envelope to a typed failure with code and trace id', () async {
      final client = _client(
        (_) async => _json({
          'success': false,
          'message': 'Country not found.',
          'data': null,
          'errors': [
            {'code': 'catalog.country_not_found', 'message': 'Country not found.', 'field': null},
          ],
          'traceId': 'abc123',
        }, 404),
      );

      final result = await client.get('/countries/ZZ/curricula', _decodeNames);

      final failure = (result as ApiError<List<String>>).failure;
      expect(failure.kind, ApiFailureKind.notFound);
      expect(failure.code, 'catalog.country_not_found');
      expect(failure.traceId, 'abc123');
      expect(failure.statusCode, 404);
    });

    test('keeps field errors for validation failures', () async {
      final client = _client(
        (_) async => _json({
          'success': false,
          'message': 'Invalid',
          'data': null,
          'errors': [
            {'code': 'common.validation_failed', 'message': 'Required', 'field': 'name'},
          ],
          'traceId': 't',
        }, 400),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.validation);
      expect(failure.errors.single.field, 'name');
    });

    test('falls back to the trace header when a 5xx body is not an envelope', () async {
      final client = _client(
        (_) async => ResponseBody.fromString(
          '<html>Bad gateway</html>',
          502,
          headers: {
            ApiClient.traceIdHeader: ['from-header'],
          },
        ),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.server);
      expect(failure.traceId, 'from-header');
      expect(failure.kind.isRetryable, isTrue);
    });

    test('reports network errors as retryable network failures', () async {
      final client = _client(
        (options) async => throw DioException.connectionError(requestOptions: options, reason: 'offline'),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.network);
      expect(failure.kind.isRetryable, isTrue);
    });

    test('reports timeouts as timeout failures', () async {
      final client = _client(
        (options) async =>
            throw DioException.receiveTimeout(timeout: const Duration(seconds: 1), requestOptions: options),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.timeout);
    });

    test('reports a connect timeout as an unreachable server, not a slow one', () async {
      // Regression: on the Android emulator a stopped API makes connections hang until the
      // connect timeout; users were told "the server is taking too long" instead of "can't reach".
      final client = _client(
        (options) async =>
            throw DioException.connectionTimeout(timeout: const Duration(seconds: 10), requestOptions: options),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.network);
      expect(failure.kind.isRetryable, isTrue);
    });

    test('treats data that does not match the expected shape as an invalid response', () async {
      final client = _client(
        (_) async =>
            _json({'success': true, 'message': null, 'data': 'not a list', 'errors': null, 'traceId': 't'}, 200),
      );

      final failure = ((await client.get('/x', _decodeNames)) as ApiError<List<String>>).failure;

      expect(failure.kind, ApiFailureKind.invalidResponse);
    });
  });

  group('ApiFailureKind.fromStatusCode', () {
    test('maps the documented status codes', () {
      expect(ApiFailureKind.fromStatusCode(400), ApiFailureKind.validation);
      expect(ApiFailureKind.fromStatusCode(401), ApiFailureKind.unauthorized);
      expect(ApiFailureKind.fromStatusCode(403), ApiFailureKind.forbidden);
      expect(ApiFailureKind.fromStatusCode(404), ApiFailureKind.notFound);
      expect(ApiFailureKind.fromStatusCode(409), ApiFailureKind.conflict);
      expect(ApiFailureKind.fromStatusCode(422), ApiFailureKind.businessRule);
      expect(ApiFailureKind.fromStatusCode(429), ApiFailureKind.rateLimited);
      expect(ApiFailureKind.fromStatusCode(503), ApiFailureKind.server);
    });
  });
}
