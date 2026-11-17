import '../../l10n/generated/app_localizations.dart';
import '../api/api_failure.dart';

/// Turns an [ApiFailure] into a message the user can act on, in their language.
/// Known server error codes win; otherwise the failure kind decides. Raw server
/// messages are never shown to users.
extension FailureMessages on ApiFailure {
  String localizedMessage(AppLocalizations l10n) {
    final byCode = switch (code) {
      'catalog.country_not_found' => l10n.errorCountryNotFound,
      'catalog.curriculum_not_found' => l10n.errorCurriculumNotFound,
      _ => null,
    };
    if (byCode != null) {
      return byCode;
    }

    return switch (kind) {
      ApiFailureKind.network => l10n.errorNetwork,
      ApiFailureKind.timeout => l10n.errorTimeout,
      ApiFailureKind.rateLimited => l10n.errorRateLimited,
      ApiFailureKind.notFound => l10n.errorNotFound,
      ApiFailureKind.server || ApiFailureKind.invalidResponse => l10n.errorServer,
      _ => l10n.errorUnexpected,
    };
  }

  /// "Reference: `traceId`" when the server identified the request, otherwise `null`.
  String? localizedReference(AppLocalizations l10n) => traceId == null ? null : l10n.commonErrorReference(traceId!);
}
