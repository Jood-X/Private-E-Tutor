import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/errors/failure_messages.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/teacher_courses_cubit.dart';
import '../models/teaching_models.dart';

class LessonEditorScreen extends StatelessWidget {
  const LessonEditorScreen({required this.createController, super.key});

  final LessonEditorCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _LessonEditorView());
}

class _LessonEditorView extends StatelessWidget {
  const _LessonEditorView();

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<LessonEditorCubit>();
    final lesson = context.select((LessonEditorCubit cubit) => cubit.state);
    final value = lesson.value;
    if (value == null) {
      return Scaffold(
        appBar: AppBar(),
        body: Padding(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          child: LoadableView(loadable: lesson, onRetry: cubit.load, builder: (_) => const SizedBox.shrink()),
        ),
      );
    }
    // Keyed by lesson ID so the form fields initialize once with the loaded values.
    return _LessonForm(key: ValueKey(value.id), lesson: value);
  }
}

class _LessonForm extends StatefulWidget {
  const _LessonForm({required this.lesson, super.key});

  final LessonDetail lesson;

  @override
  State<_LessonForm> createState() => _LessonFormState();
}

class _LessonFormState extends State<_LessonForm> {
  late final _title = TextEditingController(text: widget.lesson.title);
  late final _summary = TextEditingController(text: widget.lesson.summary);
  late final _notes = TextEditingController(text: widget.lesson.notes);
  late final _minutes = TextEditingController(text: widget.lesson.estimatedMinutes?.toString() ?? '');
  bool _saving = false;

  @override
  void dispose() {
    for (final controller in [_title, _summary, _notes, _minutes]) {
      controller.dispose();
    }
    super.dispose();
  }

  Future<void> _save() async {
    final l10n = AppLocalizations.of(context);
    setState(() => _saving = true);
    final failure = await context.read<LessonEditorCubit>().save(
      LessonEdit(
        title: _title.text,
        summary: _summary.text,
        notes: _notes.text,
        estimatedMinutes: int.tryParse(_minutes.text.trim()),
      ),
    );
    if (!mounted) return;
    setState(() => _saving = false);
    failure == null ? showMessage(context, l10n.savedMessage) : showFailure(context, failure);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final resources = context.select(
      (LessonEditorCubit cubit) => cubit.state.value?.resources ?? const <LessonResource>[],
    );

    return AppFlowScaffold(
      title: l10n.lessonEditorTitle,
      primaryAction: AppPrimaryButton(label: l10n.actionSave, busy: _saving, onPressed: _save),
      children: [
        AppTextField(label: l10n.fieldLessonTitle, controller: _title, textInputAction: TextInputAction.next),
        const SizedBox(height: AppSpacing.lg),
        AppTextField(label: l10n.fieldSummary, controller: _summary, textInputAction: TextInputAction.next),
        const SizedBox(height: AppSpacing.lg),
        AppTextField(label: l10n.fieldMinutes, controller: _minutes, keyboardType: TextInputType.number),
        const SizedBox(height: AppSpacing.lg),
        Text(l10n.fieldNotes, style: textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs - 2),
        TextField(controller: _notes, minLines: 6, maxLines: null, keyboardType: TextInputType.multiline),
        const SizedBox(height: AppSpacing.xl),
        Semantics(header: true, child: Text(l10n.resourcesTitle, style: textTheme.titleLarge)),
        // Two actions wrap onto a second line on narrow screens or with longer Arabic labels.
        Wrap(
          spacing: AppSpacing.xs,
          children: [
            TextButton.icon(
              onPressed: _addVideo,
              icon: const Icon(Icons.smart_display_outlined),
              label: Text(l10n.addVideo),
            ),
            TextButton.icon(onPressed: _addLink, icon: const Icon(Icons.add_link_rounded), label: Text(l10n.addLink)),
          ],
        ),
        if (resources.isEmpty) AppEmptyState(message: l10n.resourcesEmpty, icon: Icons.attach_file_rounded),
        for (final resource in resources) _ResourceRow(resource: resource),
        const SizedBox(height: AppSpacing.sm),
        Text(l10n.pdfUploadSoon, style: textTheme.bodyMedium),
      ],
    );
  }

  Future<void> _addVideo() async {
    final cubit = context.read<LessonEditorCubit>();
    await showDialog<void>(
      context: context,
      builder: (_) => _VideoDialog(submit: (url, title) => cubit.addVideo(url: url, title: title)),
    );
  }

  Future<void> _addLink() async {
    final link = await showDialog<({String title, String url})>(context: context, builder: (_) => const _LinkDialog());
    if (link == null || !mounted) return;
    final failure = await context.read<LessonEditorCubit>().addLink(title: link.title, url: link.url);
    if (failure != null && mounted) showFailure(context, failure);
  }
}

/// Adds a YouTube video. The dialog sends the request itself and stays open while the server checks the video, so
/// the provider's answer (not found, embedding off, offline) appears right under the link field.
class _VideoDialog extends StatefulWidget {
  const _VideoDialog({required this.submit});

