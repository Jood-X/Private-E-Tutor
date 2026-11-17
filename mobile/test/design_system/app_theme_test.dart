import 'package:etutor_app/design_system/theme/app_theme.dart';
import 'package:etutor_app/design_system/tokens/app_typography.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  for (final (locale, family) in [
    (const Locale('en'), AppTypography.latinFamily),
    (const Locale('ar'), AppTypography.arabicFamily),
  ]) {
    group('AppTheme.light(${locale.languageCode})', () {
      final theme = AppTheme.light(locale);

      test('every text style uses the brand family', () {
        final styles = [
          theme.textTheme.headlineSmall,
          theme.textTheme.bodyLarge,
          theme.textTheme.labelLarge,
          theme.appBarTheme.titleTextStyle,
          theme.snackBarTheme.contentTextStyle,
          theme.inputDecorationTheme.labelStyle,
        ];
        for (final style in styles) {
          expect(style?.fontFamily, family);
        }
      });

      test('button labels use the brand family (regression: they fell back to Roboto)', () {
        TextStyle? labelOf(ButtonStyle? style) => style?.textStyle?.resolve({});

        expect(labelOf(theme.filledButtonTheme.style)?.fontFamily, family);
        expect(labelOf(theme.outlinedButtonTheme.style)?.fontFamily, family);
        expect(labelOf(theme.textButtonTheme.style)?.fontFamily, family);
      });
    });
  }
}
