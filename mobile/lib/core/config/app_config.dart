/// Build-time configuration, supplied with `--dart-define` (see docs/setup.md).
class AppConfig {
  const AppConfig({required this.apiBaseUrl});

  /// Reads values passed at build time. The default targets the Android emulator,
  /// where 10.0.2.2 is the host machine's localhost.
  factory AppConfig.fromEnvironment() =>
      const AppConfig(apiBaseUrl: String.fromEnvironment('API_BASE_URL', defaultValue: 'http://10.0.2.2:5258'));

  final String apiBaseUrl;
}
