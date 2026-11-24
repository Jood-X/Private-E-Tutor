import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/controllers/session_cubit.dart';
import '../features/auth/controllers/sign_in_cubit.dart';
import '../features/auth/controllers/student_signup_cubit.dart';
import '../features/auth/controllers/study_details_cubit.dart';
import '../features/auth/controllers/teacher_signup_cubit.dart';
import '../features/auth/models/app_user.dart';
import '../features/auth/views/create_account_screen.dart';
import '../features/auth/views/sign_in_screen.dart';
import '../features/auth/views/splash_screen.dart';
import '../features/auth/views/study_details_screen.dart';
import '../features/auth/views/teacher_signup_screen.dart';
import '../features/auth/views/welcome_screen.dart';
import '../features/content/models/content_models.dart';
import '../features/learning/controllers/discover_cubit.dart';
import '../features/learning/controllers/learning_cubits.dart';
import '../features/learning/views/discover_screen.dart';
import '../features/learning/views/my_learning_screen.dart';
import '../features/learning/views/student_course_screen.dart';
import '../features/learning/views/student_lesson_screen.dart';
import '../features/learning/views/teacher_profile_screen.dart';
import '../features/learning/views/video_player_screen.dart';
import '../features/onboarding/controllers/curriculum_selection_cubit.dart';
import '../features/onboarding/models/catalog_models.dart';
import '../features/onboarding/views/curriculum_selection_screen.dart';
import '../features/profile/views/profile_screen.dart';
import '../features/settings/views/settings_screen.dart';
import '../features/teaching/controllers/create_course_cubit.dart';
import '../features/teaching/controllers/enrollment_requests_cubit.dart';
import '../features/teaching/controllers/teacher_courses_cubit.dart';
import '../features/teaching/views/course_editor_screen.dart';
import '../features/teaching/views/create_course_screen.dart';
import '../features/teaching/views/enrollment_requests_screen.dart';
import '../features/teaching/views/lesson_editor_screen.dart';
import '../features/teaching/views/teacher_courses_screen.dart';
import '../l10n/generated/app_localizations.dart';
import 'role_shell.dart';

/// Route paths. Every key screen is deep-linkable by path. Student screens live under `/s`, teacher
/// screens under `/t`; each role only reaches its own (see [redirectFor]).
abstract final class AppRoutes {
  static const splash = '/splash';
  static const welcome = '/welcome';
  static const signIn = '/sign-in';
  static const studentSignup = '/signup/student';
  static const studentDetails = '/signup/student/details';
  static const studentAccount = '/signup/student/account';
  static const teacherSignup = '/signup/teacher';
  static const settings = '/settings';

  // Student
  static const learning = '/s/learning';
  static const discover = '/s/discover';
  static const studentProfile = '/s/profile';
  static String teacherProfile(String teacherId) => '/s/teachers/$teacherId';
  static String studentCourse(String courseId) => '/s/courses/$courseId';
  static String studentLesson(String lessonId) => '/s/lessons/$lessonId';
  static String studentVideo(String resourceId) => '/s/videos/$resourceId';

  // Teacher
  static const teacherCourses = '/t/courses';
  static const newCourse = '/t/courses/new';
  static const requests = '/t/requests';
  static const teacherAccount = '/t/profile';
  static String courseEditor(String courseId) => '/t/courses/$courseId';
  static String lessonEditor(String lessonId) => '/t/lessons/$lessonId';

  /// Where a signed-in user starts.
  static String homeFor(UserRole? role) => role == UserRole.teacher ? teacherCourses : learning;

  static bool isSignedOutOnly(String location) =>
      location == welcome || location == signIn || location.startsWith('/signup');
}

/// Decides where a user may be, based only on the session state and role (pure, unit-tested).
String? redirectFor(SessionStatus status, String location, {UserRole? role}) {
  switch (status) {
    case SessionStatus.restoring || SessionStatus.unavailable:
      return location == AppRoutes.splash ? null : AppRoutes.splash;
    case SessionStatus.signedOut:
      return AppRoutes.isSignedOutOnly(location) || location == AppRoutes.settings ? null : AppRoutes.welcome;
    case SessionStatus.signedIn:
      final home = AppRoutes.homeFor(role);
      if (AppRoutes.isSignedOutOnly(location) || location == AppRoutes.splash || location == '/') return home;
      // Each role stays in its own area; a wrong-area deep link goes to the user's home.
      final teacher = role == UserRole.teacher;
      if (location.startsWith('/t/') && !teacher) return home;
      if (location.startsWith('/s/') && teacher) return home;
      return null;
  }
}

