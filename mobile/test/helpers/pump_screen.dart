import 'package:etutor_app/design_system/theme/app_theme.dart';
import 'package:etutor_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

/// Pumps one screen with the app's theme and localizations (English), without the router.
Future<void> pumpScreen(WidgetTester tester, Widget screen) async {
  await tester.pumpWidget(
    MaterialApp(
      locale: const Locale('en'),
      supportedLocales: AppLocalizations.supportedLocales,
      localizationsDelegates: AppLocalizations.localizationsDelegates,
      theme: AppTheme.light(const Locale('en')),
      home: screen,
    ),
  );
  await tester.pumpAndSettle();
}
