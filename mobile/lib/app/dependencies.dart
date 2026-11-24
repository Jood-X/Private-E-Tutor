import 'package:get_it/get_it.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/api/api_client.dart';
import '../core/api/api_failure.dart';
import '../core/api/api_result.dart';
import '../core/api/dio_factory.dart';
import '../core/auth/auth_interceptor.dart';
import '../core/auth/token_store.dart';
import '../core/config/app_config.dart';
import '../features/auth/controllers/session_cubit.dart';
import '../features/auth/controllers/sign_in_cubit.dart';
import '../features/auth/controllers/student_signup_cubit.dart';
import '../features/auth/controllers/study_details_cubit.dart';
import '../features/auth/controllers/teacher_signup_cubit.dart';
import '../features/auth/data/auth_api_repository.dart';
import '../features/auth/models/auth_repository.dart';
import '../features/content/models/content_models.dart';
import '../features/learning/controllers/discover_cubit.dart';
import '../features/learning/controllers/learning_cubits.dart';
import '../features/learning/data/learning_api_repository.dart';
import '../features/learning/models/learning_models.dart';
import '../features/onboarding/controllers/curriculum_selection_cubit.dart';
import '../features/onboarding/data/catalog_api_repository.dart';
import '../features/onboarding/models/catalog_models.dart';
import '../features/onboarding/models/catalog_repository.dart';
import '../features/settings/controllers/language_cubit.dart';
import '../features/settings/data/language_preferences.dart';
import '../features/teaching/controllers/create_course_cubit.dart';
import '../features/teaching/controllers/enrollment_requests_cubit.dart';
import '../features/teaching/controllers/teacher_courses_cubit.dart';
import '../features/teaching/data/teaching_api_repository.dart';
import '../features/teaching/models/teaching_models.dart';

/// Composition root: the only place that knows concrete implementations.
///
/// Two HTTP clients exist on purpose: `authClient` (no auth interceptor) for sign-in, registration and
/// refresh, and the main `ApiClient` which attaches tokens and refreshes them on 401.
Future<GetIt> configureDependencies(AppConfig config, {GetIt? locator, TokenStore? tokenStore}) async {
  final getIt = locator ?? GetIt.instance;
  final preferences = await SharedPreferences.getInstance();
  String languageCode() => getIt<LanguageCubit>().state.code;

  final authDio = DioFactory.create(config, languageCode: languageCode);
  final authRepository = AuthApiRepository(ApiClient(authDio));
  final store = tokenStore ?? SecureTokenStore();

  final mainDio = DioFactory.create(config, languageCode: languageCode);
  mainDio.interceptors.insert(
    0,
    AuthInterceptor(
      tokenStore: store,
      retryClient: mainDio,
      refresh: (refreshToken) => _refresh(authRepository, refreshToken),
      onSessionExpired: () => getIt<SessionCubit>().sessionExpired(),
    ),
  );

  getIt
    ..registerSingleton(config)
    ..registerSingleton(LanguagePreferences(preferences))
    ..registerSingleton(LanguageCubit(getIt<LanguagePreferences>()))
    ..registerSingleton<TokenStore>(store)
    ..registerSingleton<AuthRepository>(authRepository)
    ..registerSingleton(SessionCubit(repository: authRepository, tokenStore: store))
    ..registerSingleton(ApiClient(mainDio))
    ..registerLazySingleton<CatalogRepository>(() => CatalogApiRepository(getIt<ApiClient>()))
    // Screen controllers are created per screen and closed by their BlocProvider.
    ..registerFactory(() => CurriculumSelectionCubit(getIt<CatalogRepository>()))
    ..registerFactory(() => SignInCubit(repository: getIt(), session: getIt()))
    ..registerFactory(() => StudentSignupCubit(repository: getIt(), session: getIt()))
    ..registerFactory(() => TeacherSignupCubit(repository: getIt(), catalog: getIt(), session: getIt()))
    ..registerFactoryParam<StudyDetailsCubit, Curriculum, void>(
      (curriculum, _) => StudyDetailsCubit(repository: getIt(), curriculum: curriculum),
    );
  registerContentControllers(
    getIt,
    teaching: TeachingApiRepository(getIt<ApiClient>()),
    learning: LearningApiRepository(getIt<ApiClient>()),
  );

  return getIt;
}

/// Teacher and student screen controllers. Shared with the widget-test harness, which passes fakes.
void registerContentControllers(
  GetIt getIt, {
  required TeachingRepository teaching,
  required LearningRepository learning,
}) {
  getIt
    ..registerSingleton<TeachingRepository>(teaching)
    ..registerSingleton<LearningRepository>(learning)
    ..registerFactory(() => TeacherCoursesCubit(teaching))
    ..registerFactoryParam<CreateCourseCubit, String, void>(
      (countryCode, _) => CreateCourseCubit(catalog: getIt(), teaching: teaching, countryCode: countryCode),
    )
    ..registerFactoryParam<CourseEditorCubit, String, void>(
      (courseId, _) => CourseEditorCubit(repository: teaching, courseId: courseId),
    )
    ..registerFactoryParam<LessonEditorCubit, String, void>(
      (lessonId, _) => LessonEditorCubit(repository: teaching, lessonId: lessonId),
    )
    ..registerFactoryParam<EnrollmentListCubit, EnrollmentStatus, void>(
      (status, _) => EnrollmentListCubit(repository: teaching, status: status),
    )
    ..registerFactory(() => MyLearningCubit(learning))
    ..registerFactoryParam<DiscoverCubit, int?, void>(
      (gradeLevelId, _) => DiscoverCubit(learning: learning, catalog: getIt(), gradeLevelId: gradeLevelId),
    )
    ..registerFactoryParam<TeacherProfileCubit, String, void>(
      (teacherId, _) => TeacherProfileCubit(repository: learning, teacherId: teacherId),
    )
    ..registerFactoryParam<StudentCourseCubit, String, void>(
      (courseId, _) => StudentCourseCubit(repository: learning, courseId: courseId),
    )
    ..registerFactoryParam<StudentLessonCubit, String, void>(
      (lessonId, _) => StudentLessonCubit(repository: learning, lessonId: lessonId),
    );
}

Future<RefreshOutcome> _refresh(AuthRepository repository, String refreshToken) async {
  final result = await repository.refresh(refreshToken);
  return switch (result) {
    ApiSuccess(:final value) => Refreshed(value.tokens),
    ApiError(:final failure) when failure.kind == ApiFailureKind.unauthorized => const RefreshRejected(),
    ApiError() => const RefreshUnavailable(),
  };
}
