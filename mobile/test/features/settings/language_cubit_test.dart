import 'package:bloc_test/bloc_test.dart';
import 'package:etutor_app/features/settings/controllers/language_cubit.dart';
import 'package:etutor_app/features/settings/data/language_preferences.dart';
import 'package:etutor_app/features/settings/models/app_language.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

Future<LanguagePreferences> _preferences([Map<String, Object> values = const {}]) async {
  SharedPreferences.setMockInitialValues(values);
  return LanguagePreferences(await SharedPreferences.getInstance());
}

void main() {
  test('defaults to English on first launch', () async {
    final cubit = LanguageCubit(await _preferences());

    expect(cubit.state, AppLanguage.english);
    expect(cubit.state.textDirection, TextDirection.ltr);
  });

  test('restores the saved language', () async {
    final cubit = LanguageCubit(await _preferences({'settings.language': 'ar'}));

    expect(cubit.state, AppLanguage.arabic);
    expect(cubit.state.textDirection, TextDirection.rtl);
  });

  test('falls back to English for an unknown saved value', () async {
    final cubit = LanguageCubit(await _preferences({'settings.language': 'xx'}));

    expect(cubit.state, AppLanguage.english);
  });

  late LanguagePreferences preferences;
  blocTest<LanguageCubit, AppLanguage>(
    'selecting Arabic emits it and persists it',
    setUp: () async => preferences = await _preferences(),
    build: () => LanguageCubit(preferences),
    act: (cubit) => cubit.select(AppLanguage.arabic),
    expect: () => [AppLanguage.arabic],
    verify: (_) => expect(preferences.read(), AppLanguage.arabic),
  );

  blocTest<LanguageCubit, AppLanguage>(
    'selecting the current language emits nothing',
    setUp: () async => preferences = await _preferences(),
    build: () => LanguageCubit(preferences),
    act: (cubit) => cubit.select(AppLanguage.english),
    expect: () => <AppLanguage>[],
  );
}