  final Future<ApiFailure?> Function(String url, String? title) submit;

  @override
  State<_VideoDialog> createState() => _VideoDialogState();
}

class _VideoDialogState extends State<_VideoDialog> {
  final _url = TextEditingController();
  final _title = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _url.dispose();
    _title.dispose();
    super.dispose();
  }

  Future<void> _add() async {
    if (_url.text.trim().isEmpty || _busy) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    final failure = await widget.submit(_url.text, _title.text);
    if (!mounted) return;
    if (failure == null) {
      Navigator.pop(context);
      return;
    }
    final message = failure.localizedMessage(AppLocalizations.of(context));
    setState(() {
      _busy = false;
      _error = message;
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(l10n.addVideo),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: _url,
              autofocus: true,
              enabled: !_busy,
              keyboardType: TextInputType.url,
              textDirection: TextDirection.ltr,
              decoration: InputDecoration(
                labelText: l10n.fieldVideoLink,
                hintText: 'https://youtu.be/…',
                hintTextDirection: TextDirection.ltr,
                helperText: _error == null ? l10n.videoLinkHelp : null,
                helperMaxLines: 3,
                errorText: _error,
                errorMaxLines: 4,
              ),
            ),
            const SizedBox(height: AppSpacing.sm),
            TextField(
              controller: _title,
              enabled: !_busy,
              decoration: InputDecoration(labelText: l10n.fieldVideoTitleOptional),
            ),
            if (_busy) ...[
              const SizedBox(height: AppSpacing.md),
              Semantics(
                liveRegion: true,
                child: Row(
                  children: [
                    const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2)),
                    const SizedBox(width: AppSpacing.sm),
                    Expanded(child: Text(l10n.checkingVideo)),
                  ],
                ),
              ),
            ],
            // Announce provider errors to screen readers as they appear.
            if (_error case final error?) Semantics(liveRegion: true, label: error, child: const SizedBox.shrink()),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: _busy ? null : () => Navigator.pop(context), child: Text(l10n.actionCancel)),
        FilledButton(style: dialogActionStyle, onPressed: _busy ? null : _add, child: Text(l10n.actionAdd)),
      ],
    );
  }
}

/// Asks for a link's title and https address in one dialog.
class _LinkDialog extends StatefulWidget {
  const _LinkDialog();

  @override
  State<_LinkDialog> createState() => _LinkDialogState();
}

class _LinkDialogState extends State<_LinkDialog> {
  final _title = TextEditingController();
  final _url = TextEditingController(text: 'https://');

  @override
  void dispose() {
    _title.dispose();
    _url.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return AlertDialog(
      title: Text(l10n.addLink),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(controller: _title, autofocus: true, decoration: InputDecoration(labelText: l10n.fieldLinkTitle)),
          const SizedBox(height: AppSpacing.sm),
          TextField(
            controller: _url,
            keyboardType: TextInputType.url,
            textDirection: TextDirection.ltr,
            decoration: InputDecoration(labelText: l10n.fieldUrl),
          ),
        ],
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: Text(l10n.actionCancel)),
        FilledButton(
          style: dialogActionStyle,
          onPressed: () {
            if (_title.text.trim().isEmpty) return;
            Navigator.pop(context, (title: _title.text.trim(), url: _url.text.trim()));
          },
          child: Text(l10n.actionAdd),
        ),
      ],
    );
  }
}

class _ResourceRow extends StatelessWidget {
  const _ResourceRow({required this.resource});

  final LessonResource resource;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final (icon, subtitle) = switch (resource.type) {
      ResourceType.link => (Icons.link_rounded, resource.url ?? ''),
      ResourceType.document => (Icons.picture_as_pdf_outlined, resource.fileName ?? l10n.resourceDocument),
      ResourceType.video => (
        Icons.smart_display_outlined,
        switch (resource.video) {
          VideoInfo(isAvailable: false) => l10n.videoUnavailableTile,
          VideoInfo(:final durationSeconds?) => formatDuration(durationSeconds),
          _ => 'YouTube',
        },
      ),
    };
    return ListTile(
      contentPadding: EdgeInsets.zero,
      leading: Icon(icon, color: AppColors.primary),
      title: Text(resource.title),
      subtitle: Text(
        subtitle,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        textDirection: resource.type == ResourceType.link ? TextDirection.ltr : null,
      ),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (resource.isRequired) AppStatusChip(label: l10n.resourceRequired, tone: StatusTone.info),
          IconButton(
            tooltip: l10n.actionDelete,
            icon: const Icon(Icons.delete_outline_rounded, color: AppColors.error),
            onPressed: () async {
              if (!await confirm(
                context,
                message: l10n.confirmDelete(resource.title),
                confirmLabel: l10n.actionDelete,
              )) {
                return;
              }
              if (!context.mounted) return;
              final ApiFailure? failure = await context.read<LessonEditorCubit>().deleteResource(resource.id);
              if (failure != null && context.mounted) showFailure(context, failure);
            },
          ),
        ],
      ),
    );
  }
}
