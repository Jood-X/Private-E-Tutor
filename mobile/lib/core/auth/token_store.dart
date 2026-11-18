import 'package:flutter_secure_storage/flutter_secure_storage.dart';

import 'auth_tokens.dart';

/// Where tokens live between app launches. The in-memory copy avoids a keystore read per request.
abstract interface class TokenStore {
  AuthTokens? get current;

  Future<AuthTokens?> load();

  Future<void> save(AuthTokens tokens);

  Future<void> clear();
}

/// Stores tokens in the Android Keystore / iOS Keychain via flutter_secure_storage.
/// Tokens are never written to shared preferences, files or logs.
class SecureTokenStore implements TokenStore {
  SecureTokenStore([FlutterSecureStorage? storage]) : _storage = storage ?? const FlutterSecureStorage();

  static const _accessKey = 'auth.access_token';
  static const _refreshKey = 'auth.refresh_token';

  final FlutterSecureStorage _storage;
  AuthTokens? _current;

  @override
  AuthTokens? get current => _current;

  @override
  Future<AuthTokens?> load() async {
    final access = await _storage.read(key: _accessKey);
    final refresh = await _storage.read(key: _refreshKey);
    _current = access != null && refresh != null ? AuthTokens(accessToken: access, refreshToken: refresh) : null;
    return _current;
  }

  @override
  Future<void> save(AuthTokens tokens) async {
    _current = tokens;
    await _storage.write(key: _accessKey, value: tokens.accessToken);
    await _storage.write(key: _refreshKey, value: tokens.refreshToken);
  }

  @override
  Future<void> clear() async {
    _current = null;
    await _storage.delete(key: _accessKey);
    await _storage.delete(key: _refreshKey);
  }
}

/// Non-persistent store for tests.
class InMemoryTokenStore implements TokenStore {
  AuthTokens? _current;

  @override
  AuthTokens? get current => _current;

  @override
  Future<AuthTokens?> load() async => _current;

  @override
  Future<void> save(AuthTokens tokens) async => _current = tokens;

  @override
  Future<void> clear() async => _current = null;
}
