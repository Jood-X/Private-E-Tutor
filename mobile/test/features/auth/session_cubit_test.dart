import 'package:bloc_test/bloc_test.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/core/auth/token_store.dart';
import 'package:etutor_app/features/auth/controllers/session_cubit.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_auth_repository.dart';
import '../../helpers/fake_catalog_repository.dart' show networkFailure;

void main() {
  late FakeAuthRepository repository;
  late InMemoryTokenStore store;

  setUp(() {
    repository = FakeAuthRepository();
    store = InMemoryTokenStore();
  });

  blocTest<SessionCubit, SessionState>(
    'without saved tokens, restore signs out',
    build: () => SessionCubit(repository: repository, tokenStore: store),
    act: (cubit) => cubit.restore(),
    expect: () => const [SessionState.restoring(), SessionState.signedOut()],
  );

  blocTest<SessionCubit, SessionState>(
    'with saved tokens, restore refreshes them and signs in',
    setUp: () => store.save(tokensA),
    build: () => SessionCubit(repository: repository, tokenStore: store),
    act: (cubit) => cubit.restore(),
    expect: () => const [SessionState.restoring(), SessionState.signedIn(studentUser)],
    verify: (_) => expect(store.current, tokensB, reason: 'rotated tokens are saved'),
  );

  blocTest<SessionCubit, SessionState>(
    'a rejected saved session is cleared',
    setUp: () async {
      await store.save(tokensA);
      repository.refreshResult = const ApiError(sessionRejected);
    },
    build: () => SessionCubit(repository: repository, tokenStore: store),
    act: (cubit) => cubit.restore(),
    expect: () => const [SessionState.restoring(), SessionState.signedOut()],
    verify: (_) => expect(store.current, isNull),
  );

  blocTest<SessionCubit, SessionState>(
    'when the server is unreachable, the saved session is kept and reported as unavailable',
    setUp: () async {
      await store.save(tokensA);
      repository.refreshResult = const ApiError(networkFailure);
    },
    build: () => SessionCubit(repository: repository, tokenStore: store),
    act: (cubit) => cubit.restore(),
    expect: () => const [SessionState.restoring(), SessionState.unavailable(networkFailure)],
    verify: (_) => expect(store.current, tokensA),
  );

  blocTest<SessionCubit, SessionState>(
    'sign-out clears tokens locally and revokes the refresh token on the server',
    setUp: () => store.save(tokensA),
    build: () => SessionCubit(repository: repository, tokenStore: store),
    act: (cubit) => cubit.signOut(),
    expect: () => const [SessionState.signedOut()],
    verify: (_) {
      expect(store.current, isNull);
      expect(repository.signedOutTokens, [tokensA.refreshToken]);
    },
  );
}
