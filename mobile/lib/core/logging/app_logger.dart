import 'package:flutter/foundation.dart';
import 'package:logging/logging.dart';

import 'error_reporter.dart';

/// Thin facade over `package:logging` so the whole app logs the same way.
///
/// Rules (docs/logging.md): never log tokens, passwords, request bodies or personal data.
/// Release builds keep warnings and above only.
class AppLogger {
  AppLogger(String name) : _logger = Logger(name);

  final Logger _logger;

  void debug(String message) => _logger.fine(message);

  void info(String message) => _logger.info(message);

  void warning(String message, [Object? error, StackTrace? stackTrace]) => _logger.warning(message, error, stackTrace);

  void error(String message, [Object? error, StackTrace? stackTrace]) => _logger.severe(message, error, stackTrace);

  /// Routes all log records to the console (debug) and errors to the [ErrorReporter].
  static void configure({required ErrorReporter reporter}) {
    Logger.root.level = kReleaseMode ? Level.WARNING : Level.ALL;
    Logger.root.onRecord.listen((record) {
      if (!kReleaseMode) {
        debugPrint(
          '[${record.level.name}] ${record.loggerName}: ${record.message}'
          '${record.error == null ? '' : ' | ${record.error}'}',
        );
      }
      if (record.level >= Level.SEVERE) {
        reporter.report(record.error ?? record.message, record.stackTrace, context: record.loggerName);
      }
    });
  }
}
