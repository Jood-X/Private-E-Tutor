import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';
import '../tokens/app_typography.dart';

/// Builds the app theme from design tokens. Material widgets are used as primitives only;
/// their look comes from here, not from Material defaults.
abstract final class AppTheme {
  static ThemeData light(Locale locale) {
    final isArabic = locale.languageCode == 'ar';
    final fontFamily = isArabic ? AppTypography.arabicFamily : AppTypography.latinFamily;
    const fontFallback = [AppTypography.arabicFamily, AppTypography.latinFamily];

    // The family must be baked into the text styles themselves: component themes below
    // (buttons, app bar, inputs, snackbars) receive these styles directly and would
    // otherwise fall back to the platform default font.
    final textTheme = AppTypography.textTheme(
      isArabic: isArabic,
    ).apply(fontFamily: fontFamily, fontFamilyFallback: fontFallback);
    const controlShape = RoundedRectangleBorder(borderRadius: BorderRadius.all(AppRadii.control));

    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,
      fontFamily: fontFamily,
      fontFamilyFallback: fontFallback,
      scaffoldBackgroundColor: AppColors.background,
      colorScheme: const ColorScheme.light(
        primary: AppColors.primary,
        onPrimary: AppColors.onPrimary,
        primaryContainer: AppColors.primarySoft,
        onPrimaryContainer: AppColors.primaryStrong,
        secondary: AppColors.accent,
        onSecondary: AppColors.ink,
        surface: AppColors.surface,
        onSurface: AppColors.ink,
        onSurfaceVariant: AppColors.inkMuted,
        outline: AppColors.inputBorder,
        outlineVariant: AppColors.border,
        error: AppColors.error,
        onError: AppColors.onPrimary,
      ),
      textTheme: textTheme,
      materialTapTargetSize: MaterialTapTargetSize.padded,
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.background,
        foregroundColor: AppColors.ink,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
        titleTextStyle: textTheme.titleMedium,
      ),
      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          minimumSize: const Size.fromHeight(AppSizes.buttonHeight),
          shape: controlShape,
          backgroundColor: AppColors.primary,
          foregroundColor: AppColors.onPrimary,
          disabledBackgroundColor: AppColors.track,
          disabledForegroundColor: AppColors.inkMuted,
          textStyle: textTheme.labelLarge,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          minimumSize: const Size.fromHeight(AppSizes.buttonHeight),
          shape: controlShape,
          foregroundColor: AppColors.primary,
          side: const BorderSide(color: AppColors.primary, width: 1.5),
          textStyle: textTheme.labelLarge,
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          minimumSize: const Size(AppSizes.minTouchTarget, AppSizes.minTouchTarget),
          foregroundColor: AppColors.primary,
          textStyle: textTheme.labelLarge,
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
        border: _inputBorder(AppColors.inputBorder),
        enabledBorder: _inputBorder(AppColors.inputBorder),
        focusedBorder: _inputBorder(AppColors.primary, width: 2),
        errorBorder: _inputBorder(AppColors.error),
        labelStyle: textTheme.titleSmall,
        helperStyle: textTheme.bodyMedium,
      ),
      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: AppColors.ink,
        contentTextStyle: textTheme.bodyLarge?.copyWith(color: AppColors.onPrimary),
        shape: const RoundedRectangleBorder(borderRadius: BorderRadius.all(AppRadii.control)),
      ),
      progressIndicatorTheme: const ProgressIndicatorThemeData(
        color: AppColors.primary,
        linearTrackColor: AppColors.track,
      ),
      dividerTheme: const DividerThemeData(color: AppColors.border, thickness: 1, space: 1),
    );
  }

  static OutlineInputBorder _inputBorder(Color color, {double width = 1.5}) => OutlineInputBorder(
    borderRadius: const BorderRadius.all(AppRadii.control),
    borderSide: BorderSide(color: color, width: width),
  );
}