GoRouter buildRouter(GetIt locator) {
  final session = locator<SessionCubit>();
  return GoRouter(
    initialLocation: AppRoutes.splash,
    refreshListenable: _StreamListenable(session.stream),
    redirect: (_, state) => redirectFor(session.state.status, state.matchedLocation, role: session.state.user?.role),
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
      GoRoute(path: AppRoutes.settings, builder: (_, _) => const SettingsScreen()),
      ..._studentRoutes(locator, session),
      ..._teacherRoutes(locator, session),
    ],
  );
}

List<RouteBase> _studentRoutes(GetIt locator, SessionCubit session) => [
  StatefulShellRoute.indexedStack(
    builder: (context, _, shell) {
      final l10n = AppLocalizations.of(context);
      return RoleShell(
        shell: shell,
        tabs: [
          ShellTab(label: l10n.navLearning, icon: Icons.school_outlined, selectedIcon: Icons.school_rounded),
          ShellTab(label: l10n.navDiscover, icon: Icons.search_rounded, selectedIcon: Icons.manage_search_rounded),
          ShellTab(label: l10n.navProfile, icon: Icons.person_outline_rounded, selectedIcon: Icons.person_rounded),
        ],
      );
    },
    branches: [
      StatefulShellBranch(
        routes: [
          GoRoute(
            path: AppRoutes.learning,
            builder: (_, _) => MyLearningScreen(createController: locator.call<MyLearningCubit>),
          ),
        ],
      ),
      StatefulShellBranch(
        routes: [
          GoRoute(
            path: AppRoutes.discover,
            builder:
                (_, _) => DiscoverScreen(
                  createController: () => locator<DiscoverCubit>(param1: session.state.user?.student?.gradeLevelId),
                ),
          ),
        ],
      ),
      StatefulShellBranch(routes: [GoRoute(path: AppRoutes.studentProfile, builder: (_, _) => const ProfileScreen())]),
    ],
  ),
  GoRoute(
    path: '/s/teachers/:id',
    builder:
        (_, state) => TeacherProfileScreen(
          createController: () => locator<TeacherProfileCubit>(param1: state.pathParameters['id']),
        ),
  ),
  GoRoute(
    path: '/s/courses/:id',
    builder:
        (_, state) => StudentCourseScreen(
          createController: () => locator<StudentCourseCubit>(param1: state.pathParameters['id']),
        ),
  ),
  GoRoute(
    path: '/s/videos/:id',
    builder:
        (_, state) =>
            VideoPlayerScreen(createController: () => locator<VideoPlaybackCubit>(param1: state.pathParameters['id'])),
  ),
  GoRoute(
    path: '/s/lessons/:id',
    builder:
        (_, state) => StudentLessonScreen(
          createController: () => locator<StudentLessonCubit>(param1: state.pathParameters['id']),
        ),
  ),
];

List<RouteBase> _teacherRoutes(GetIt locator, SessionCubit session) => [
  StatefulShellRoute.indexedStack(
    builder: (context, _, shell) {
      final l10n = AppLocalizations.of(context);
      return RoleShell(
        shell: shell,
        tabs: [
          ShellTab(label: l10n.navCourses, icon: Icons.menu_book_outlined, selectedIcon: Icons.menu_book_rounded),
          ShellTab(label: l10n.navRequests, icon: Icons.inbox_outlined, selectedIcon: Icons.inbox_rounded),
          ShellTab(label: l10n.navProfile, icon: Icons.person_outline_rounded, selectedIcon: Icons.person_rounded),
        ],
      );
    },
    branches: [
      StatefulShellBranch(
        routes: [
          GoRoute(
            path: AppRoutes.teacherCourses,
            builder: (_, _) => TeacherCoursesScreen(createController: locator.call<TeacherCoursesCubit>),
          ),
        ],
      ),
      StatefulShellBranch(
        routes: [
          GoRoute(
            path: AppRoutes.requests,
            builder:
                (_, _) => EnrollmentRequestsScreen(
                  createController: (EnrollmentStatus status) => locator<EnrollmentListCubit>(param1: status),
                ),
          ),
        ],
      ),
      StatefulShellBranch(routes: [GoRoute(path: AppRoutes.teacherAccount, builder: (_, _) => const ProfileScreen())]),
    ],
  ),
  // Declared before '/t/courses/:id' so "new" is never read as a course ID.
  GoRoute(
    path: AppRoutes.newCourse,
    builder:
        (_, _) => CreateCourseScreen(
          createController: () => locator<CreateCourseCubit>(param1: session.state.user?.countryCode ?? ''),
        ),
  ),
  GoRoute(
    path: '/t/courses/:id',
    builder:
        (_, state) =>
            CourseEditorScreen(createController: () => locator<CourseEditorCubit>(param1: state.pathParameters['id'])),
  ),
  GoRoute(
    path: '/t/lessons/:id',
    builder:
        (_, state) =>
            LessonEditorScreen(createController: () => locator<LessonEditorCubit>(param1: state.pathParameters['id'])),
  ),
];

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
