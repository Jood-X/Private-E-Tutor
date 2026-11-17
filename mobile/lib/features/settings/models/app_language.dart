import 'package:flutter/widgets.dart';

/// Languages the app supports. English is the default.
enum AppLanguage {
  english('en', 'English'),
  arabic('ar', 'العربية');

  const AppLanguage(this.code, this.nativeName);

  /// ISO 639-1 code, also sent to the API as Accept-Language.
  final String code;

  /// Shown in the language picker in the language itself, so it is never translated.
  final String nativeName;

  static const AppLanguage fallback = AppLanguage.english;

  Locale get locale => Locale(code);

  TextDirection get textDirection => this == arabic ? TextDirection.rtl : TextDirection.ltr;

  static AppLanguage fromCode(String? code) =>
      values.firstWhere((language) => language.code == code, orElse: () => fallback);
}
