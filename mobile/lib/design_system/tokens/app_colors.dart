import 'package:flutter/painting.dart';

/// Color tokens from docs/design-direction.md. Every text/background pair used by the
/// components is contrast-checked in test/design_system/color_contrast_test.dart.
/// Widgets use these tokens (or the theme built from them), never raw hex values.
abstract final class AppColors {
  static const primary = Color(0xFF0F766E);
  static const primaryStrong = Color(0xFF115E59);
  static const primarySoft = Color(0xFFE6F2F0);
  static const onPrimary = Color(0xFFFFFFFF);
  static const onPrimaryMuted = Color(0xFFE6F2F0);

  /// Progress and highlights. Only ink text may sit on it.
  static const accent = Color(0xFFF59E0B);
  static const accentText = Color(0xFFB45309);
  static const accentSoft = Color(0xFFFEF3C7);
  static const onAccentSoft = Color(0xFF92400E);

  static const background = Color(0xFFF4F7F6);
  static const surface = Color(0xFFFFFFFF);
  static const surfaceMuted = Color(0xFFEEF2F1);
  static const ink = Color(0xFF0F172A);
  static const inkMuted = Color(0xFF475569);
  static const border = Color(0xFFD9E1E0);

  /// Boundary of interactive inputs; meets 3:1 non-text contrast on white.
  static const inputBorder = Color(0xFF64748B);
  static const track = Color(0xFFE2E8F0);

  static const success = Color(0xFF15803D);
  static const successSoft = Color(0xFFDCFCE7);
  static const onSuccessSoft = Color(0xFF166534);
  static const error = Color(0xFFDC2626);
  static const errorSoft = Color(0xFFFEE2E2);
  static const onErrorSoft = Color(0xFF991B1B);
}
