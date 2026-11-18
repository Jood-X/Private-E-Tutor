import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../core/config/app_config.dart';
import '../core/logging/app_bloc_observer.dart';
import '../core/logging/app_logger.dart';
import '../core/logging/error_reporter.dart';
import '../features/auth/controllers/session_cubit.dart';
import '../features/settings/controllers/language_cubit.dart';
import 'app.dart';
import 'dependencies.dart';
import 'routes.dart';

/// Starts the app with logging and global error handlers installed first, so nothing
/// that fails during startup goes unreported.
Future<void> bootstrap() async {
  WidgetsFlutterBinding.ensureInitialized();

  const reporter = DebugErrorReporter();
  AppLogger.configure(reporter: reporter);
  final log = AppLogger('App');

  FlutterError.onError = (details) {
    log.error('Flutter framework error', details.exception, details.stack);
    if (kDebugMode) {
      FlutterError.presentError(details);
    }
  };
  PlatformDispatcher.instance.onError = (error, stackTrace) {
    log.error('Uncaught asynchronous error', error, stackTrace);
    return true;
  };
  Bloc.observer = AppBlocObserver();

  final locator = await configureDependencies(AppConfig.fromEnvironment());
  log.info('Starting');
  final session = locator<SessionCubit>();
  runApp(EtutorApp(languageCubit: locator<LanguageCubit>(), sessionCubit: session, router: buildRouter(locator)));

  // Restore after the first frame so the splash screen is visible while the saved session is checked.
  unawaited(session.restore());
}
