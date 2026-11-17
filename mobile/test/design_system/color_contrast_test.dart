import 'dart:math' as math;

import 'package:etutor_app/design_system/tokens/app_colors.dart';
import 'package:flutter/painting.dart';
import 'package:flutter_test/flutter_test.dart';

/// WCAG 2.x contrast ratio between two opaque colors.
double _contrast(Color a, Color b) {
  final la = a.computeLuminance();
  final lb = b.computeLuminance();
  return (math.max(la, lb) + 0.05) / (math.min(la, lb) + 0.05);
}

void main() {
  const normalText = 4.5;
  const nonText = 3.0;

  // Every foreground/background pair the components actually use.
  const textPairs = <String, (Color, Color)>{
    'ink on background': (AppColors.ink, AppColors.background),
    'muted text on background': (AppColors.inkMuted, AppColors.background),
    'muted text on surface': (AppColors.inkMuted, AppColors.surface),
    'button label on primary': (AppColors.onPrimary, AppColors.primary),
    'primary text on surface': (AppColors.primary, AppColors.surface),
    'primary text on primary soft': (AppColors.primary, AppColors.primarySoft),
    'ink on accent': (AppColors.ink, AppColors.accent),
    'accent text on surface': (AppColors.accentText, AppColors.surface),
    'accent-soft label': (AppColors.onAccentSoft, AppColors.accentSoft),
    'error-soft label': (AppColors.onErrorSoft, AppColors.errorSoft),
    'success-soft label': (AppColors.onSuccessSoft, AppColors.successSoft),
    'muted on-primary text': (AppColors.onPrimaryMuted, AppColors.primary),
    'disabled button label': (AppColors.inkMuted, AppColors.track),
  };

  for (final entry in textPairs.entries) {
    test('${entry.key} meets AA for normal text', () {
      final (foreground, background) = entry.value;
      expect(_contrast(foreground, background), greaterThanOrEqualTo(normalText));
    });
  }

  test('input borders meet the 3:1 non-text contrast requirement', () {
    expect(_contrast(AppColors.inputBorder, AppColors.surface), greaterThanOrEqualTo(nonText));
  });
}
