import 'package:flutter/material.dart';

import '../../../../core/errors/failure_messages.dart';
import '../../../../design_system/components/app_state_views.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../controllers/form_submission.dart';
import '../../models/credential_rules.dart';

/// The message to show under [field]: a client-side problem first, otherwise a server field error.
String? fieldError(AppLocalizations l10n, FormSubmission form, String field) {
  final problem = form.problems[field];
  if (problem != null) {
    return switch (problem) {
      InputProblem.required => l10n.validationRequired,
      InputProblem.email => l10n.validationEmail,
      InputProblem.password => l10n.validationPassword,
      InputProblem.name => l10n.validationName,
      InputProblem.headline => l10n.validationHeadline,
    };
  }
  return form.failure?.localizedFieldErrors(l10n)[field];
}

/// Form-level error banner for failures that are not about one field (wrong password, offline, …).
class FormFailureBanner extends StatelessWidget {
  const FormFailureBanner({required this.form, required this.fields, super.key});

  final FormSubmission form;

  /// Fields shown on this form; errors for them are displayed inline instead.
  final Set<String> fields;

  @override
  Widget build(BuildContext context) {
    final failure = form.failure;
    if (failure == null) return const SizedBox.shrink();
    final l10n = AppLocalizations.of(context);
    final inline = failure.errors.isNotEmpty && failure.errors.every((error) => fields.contains(error.field));
    if (inline) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 16),
      child: AppErrorState(
        message: failure.localizedMessage(l10n),
        reference: failure.kind.isRetryable ? failure.localizedReference(l10n) : null,
        retryLabel: l10n.commonRetry,
      ),
    );
  }
}
