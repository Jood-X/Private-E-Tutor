import 'dart:async';

import 'package:dio/dio.dart';

import '../logging/app_logger.dart';
import 'auth_tokens.dart';
import 'token_store.dart';

/// Outcome of trying to exchange a refresh token.
sealed class RefreshOutcome {
  const RefreshOutcome();
}

/// New tokens were issued.
final class Refreshed extends RefreshOutcome {
  const Refreshed(this.tokens);

  final AuthTokens tokens;
}

/// The server rejected the refresh token: the session is over.
final class RefreshRejected extends RefreshOutcome {
  const RefreshRejected();
}

/// The server could not be reached; the session may still be valid, so keep it.
final class RefreshUnavailable extends RefreshOutcome {
  const RefreshUnavailable();
}

typedef TokenRefresher = Future<RefreshOutcome> Function(String refreshToken);

/// Adds the access token to requests and transparently refreshes it once when the API answers 401.
///
/// Refresh tokens are single-use on the server, so concurrent 401s must not each refresh: all of them
/// wait for one shared refresh and then retry with the new token. If refreshing fails, the tokens are
/// cleared and [onSessionExpired] is called so the app can return to sign-in. A refresh that fails only
/// because the network is down keeps the session and fails the original request instead.
class AuthInterceptor extends Interceptor {
  AuthInterceptor({
    required TokenStore tokenStore,
    required TokenRefresher refresh,
    required Dio retryClient,
    required void Function() onSessionExpired,
  }) : _tokenStore = tokenStore,
       _refresh = refresh,
       _retryClient = retryClient,
       _onSessionExpired = onSessionExpired;

  static final _log = AppLogger('Auth');

  /// Requests that must never carry or refresh tokens (they are how tokens are obtained).
  static const _authPathPrefix = '/api/v1/auth/';
  static const _retriedKey = 'auth.retried';

  final TokenStore _tokenStore;
  final TokenRefresher _refresh;
  final Dio _retryClient;
  final void Function() _onSessionExpired;

  Future<RefreshOutcome>? _refreshInFlight;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    final access = _tokenStore.current?.accessToken;
    if (access != null && !_isAuthEndpoint(options)) {
      options.headers['Authorization'] = 'Bearer $access';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    final options = err.requestOptions;
    final unauthorized = err.response?.statusCode == 401;
    final current = _tokenStore.current;
    if (!unauthorized || current == null || _isAuthEndpoint(options) || options.extra[_retriedKey] == true) {
      handler.next(err);
      return;
    }

    final outcome = await _refreshOnce(current.refreshToken);
    if (outcome is! Refreshed) {
      handler.next(err);
      return;
    }

    try {
      options
        ..extra[_retriedKey] = true
        ..headers['Authorization'] = 'Bearer ${outcome.tokens.accessToken}';
      handler.resolve(await _retryClient.fetch<Object?>(options));
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }

  /// All concurrent callers share the same refresh call.
  Future<RefreshOutcome> _refreshOnce(String refreshToken) {
    return _refreshInFlight ??= _doRefresh(refreshToken).whenComplete(() => _refreshInFlight = null);
  }

  Future<RefreshOutcome> _doRefresh(String refreshToken) async {
    final outcome = await _refresh(refreshToken);
    switch (outcome) {
      case Refreshed(:final tokens):
        await _tokenStore.save(tokens);
      case RefreshRejected():
        _log.warning('Session expired; signing out');
        await _tokenStore.clear();
        _onSessionExpired();
      case RefreshUnavailable():
        _log.warning('Could not refresh the session (server unreachable); keeping it');
    }
    return outcome;
  }

  bool _isAuthEndpoint(RequestOptions options) => options.path.startsWith(_authPathPrefix);
}
