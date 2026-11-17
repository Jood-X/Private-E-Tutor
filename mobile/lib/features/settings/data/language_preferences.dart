import 'package:shared_preferences/shared_preferences.dart';

import '../models/app_language.dart';

/// Persists the chosen language on the device. Not sensitive, so plain preferences are fine.
class LanguagePreferences {
  LanguagePreferences(this._preferences);

  static const _key = 'settings.language';

  final SharedPreferences _preferences;

  AppLanguage read() => AppLanguage.fromCode(_preferences.getString(_key));

  Future<void> write(AppLanguage language) => _preferences.setString(_key, language.code);
}
