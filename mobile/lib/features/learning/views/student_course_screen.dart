import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/learning_cubits.dart';
import '../models/learning_models.dart';

/// A course for the student: the outline once approved, otherwise a status banner.
class StudentCourseScreen extends StatelessWidget {
  const StudentCourseScreen({required this.createController, super.key});

  final StudentCourseCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _StudentCourseView());
}

class _StudentCourseView extends StatelessWidget {
  const _StudentCourseView();

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<StudentCourseCubit>();
    final course = context.watch<StudentCourseCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(course.value?.title ?? '')),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [LoadableView(loadable: course, onRetry: cubit.load, builder: (value) => _Course(course: value))],
        ),
      ),
    );
  }
}

class _Course extends StatelessWidget {
  const _Course({required this.course});

  final StudentCourse course;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final status = course.enrollmentStatus;
    final units = course.units;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                '${course.subjectName} · ${course.gradeLevelName}\n${l10n.byTeacher(course.teacherName)}',
                style: textTheme.bodyMedium,
              ),
            ),
            if (status != null) enrollmentStatusChip(l10n, status),
          ],
        ),
        if (course.description.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(course.description, style: textTheme.bodyLarge),
        ],
        const SizedBox(height: AppSpacing.lg),
        if (units != null) ...[
          for (final unit in units) ...[_UnitSection(unit: unit), const SizedBox(height: AppSpacing.md)],
        ] else ...[
          _InfoBanner(text: status == EnrollmentStatus.pending ? l10n.coursePendingInfo : l10n.courseLockedInfo),
          if (status != EnrollmentStatus.pending) ...[
            const SizedBox(height: AppSpacing.sm),
            OutlinedButton(
              onPressed: () => context.push(AppRoutes.teacherProfile(course.teacherId)),
              child: Text(l10n.requestToJoin),
            ),
          ],
        ],
        if (status == EnrollmentStatus.pending || status == EnrollmentStatus.approved) ...[
          const SizedBox(height: AppSpacing.lg),
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: TextButton(
              style: TextButton.styleFrom(foregroundColor: AppColors.error),
              onPressed: () async {
                final cubit = context.read<StudentCourseCubit>();
                if (!await confirm(context, message: l10n.confirmWithdraw, confirmLabel: l10n.actionWithdraw)) return;
                final failure = await cubit.withdraw();
                if (failure != null && context.mounted) showFailure(context, failure);
              },
              child: Text(l10n.actionWithdraw),
            ),
          ),
        ],
      ],
    );
  }
}

class _UnitSection extends StatelessWidget {
  const _UnitSection({required this.unit});

  final StudentUnit unit;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    return AppCard(
      padding: const EdgeInsets.fromLTRB(AppSpacing.md, AppSpacing.sm, AppSpacing.md, AppSpacing.xs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Semantics(header: true, child: Text(unit.title, style: textTheme.titleMedium)),
          for (final lesson in unit.lessons)
            InkWell(
              onTap: () => context.push(AppRoutes.studentLesson(lesson.id)),
              child: ConstrainedBox(
                constraints: const BoxConstraints(minHeight: AppSizes.minTouchTarget),
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: AppSpacing.xs),
                  child: Row(
                    children: [
                      const Icon(Icons.play_lesson_outlined, size: 20, color: AppColors.primary),
                      const SizedBox(width: AppSpacing.sm),
                      Expanded(child: Text(lesson.title, style: textTheme.bodyLarge)),
                      if (lesson.estimatedMinutes case final minutes?)
                        Text(
                          l10n.lessonMinutes(minutes),
                          style: textTheme.bodySmall?.copyWith(color: AppColors.inkMuted),
                        ),
                      const Icon(Icons.chevron_right_rounded, color: AppColors.inkMuted),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class _InfoBanner extends StatelessWidget {
  const _InfoBanner({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) => AppCard(
    child: Row(
      children: [
        const Icon(Icons.lock_clock_outlined, color: AppColors.inkMuted),
        const SizedBox(width: AppSpacing.sm),
        Expanded(child: Text(text, style: Theme.of(context).textTheme.bodyLarge)),
      ],
    ),
  );
}
