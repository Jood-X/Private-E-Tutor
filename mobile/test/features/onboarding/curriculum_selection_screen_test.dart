import 'package:etutor_app/app/app.dart';
import 'package:etutor_app/app/routes.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/onboarding/controllers/curriculum_selection_cubit.dart';
import 'package:etutor_app/features/settings/controllers/language_cubit.dart';
import 'package:etutor_app/features/settings/data/language_preferences.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:get_it/get_it.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../helpers/fake_catalog_repository.dart';

/// Pumps the real app (router, theme, localization) with a fake catalog.
Future<FakeCatalogRepository> _pumpApp(WidgetTester tester, {String? savedLanguage}) async {
  SharedPreferences.setMockInitialValues({if (savedLanguage != null) 'settings.language': savedLanguage});
  final preferences = LanguagePreferences(await SharedPreferences.getInstance());
  final repository = FakeCatalogRepository();
  final locator = GetIt.asNewInstance()..registerFactory(() => CurriculumSelectionCubit(repository));

  await tester.pumpWidget(EtutorApp(languageCubit: LanguageCubit(preferences), router: buildRouter(locator)));
  await tester.pumpAndSettle();
  return repository;
}

void main() {
  testWidgets('loads countries, then curricula, and enables Continue after a choice', (tester) async {
    await _pumpApp(tester);

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
    final repository = FakeCatalogRepository()..countriesResult = const ApiError(serverFailure);
    SharedPreferences.setMockInitialValues({});
    final preferences = LanguagePreferences(await SharedPreferences.getInstance());
    final locator = GetIt.asNewInstance()..registerFactory(() => CurriculumSelectionCubit(repository));
    await tester.pumpWidget(EtutorApp(languageCubit: LanguageCubit(preferences), router: buildRouter(locator)));
    await tester.pumpAndSettle();

    expect(find.text('Something went wrong on our side. Please try again in a moment.'), findsOneWidget);
    expect(find.text('Reference: trace-123'), findsOneWidget);

    repository.countriesResult = const ApiSuccess([jordan]);
    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(find.text('Jordan'), findsOneWidget);
    expect(repository.countriesCalls, 2);
  });

  testWidgets('renders right-to-left with Arabic text when Arabic is saved', (tester) async {
    await _pumpApp(tester, savedLanguage: 'ar');

    expect(find.text('ماذا تدرس؟'), findsOneWidget);
    final direction = Directionality.of(tester.element(find.text('ماذا تدرس؟')));
    expect(direction, TextDirection.rtl);
  });

  testWidgets('switching the language in Settings flips the layout direction and reloads names', (tester) async {
    final repository = await _pumpApp(tester);

    await tester.tap(find.byTooltip('Settings'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('العربية'));
    await tester.pumpAndSettle();

    expect(find.text('الإعدادات'), findsOneWidget);
    expect(Directionality.of(tester.element(find.text('الإعدادات'))), TextDirection.rtl);
    expect(repository.countriesCalls, 2, reason: 'catalog names are reloaded in the new language');
  });
}
