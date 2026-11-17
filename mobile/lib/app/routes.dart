import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';

import '../features/onboarding/controllers/curriculum_selection_cubit.dart';
import '../features/onboarding/views/curriculum_selection_screen.dart';
import '../features/settings/views/settings_screen.dart';

/// Route paths. Every key screen is deep-linkable by path.
abstract final class AppRoutes {
  static const onboarding = '/';
  static const settings = '/settings';
}

GoRouter buildRouter(GetIt locator) => GoRouter(
  initialLocation: AppRoutes.onboarding,
  routes: [
    GoRoute(
      path: AppRoutes.onboarding,
      builder: (_, __) => CurriculumSelectionScreen(createController: locator.call<CurriculumSelectionCubit>),
    ),
    GoRoute(path: AppRoutes.settings, builder: (_, __) => const SettingsScreen()),
  ],
);
