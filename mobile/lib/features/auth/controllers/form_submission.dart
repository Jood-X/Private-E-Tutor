import 'package:equatable/equatable.dart';

import '../../../core/api/api_failure.dart';
import '../models/credential_rules.dart';

enum SubmitStatus { idle, submitting, failed }

/// Common state of a submit-able form: client-side problems per field and the last server failure.
class FormSubmission extends Equatable {
  const FormSubmission({this.status = SubmitStatus.idle, this.problems = const {}, this.failure});

  final SubmitStatus status;

  /// Client-side validation problems keyed by API field name (e.g. `email`).
  final Map<String, InputProblem> problems;
  final ApiFailure? failure;

  bool get isSubmitting => status == SubmitStatus.submitting;

  FormSubmission invalid(Map<String, InputProblem> problems) =>
      FormSubmission(status: SubmitStatus.idle, problems: problems);

  static const submitting = FormSubmission(status: SubmitStatus.submitting);

  static FormSubmission failed(ApiFailure failure) => FormSubmission(status: SubmitStatus.failed, failure: failure);

  @override
  List<Object?> get props => [status, problems, failure];
}

/// Collects non-null problems into a map keyed by field name.
Map<String, InputProblem> collectProblems(Map<String, InputProblem?> checks) => {
  for (final entry in checks.entries)
    if (entry.value case final problem?) entry.key: problem,
};
