import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/views/content_labels.dart';
import '../controllers/teacher_courses_cubit.dart';
import '../models/teaching_models.dart';

class TeacherCoursesScreen extends StatelessWidget {
  const TeacherCoursesScreen({required this.createController, super.key});

  final TeacherCoursesCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _TeacherCoursesView());
}

class _TeacherCoursesView extends StatelessWidget {
  const _TeacherCoursesView();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<TeacherCoursesCubit>();
    final courses = context.watch<TeacherCoursesCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.teacherCoursesTitle)),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () async {
          await context.push(AppRoutes.newCourse);
          await cubit.load();
        },
        icon: const Icon(Icons.add_rounded),
        label: Text(l10n.newCourse),
      ),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.fromLTRB(AppSpacing.pageGutter, AppSpacing.xs, AppSpacing.pageGutter, 96),
          children: [
            LoadableView(
              loadable: courses,
              onRetry: cubit.load,
              builder:
                  (items) =>
                      items.isEmpty
                          ? AppEmptyState(message: l10n.teacherCoursesEmpty, icon: Icons.menu_book_outlined)
                          : Column(
                            children: [
                              for (final course in items) ...[
                                _CourseCard(
                                  course: course,
                                  onTap: () async {
                                    await context.push(AppRoutes.courseEditor(course.id));
                                    await cubit.load();
                                  },
                                ),
                                const SizedBox(height: AppSpacing.sm),
                              ],
                            ],
                          ),
            ),
          ],
        ),
      ),
    );
  }
}

class _CourseCard extends StatelessWidget {
  const _CourseCard({required this.course, required this.onTap});

  final TeacherCourseSummary course;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    return AppCard(
      onTap: onTap,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: Text(course.title, style: textTheme.titleMedium)),
              const SizedBox(width: AppSpacing.xs),
              contentStatusChip(l10n, course.status),
            ],
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text('${course.subjectName} · ${course.gradeLevelName}', style: textTheme.bodyMedium),
          const SizedBox(height: AppSpacing.sm),
          Wrap(
            spacing: AppSpacing.xs,
            runSpacing: AppSpacing.xs,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              Text(l10n.courseCounts(course.unitCount, course.lessonCount), style: textTheme.labelSmall),
              Text(l10n.courseStudents(course.activeStudents), style: textTheme.labelSmall),
              if (course.pendingRequests > 0)
                AppStatusChip(label: l10n.courseWaiting(course.pendingRequests), tone: StatusTone.warning),
            ],
          ),
        ],
      ),
    );
  }
}
