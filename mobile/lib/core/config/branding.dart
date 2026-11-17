/// User-visible product identity. The name is provisional; change it here (or with
/// `--dart-define=PRODUCT_NAME=...`) and nowhere else.
abstract final class Branding {
  static const String productName = String.fromEnvironment('PRODUCT_NAME', defaultValue: 'E-Tutor');
}
