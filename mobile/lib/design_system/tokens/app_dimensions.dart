import 'package:flutter/widgets.dart';

/// 4-pt spacing scale.
abstract final class AppSpacing {
  static const xxs = 4.0;
  static const xs = 8.0;
  static const sm = 12.0;
  static const md = 16.0;
  static const lg = 20.0;
  static const xl = 24.0;
  static const xxl = 32.0;
  static const xxxl = 48.0;

  /// Horizontal page gutter on phones.
  static const pageGutter = lg;
}

abstract final class AppRadii {
  static const chip = Radius.circular(8);
  static const control = Radius.circular(14);
  static const card = Radius.circular(18);
  static const pill = Radius.circular(999);
}

abstract final class AppSizes {
  /// Minimum touch target (Android 48 dp).
  static const minTouchTarget = 48.0;
  static const buttonHeight = 52.0;
  static const iconMd = 22.0;
  static const progressHeight = 6.0;
}

abstract final class AppShadows {
  static const card = [
    BoxShadow(color: Color(0x0F0F172A), blurRadius: 2, offset: Offset(0, 1)),
    BoxShadow(color: Color(0x0A0F172A), blurRadius: 1, offset: Offset(0, 1)),
  ];
}

abstract final class AppDurations {
  static const fast = Duration(milliseconds: 150);
  static const medium = Duration(milliseconds: 250);
}
