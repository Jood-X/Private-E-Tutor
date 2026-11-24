import '../../l10n/generated/app_localizations.dart';
import '../api/api_failure.dart';

/// Turns an [ApiFailure] into a message the user can act on, in their language.
/// Known server error codes win; otherwise the failure kind decides. Raw server
/// messages are never shown to users.
extension FailureMessages on ApiFailure {
  String localizedMessage(AppLocalizations l10n) =>
      messageForCode(code, l10n) ??
      switch (kind) {
        ApiFailureKind.network => l10n.errorNetwork,
        ApiFailureKind.timeout => l10n.errorTimeout,
        ApiFailureKind.rateLimited => l10n.errorRateLimited,
        ApiFailureKind.notFound => l10n.errorNotFound,
        ApiFailureKind.server || ApiFailureKind.invalidResponse => l10n.errorServer,
        _ => l10n.errorUnexpected,
      };

  /// "Reference: `traceId`" when the server identified the request, otherwise `null`.
  String? localizedReference(AppLocalizations l10n) => traceId == null ? null : l10n.commonErrorReference(traceId!);

  /// Localized messages for errors attached to one input field, keyed by the field's API name.
  Map<String, String> localizedFieldErrors(AppLocalizations l10n) => {
    for (final error in errors)
      if (error.field case final field? when field.isNotEmpty)
        field: messageForCode(error.code, l10n) ?? l10n.errorUnexpected,
  };
}

/// Messages for stable server error codes (docs/api-conventions.md). Unknown codes return null.
String? messageForCode(String? code, AppLocalizations l10n) => switch (code) {
  'catalog.country_not_found' => l10n.errorCountryNotFound,
  'catalog.curriculum_not_found' => l10n.errorCurriculumNotFound,
  'auth.invalid_credentials' => l10n.errorInvalidCredentials,
  'auth.locked_out' => l10n.errorLockedOut,
  'auth.email_taken' => l10n.errorEmailTaken,
  'auth.weak_password' => l10n.errorWeakPassword,
  'auth.invalid_refresh_token' => l10n.errorSessionExpired,
  'profile.curriculum_not_available' ||
  'profile.grade_not_in_curriculum' ||
  'profile.stream_not_in_curriculum' ||
  'profile.subject_not_in_grade' => l10n.errorStudyPlanInvalid,
  'enrollment.already_requested' => l10n.errorAlreadyRequested,
  'enrollment.not_accepting_students' => l10n.errorNotAccepting,
  'enrollment.invalid_transition' => l10n.errorInvalidTransition,
  'course.no_published_lesson' => l10n.errorNoPublishedLesson,
  'course.has_students' => l10n.errorHasStudents,
  'course.only_draft_can_be_deleted' => l10n.errorOnlyDraftDelete,
  'course.invalid_link' => l10n.errorInvalidLink,
  'course.invalid_catalog_selection' => l10n.errorInvalidCatalogSelection,
  'common.concurrency_conflict' => l10n.errorConcurrency,
  'video.invalid_url' => l10n.errorVideoInvalidUrl,
  'video.not_found' => l10n.errorVideoNotFound,
  'video.not_embeddable' => l10n.errorVideoNotEmbeddable,
  'video.unavailable' => l10n.errorVideoUnavailable,
  'video.provider_unavailable' => l10n.errorVideoProviderUnavailable,
  _ => null,
};
