import 'package:get_it/get_it.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../core/api/api_client.dart';
import '../core/api/dio_factory.dart';
import '../core/config/app_config.dart';
import '../features/onboarding/controllers/curriculum_selection_cubit.dart';
import '../features/onboarding/data/catalog_api_repository.dart';
import '../features/onboarding/models/catalog_repository.dart';
import '../features/settings/controllers/language_cubit.dart';
import '../features/settings/data/language_preferences.dart';

/// Composition root: the only place that knows concrete implementations.
Future<GetIt> configureDependencies(AppConfig config, {GetIt? locator}) async {
  final getIt = locator ?? GetIt.instance;
  final preferences = await SharedPreferences.getInstance();

  getIt
    ..registerSingleton(config)
    ..registerSingleton(LanguagePreferences(preferences))
    ..registerSingleton(LanguageCubit(getIt<LanguagePreferences>()))
    ..registerLazySingleton(
      () => ApiClient(DioFactory.create(config, languageCode: () => getIt<LanguageCubit>().state.code)),
    )
    ..registerLazySingleton<CatalogRepository>(() => CatalogApiRepository(getIt<ApiClient>()))
    // Screen controllers are created per screen and closed by their BlocProvider.
    ..registerFactory(() => CurriculumSelectionCubit(getIt<CatalogRepository>()));

  return getIt;
}
