import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/views/content_labels.dart';
import '../controllers/learning_cubits.dart';
import '../models/learning_models.dart';

/// A teacher's profile and published courses; the student asks to join a course from here.
class TeacherProfileScreen extends StatelessWidget {
  const TeacherProfileScreen({required this.createController, super.key});

  final TeacherProfileCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _TeacherProfileView());
}

class _TeacherProfileView extends StatelessWidget {
  const _TeacherProfileView();

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<TeacherProfileCubit>();
    final teacher = context.watch<TeacherProfileCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(teacher.value?.displayName ?? '')),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            LoadableView(loadable: teacher, onRetry: cubit.load, builder: (value) => _Profile(teacher: value)),
          ],
        ),
      ),
    );
  }
}

class _Profile extends StatelessWidget {
  const _Profile({required this.teacher});

  final TeacherDetail teacher;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (teacher.headline.isNotEmpty) Text(teacher.headline, style: textTheme.titleMedium),
        if (teacher.bio.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(teacher.bio, style: textTheme.bodyLarge),
        ],
        if (!teacher.isAcceptingStudents) ...[
          const SizedBox(height: AppSpacing.sm),
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: AppStatusChip(label: l10n.notAccepting, tone: StatusTone.warning),
          ),
        ],
        const SizedBox(height: AppSpacing.xl),
        Semantics(header: true, child: Text(l10n.navCourses, style: textTheme.titleLarge)),
        const SizedBox(height: AppSpacing.sm),
        for (final course in teacher.courses) ...[
          _CourseCard(course: course, canRequest: teacher.isAcceptingStudents),
          const SizedBox(height: AppSpacing.sm),
        ],
      ],
    );
  }
}

class _CourseCard extends StatelessWidget {
  const _CourseCard({required this.course, required this.canRequest});

  final PublicCourse course;
  final bool canRequest;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final status = course.myStatus;

    return AppCard(
      onTap: status == null ? null : () => context.push(AppRoutes.studentCourse(course.id)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: Text(course.title, style: textTheme.titleMedium)),
              if (status != null) enrollmentStatusChip(l10n, status),
            ],
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text('${course.subjectName} · ${course.gradeLevelName}', style: textTheme.bodyMedium),
          if (course.description.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(course.description, style: textTheme.bodyLarge, maxLines: 3, overflow: TextOverflow.ellipsis),
          ],
          const SizedBox(height: AppSpacing.xs),
          Text(
            l10n.courseCounts(course.unitCount, course.lessonCount),
            style: textTheme.bodySmall?.copyWith(color: AppColors.inkMuted),
          ),
          if (status == null && canRequest) ...[
            const SizedBox(height: AppSpacing.sm),
            SizedBox(
              width: double.infinity,
              child: FilledButton(onPressed: () => _request(context), child: Text(l10n.requestToJoin)),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _request(BuildContext context) async {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<TeacherProfileCubit>();
    final message = await askForText(
      context,
      title: l10n.requestToJoin,
      label: l10n.requestMessageLabel,
      confirmLabel: l10n.sendRequest,
      allowEmpty: true,
    );
    if (message == null) return;
    final failure = await cubit.requestToJoin(course.id, message: message);
    if (!context.mounted) return;
    failure == null ? showMessage(context, l10n.requestSent) : showFailure(context, failure);
  }
}
