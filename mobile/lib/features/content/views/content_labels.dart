import 'package:flutter/material.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/errors/failure_messages.dart';
import '../../../core/state/load_status.dart';
import '../../../core/state/loadable.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../models/content_models.dart';

/// Localized, toned chips for content and enrollment statuses (shared by teacher and student screens).
AppStatusChip contentStatusChip(AppLocalizations l10n, ContentStatus status) => switch (status) {
  ContentStatus.draft => AppStatusChip(label: l10n.statusDraft),
  ContentStatus.published => AppStatusChip(label: l10n.statusPublished, tone: StatusTone.success),
  ContentStatus.archived => AppStatusChip(label: l10n.statusArchived, tone: StatusTone.warning),
};

AppStatusChip enrollmentStatusChip(AppLocalizations l10n, EnrollmentStatus status) => switch (status) {
  EnrollmentStatus.pending => AppStatusChip(label: l10n.enrollmentPending, tone: StatusTone.warning),
  EnrollmentStatus.approved => AppStatusChip(label: l10n.enrollmentApproved, tone: StatusTone.success),
  EnrollmentStatus.rejected => AppStatusChip(label: l10n.enrollmentRejected, tone: StatusTone.danger),
  EnrollmentStatus.withdrawn => AppStatusChip(label: l10n.enrollmentWithdrawn),
  EnrollmentStatus.revoked => AppStatusChip(label: l10n.enrollmentRevoked, tone: StatusTone.danger),
};

/// Shows a localized failure as a snackbar (for actions such as save, approve, delete).
void showFailure(BuildContext context, ApiFailure failure) {
  final l10n = AppLocalizations.of(context);
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(SnackBar(content: Text(failure.localizedMessage(l10n))));
}

void showMessage(BuildContext context, String message) =>
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));

/// Renders a [Loadable]: skeleton while loading the first time, an error with retry, or the content.
class LoadableView<T> extends StatelessWidget {
  const LoadableView({required this.loadable, required this.builder, required this.onRetry, super.key});

  final Loadable<T> loadable;
  final Widget Function(T value) builder;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final value = loadable.value;
    if (value != null) return builder(value);
    if (loadable.status == LoadStatus.failed) {
      return AppErrorState(
        message: loadable.failure!.localizedMessage(l10n),
        reference: loadable.failure!.localizedReference(l10n),
        retryLabel: l10n.commonRetry,
        onRetry: onRetry,
      );
    }
    return AppSkeletonList(semanticLabel: l10n.commonLoading);
  }
}

/// Dialog actions sit side by side, so they must not take the theme's full-width button size.
final dialogActionStyle = FilledButton.styleFrom(minimumSize: const Size(88, 48));

/// A text dialog for quick inputs (unit title, lesson title, notes). Returns the text, or null if cancelled.
Future<String?> askForText(
  BuildContext context, {
  required String title,
  required String label,
  required String confirmLabel,
  String initial = '',
  bool allowEmpty = false,
}) => showDialog<String>(
  context: context,
  builder:
      (_) =>
          _TextDialog(title: title, label: label, confirmLabel: confirmLabel, initial: initial, allowEmpty: allowEmpty),
);

/// Owns its controller, so the text field stays valid during the dialog's closing animation.
class _TextDialog extends StatefulWidget {
  const _TextDialog({
    required this.title,
    required this.label,
    required this.confirmLabel,
    required this.initial,
    required this.allowEmpty,
  });

  final String title;
  final String label;
  final String confirmLabel;
  final String initial;
  final bool allowEmpty;

  @override
  State<_TextDialog> createState() => _TextDialogState();
}

class _TextDialogState extends State<_TextDialog> {
  late final _controller = TextEditingController(text: widget.initial);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(widget.title),
      content: TextField(
        controller: _controller,
        autofocus: true,
        maxLines: widget.allowEmpty ? 3 : 1,
        decoration: InputDecoration(labelText: widget.label),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: Text(l10n.actionCancel)),
        FilledButton(
          style: dialogActionStyle,
          onPressed: () {
            final text = _controller.text.trim();
            if (text.isNotEmpty || widget.allowEmpty) Navigator.pop(context, text);
          },
          child: Text(widget.confirmLabel),
        ),
      ],
    );
  }
}

Future<bool> confirm(BuildContext context, {required String message, required String confirmLabel}) async {
  final l10n = AppLocalizations.of(context);
  final result = await showDialog<bool>(
    context: context,
    builder:
        (dialogContext) => AlertDialog(
          content: Text(message),
          actions: [
            TextButton(onPressed: () => Navigator.pop(dialogContext, false), child: Text(l10n.actionCancel)),
            FilledButton(
              style: dialogActionStyle,
              onPressed: () => Navigator.pop(dialogContext, true),
              child: Text(confirmLabel),
            ),
          ],
        ),
  );
  return result ?? false;
}
