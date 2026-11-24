import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../core/api/api_failure.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/teacher_courses_cubit.dart';
import '../models/teaching_models.dart';

/// Builds a course: units and lessons, each with its own status; publishing the course makes it discoverable.
class CourseEditorScreen extends StatelessWidget {
  const CourseEditorScreen({required this.createController, super.key});

  final CourseEditorCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _CourseEditorView());
}

class _CourseEditorView extends StatelessWidget {
  const _CourseEditorView();

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<CourseEditorCubit>();
    final course = context.watch<CourseEditorCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(course.value?.title ?? '')),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [LoadableView(loadable: course, onRetry: cubit.load, builder: (value) => _Outline(course: value))],
        ),
      ),
    );
  }
}

class _Outline extends StatelessWidget {
  const _Outline({required this.course});

  final TeacherCourse course;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final cubit = context.read<CourseEditorCubit>();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(child: Text('${course.subjectName} · ${course.gradeLevelName}', style: textTheme.bodyMedium)),
            contentStatusChip(l10n, course.status),
          ],
        ),
        const SizedBox(height: AppSpacing.md),
        if (course.status != ContentStatus.published) ...[
          _Banner(text: l10n.courseDraftHint),
          const SizedBox(height: AppSpacing.sm),
          FilledButton(
            onPressed: () => _run(context, cubit.setCourseStatus(ContentStatus.published)),
            child: Text(l10n.publishCourse),
          ),
        ] else
          OutlinedButton(
            onPressed: () => _run(context, cubit.setCourseStatus(ContentStatus.archived)),
            child: Text(l10n.actionArchive),
          ),
        const SizedBox(height: AppSpacing.xl),
        Row(
          children: [
            Expanded(child: Semantics(header: true, child: Text(l10n.editorUnits, style: textTheme.titleLarge))),
            TextButton.icon(
              onPressed: () async {
                final title = await askForText(
                  context,
                  title: l10n.addUnit,
                  label: l10n.fieldUnitTitle,
                  confirmLabel: l10n.actionAdd,
                );
                if (title != null && context.mounted) await _run(context, cubit.addUnit(title));
              },
              icon: const Icon(Icons.add_rounded),
              label: Text(l10n.addUnit),
            ),
          ],
        ),
        const SizedBox(height: AppSpacing.xs),
        if (course.units.isEmpty) AppEmptyState(message: l10n.editorEmptyUnits, icon: Icons.view_agenda_outlined),
        for (final unit in course.units) ...[_UnitCard(unit: unit), const SizedBox(height: AppSpacing.sm)],
      ],
    );
  }
}

class _UnitCard extends StatelessWidget {
  const _UnitCard({required this.unit});

  final TeacherUnit unit;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final cubit = context.read<CourseEditorCubit>();

    return AppCard(
      padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.sm, AppSpacing.xs, AppSpacing.xs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(child: Text(unit.title, style: textTheme.titleMedium)),
              contentStatusChip(l10n, unit.status),
              _StatusMenu(
                status: unit.status,
                onChange: (status) => _run(context, cubit.setUnitStatus(unit.id, status)),
                onDelete:
                    unit.status == ContentStatus.draft
                        ? () async {
                          if (await confirm(
                                context,
                                message: l10n.confirmDelete(unit.title),
                                confirmLabel: l10n.actionDelete,
                              ) &&
                              context.mounted) {
                            await _run(context, cubit.deleteUnit(unit.id));
                          }
                        }
                        : null,
              ),
            ],
          ),
          if (unit.lessons.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
              child: Text(l10n.editorNoLessons, style: textTheme.bodyMedium),
            ),
          for (final lesson in unit.lessons) _LessonRow(lesson: lesson),
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: TextButton.icon(
              onPressed: () async {
                final title = await askForText(
                  context,
                  title: l10n.addLesson,
                  label: l10n.fieldLessonTitle,
                  confirmLabel: l10n.actionAdd,
                );
                if (title != null && context.mounted) await _run(context, cubit.addLesson(unit.id, title));
              },
              icon: const Icon(Icons.add_rounded),
              label: Text(l10n.addLesson),
            ),
          ),
        ],
      ),
    );
  }
}

class _LessonRow extends StatelessWidget {
  const _LessonRow({required this.lesson});

  final TeacherLesson lesson;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<CourseEditorCubit>();
    return InkWell(
      onTap: () async {
        await context.push(AppRoutes.lessonEditor(lesson.id));
        await cubit.load();
      },
      child: ConstrainedBox(
        constraints: const BoxConstraints(minHeight: AppSizes.minTouchTarget),
        child: Row(
          children: [
            const Icon(Icons.play_lesson_outlined, size: 20, color: AppColors.inkMuted),
            const SizedBox(width: AppSpacing.sm),
            Expanded(child: Text(lesson.title, style: Theme.of(context).textTheme.bodyLarge)),
            if (lesson.status != ContentStatus.published) contentStatusChip(l10n, lesson.status),
            _StatusMenu(
              status: lesson.status,
              onChange: (status) => _run(context, cubit.setLessonStatus(lesson.id, status)),
              onDelete:
                  lesson.status == ContentStatus.draft
                      ? () async {
                        if (await confirm(
                              context,
                              message: l10n.confirmDelete(lesson.title),
                              confirmLabel: l10n.actionDelete,
                            ) &&
                            context.mounted) {
                          await _run(context, cubit.deleteLesson(lesson.id));
                        }
                      }
                      : null,
            ),
          ],
        ),
      ),
    );
  }
}

/// Overflow menu with the status actions that make sense for [status], plus delete for drafts.
class _StatusMenu extends StatelessWidget {
  const _StatusMenu({required this.status, required this.onChange, this.onDelete});

  final ContentStatus status;
  final void Function(ContentStatus) onChange;
  final VoidCallback? onDelete;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return PopupMenuButton<VoidCallback>(
      tooltip: MaterialLocalizations.of(context).showMenuTooltip,
      onSelected: (action) => action(),
      itemBuilder:
          (_) => [
            if (status != ContentStatus.published)
              PopupMenuItem(value: () => onChange(ContentStatus.published), child: Text(l10n.actionPublish)),
            if (status == ContentStatus.published)
              PopupMenuItem(value: () => onChange(ContentStatus.draft), child: Text(l10n.actionUnpublish)),
            if (status == ContentStatus.published)
              PopupMenuItem(value: () => onChange(ContentStatus.archived), child: Text(l10n.actionArchive)),
            if (onDelete != null)
              PopupMenuItem(
                value: onDelete,
                child: Text(l10n.actionDelete, style: const TextStyle(color: AppColors.error)),
              ),
          ],
    );
  }
}

class _Banner extends StatelessWidget {
  const _Banner({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) => AppCard(
    child: Row(
      children: [
        const Icon(Icons.visibility_off_outlined, color: AppColors.inkMuted),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: Text(text, style: Theme.of(context).textTheme.bodyMedium)),
      ],
    ),
  );
}

/// Runs an editor action and shows its failure, if any.
Future<void> _run(BuildContext context, Future<ApiFailure?> action) async {
  final failure = await action;
  if (failure != null && context.mounted) showFailure(context, failure);
}
