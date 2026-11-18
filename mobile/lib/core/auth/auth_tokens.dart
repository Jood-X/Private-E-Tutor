import 'package:equatable/equatable.dart';

/// The token pair from the API. The access token is short-lived (15 min); the refresh token is
/// single-use and replaced on every refresh.
class AuthTokens extends Equatable {
  const AuthTokens({required this.accessToken, required this.refreshToken});

  final String accessToken;
  final String refreshToken;

  @override
  List<Object?> get props => [accessToken, refreshToken];

  /// Never print token values.
  @override
  String toString() => 'AuthTokens(<redacted>)';
}
