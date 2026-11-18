import 'package:etutor_app/core/api/api_result.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_catalog_repository.dart';
import '../../helpers/test_app.dart';

Future<void> _openStudentSignup(WidgetTester tester) async {
  await tester.tap(find.text("I'm a student"));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('loads countries, then curricula, and enables Continue after a choice', (tester) async {
    await TestApp.pump(tester);
    await _openStudentSignup(tester);

    expect(find.text('What are you studying?'), findsOneWidget);
    expect(find.text('Choose your country to see the curricula offered there.'), findsOneWidget);
    final continueButton = find.widgetWithText(FilledButton, 'Continue');
    expect(tester.widget<FilledButton>(continueButton).onPressed, isNull);

    await tester.tap(find.text('Jordan'));
    await tester.pumpAndSettle();
    expect(find.text('Jordanian Tawjihi'), findsOneWidget);
    expect(find.text('Includes streams'), findsOneWidget);

    await tester.tap(find.text('Jordanian Tawjihi'));
    await tester.pumpAndSettle();
    expect(tester.widget<FilledButton>(continueButton).onPressed, isNotNull);
  });

  testWidgets('shows a localized error with a support reference and retries', (tester) async {
    final catalog = FakeCatalogRepository()..countriesResult = const ApiError(serverFailure);
    await TestApp.pump(tester, catalog: catalog);
    await _openStudentSignup(tester);

    expect(find.text('Something went wrong on our side. Please try again in a moment.'), findsOneWidget);
    expect(find.text('Reference: trace-123'), findsOneWidget);

    catalog.countriesResult = const ApiSuccess([jordan]);
    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(find.text('Jordan'), findsOneWidget);
    expect(catalog.countriesCalls, 2);
  });

  testWidgets('renders right-to-left with Arabic text when Arabic is saved', (tester) async {
    await TestApp.pump(tester, savedLanguage: 'ar');

    expect(find.text('تعلّم مع المعلّم المناسب'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('تعلّم مع المعلّم المناسب'))), TextDirection.rtl);
  });

  testWidgets('switching the language in Settings flips the layout direction and reloads names', (tester) async {
    final app = await TestApp.pump(tester);
    await _openStudentSignup(tester);

    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('العربية'));
    await tester.pumpAndSettle();

    expect(find.text('الإعدادات'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('الإعدادات'))), TextDirection.rtl);
    expect(app.catalog.countriesCalls, 2, reason: 'catalog names are reloaded in the new language');
  });
}
