import 'package:flutter/material.dart';

import 'app_colors.dart';

/// Type scale from docs/design-direction.md. Arabic uses its own family and ~15% taller lines.
abstract final class AppTypography {
  static const latinFamily = 'Figtree';
  static const arabicFamily = 'IBM Plex Sans Arabic';
  static const _arabicLineHeightFactor = 1.15;

  static TextTheme textTheme({required bool isArabic}) {
    final factor = isArabic ? _arabicLineHeightFactor : 1.0;
    TextStyle style(double size, double lineHeight, FontWeight weight, [Color color = AppColors.ink]) => TextStyle(
      fontSize: size,
      height: (lineHeight / size) * factor,
      fontWeight: weight,
      color: color,
      letterSpacing: 0,
    );

    return TextTheme(
      displaySmall: style(32, 40, FontWeight.w700),
      headlineSmall: style(24, 32, FontWeight.w700),
      titleLarge: style(20, 28, FontWeight.w600),
      titleMedium: style(17, 24, FontWeight.w600),
      titleSmall: style(15, 22, FontWeight.w600),
      bodyLarge: style(16, 24, FontWeight.w400),
      bodyMedium: style(14, 20, FontWeight.w400, AppColors.inkMuted),
      labelLarge: style(16, 20, FontWeight.w600),
      labelMedium: style(13, 16, FontWeight.w600),
      labelSmall: style(12, 16, FontWeight.w600, AppColors.inkMuted),
    );
  }
}
