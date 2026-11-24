import 'package:etutor_app/app/app.dart';
import 'package:etutor_app/app/dependencies.dart';
import 'package:etutor_app/app/routes.dart';
import 'package:etutor_app/core/auth/token_store.dart';
import 'package:etutor_app/features/auth/controllers/session_cubit.dart';
import 'package:etutor_app/features/auth/controllers/sign_in_cubit.dart';
import 'package:etutor_app/features/auth/controllers/student_signup_cubit.dart';
import 'package:etutor_app/features/auth/controllers/study_details_cubit.dart';
import 'package:etutor_app/features/auth/controllers/teacher_signup_cubit.dart';
import 'package:etutor_app/features/onboarding/controllers/curriculum_selection_cubit.dart';
import 'package:etutor_app/features/onboarding/models/catalog_models.dart';
import 'package:etutor_app/features/onboarding/models/catalog_repository.dart';
import 'package:etutor_app/features/settings/controllers/language_cubit.dart';
import 'package:etutor_app/features/settings/data/language_preferences.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'fake_auth_repository.dart';
import 'fake_catalog_repository.dart';
import 'fake_content_repositories.dart';

/// The real app (router, redirects, theme, localization) wired to in-memory fakes.
class TestApp {
  TestApp._(this.auth, this.catalog, this.teaching, this.learning, this.tokens, this.session);

  final FakeAuthRepository auth;
  final FakeCatalogRepository catalog;
  final FakeTeachingRepository teaching;
  final FakeLearningRepository learning;
  final InMemoryTokenStore tokens;
  final SessionCubit session;

  static Future<TestApp> pump(
    WidgetTester tester, {
    String? savedLanguage,
    FakeAuthRepository? auth,
    FakeCatalogRepository? catalog,
    FakeTeachingRepository? teaching,
    FakeLearningRepository? learning,
    bool restoreSession = true,
  }) async {
    SharedPreferences.setMockInitialValues({if (savedLanguage != null) 'settings.language': savedLanguage});
    final preferences = LanguagePreferences(await SharedPreferences.getInstance());
    final authRepository = auth ?? FakeAuthRepository();
    final catalogRepository = catalog ?? FakeCatalogRepository();
    final teachingRepository = teaching ?? FakeTeachingRepository();
    final learningRepository = learning ?? FakeLearningRepository();
    final tokens = InMemoryTokenStore();
    final session = SessionCubit(repository: authRepository, tokenStore: tokens);

    final locator =
        GetIt.asNewInstance()
          ..registerSingleton(session)
          ..registerSingleton<CatalogRepository>(catalogRepository)
          ..registerFactory(() => CurriculumSelectionCubit(catalogRepository))
          ..registerFactory(() => SignInCubit(repository: authRepository, session: session))
          ..registerFactory(() => StudentSignupCubit(repository: authRepository, session: session))
          ..registerFactory(
            () => TeacherSignupCubit(repository: authRepository, catalog: catalogRepository, session: session),
          )
          ..registerFactoryParam<StudyDetailsCubit, Curriculum, void>(
            (curriculum, _) => StudyDetailsCubit(repository: catalogRepository, curriculum: curriculum),
          );
    registerContentControllers(locator, teaching: teachingRepository, learning: learningRepository);

    await tester.pumpWidget(
      EtutorApp(languageCubit: LanguageCubit(preferences), sessionCubit: session, router: buildRouter(locator)),
    );
    if (restoreSession) {
      await session.restore();
      await tester.pumpAndSettle();
    } else {
      // The splash spinner animates until the session is restored, so the app never "settles".
      await tester.pump();
    }
    return TestApp._(authRepository, catalogRepository, teachingRepository, learningRepository, tokens, session);
  }
}
