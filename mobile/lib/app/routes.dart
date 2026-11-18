import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/controllers/session_cubit.dart';
import '../features/auth/controllers/sign_in_cubit.dart';
import '../features/auth/controllers/student_signup_cubit.dart';
import '../features/auth/controllers/study_details_cubit.dart';
import '../features/auth/controllers/teacher_signup_cubit.dart';
import '../features/auth/views/create_account_screen.dart';
import '../features/auth/views/sign_in_screen.dart';
import '../features/auth/views/splash_screen.dart';
import '../features/auth/views/study_details_screen.dart';
import '../features/auth/views/teacher_signup_screen.dart';
import '../features/auth/views/welcome_screen.dart';
import '../features/home/views/home_screen.dart';
import '../features/onboarding/controllers/curriculum_selection_cubit.dart';
import '../features/onboarding/models/catalog_models.dart';
import '../features/onboarding/views/curriculum_selection_screen.dart';
import '../features/settings/views/settings_screen.dart';

/// Route paths. Every key screen is deep-linkable by path.
abstract final class AppRoutes {
  static const splash = '/splash';
  static const welcome = '/welcome';
  static const signIn = '/sign-in';
  static const studentSignup = '/signup/student';
  static const studentDetails = '/signup/student/details';
  static const studentAccount = '/signup/student/account';
  static const teacherSignup = '/signup/teacher';
  static const home = '/home';
  static const settings = '/settings';

  static bool isSignedOutOnly(String location) =>
      location == welcome || location == signIn || location.startsWith('/signup');
}

/// Decides where a user may be, based only on the session state (pure, unit-tested).
String? redirectFor(SessionStatus status, String location) {
  switch (status) {
    case SessionStatus.restoring || SessionStatus.unavailable:
      return location == AppRoutes.splash ? null : AppRoutes.splash;
    case SessionStatus.signedOut:
      return AppRoutes.isSignedOutOnly(location) || location == AppRoutes.settings ? null : AppRoutes.welcome;
    case SessionStatus.signedIn:
      return AppRoutes.isSignedOutOnly(location) || location == AppRoutes.splash ? AppRoutes.home : null;
  }
}

GoRouter buildRouter(GetIt locator) {
  final session = locator<SessionCubit>();
  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: _StreamListenable(session.stream),
    redirect: (_, state) => redirectFor(session.state.status, state.matchedLocation),
    routes: [
      GoRoute(path: AppRoutes.splash, builder: (_, _) => const SplashScreen()),
      GoRoute(path: AppRoutes.welcome, builder: (_, _) => const WelcomeScreen()),
      GoRoute(path: AppRoutes.signIn, builder: (_, _) => SignInScreen(createController: locator.call<SignInCubit>)),
      GoRoute(
        path: AppRoutes.teacherSignup,
        builder: (_, _) => TeacherSignupScreen(createController: locator.call<TeacherSignupCubit>),
      ),
      // The three student sign-up steps share one flow controller for as long as any of them is open.
      ShellRoute(
        builder: (_, _, child) => BlocProvider(create: (_) => locator<StudentSignupCubit>(), child: child),
        routes: [
          GoRoute(
            path: AppRoutes.studentSignup,
            builder: (_, _) => CurriculumSelectionScreen(createController: locator.call<CurriculumSelectionCubit>),
          ),
          GoRoute(
            path: AppRoutes.studentDetails,
            builder:
                (_, _) => StudyDetailsScreen(
                  createController: (Curriculum curriculum) => locator<StudyDetailsCubit>(param1: curriculum),
                ),
          ),
          GoRoute(path: AppRoutes.studentAccount, builder: (_, _) => const CreateAccountScreen()),
        ],
      ),
      GoRoute(path: AppRoutes.home, builder: (_, _) => const HomeScreen()),
      GoRoute(path: AppRoutes.settings, builder: (_, _) => const SettingsScreen()),
    ],
  );
}

/// Lets the router re-run its redirect whenever the session changes.
class _StreamListenable extends ChangeNotifier {
  _StreamListenable(Stream<Object?> stream) {
    _subscription = stream.listen((_) => notifyListeners());
  }

  late final StreamSubscription<Object?> _subscription;

  @override
  void dispose() {
    unawaited(_subscription.cancel());
    super.dispose();
  }
}
