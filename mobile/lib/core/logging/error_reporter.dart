import 'package:flutter/foundation.dart';

/// Destination for unexpected errors. The MVP keeps reports local; a crash-reporting
/// service (e.g. Crashlytics or Sentry) can implement this later without touching callers.
abstract interface class ErrorReporter {
  void report(Object error, StackTrace? stackTrace, {String? context});
}

class DebugErrorReporter implements ErrorReporter {
  const DebugErrorReporter();

  @override
  void report(Object error, StackTrace? stackTrace, {String? context}) {
    if (kReleaseMode) {
      return;
    }
    debugPrint('ERROR${context == null ? '' : ' [$context]'}: $error');
    if (stackTrace != null) {
      debugPrintStack(stackTrace: stackTrace, maxFrames: 12);
    }
  }
}
