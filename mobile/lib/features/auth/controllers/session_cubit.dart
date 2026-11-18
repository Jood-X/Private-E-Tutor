import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/auth/token_store.dart';
import '../models/app_user.dart';
import '../models/auth_repository.dart';

enum SessionStatus {
  /// Checking for a saved session at startup.
  restoring,
  signedOut,
  signedIn,

  /// A saved session exists but the server could not be reached to confirm it.
  unavailable,
}

class SessionState extends Equatable {
  const SessionState._(this.status, {this.user, this.failure});

  const SessionState.restoring() : this._(SessionStatus.restoring);

  const SessionState.signedOut() : this._(SessionStatus.signedOut);

  const SessionState.signedIn(AppUser user) : this._(SessionStatus.signedIn, user: user);

  const SessionState.unavailable(ApiFailure failure) : this._(SessionStatus.unavailable, failure: failure);

  final SessionStatus status;
  final AppUser? user;
  final ApiFailure? failure;

  @override
  List<Object?> get props => [status, user, failure];
}

/// App-wide controller for "who is signed in". The router follows its state.
class SessionCubit extends Cubit<SessionState> {
  SessionCubit({required AuthRepository repository, required TokenStore tokenStore})
    : _repository = repository,
      _tokenStore = tokenStore,
      super(const SessionState.restoring());

  final AuthRepository _repository;
  final TokenStore _tokenStore;

  /// Restores a saved session by exchanging its refresh token (which also returns the current profile).
  Future<void> restore() async {
    emit(const SessionState.restoring());
    final saved = await _tokenStore.load();
    if (saved == null) {
      emit(const SessionState.signedOut());
      return;
    }

    final result = await _repository.refresh(saved.refreshToken);
    switch (result) {
      case ApiSuccess(:final value):
        await _tokenStore.save(value.tokens);
        emit(SessionState.signedIn(value.user));
      case ApiError(:final failure) when failure.kind == ApiFailureKind.unauthorized:
        await _tokenStore.clear();
        emit(const SessionState.signedOut());
      case ApiError(:final failure):
        emit(SessionState.unavailable(failure));
    }
  }

  /// Called after a successful sign-in or registration.
  Future<void> startSession(AuthSession session) async {
    await _tokenStore.save(session.tokens);
    emit(SessionState.signedIn(session.user));
  }

  void updateUser(AppUser user) {
    if (state.status == SessionStatus.signedIn) {
      emit(SessionState.signedIn(user));
    }
  }

  Future<void> signOut() async {
    final refreshToken = _tokenStore.current?.refreshToken;
    await _tokenStore.clear();
    emit(const SessionState.signedOut());
    if (refreshToken != null) {
      await _repository.signOut(refreshToken);
    }
  }

  /// The server rejected the session (e.g. the refresh token was revoked). Tokens are already cleared.
  void sessionExpired() {
    if (!isClosed) {
      emit(const SessionState.signedOut());
    }
  }
}
