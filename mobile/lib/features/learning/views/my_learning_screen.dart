import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/learning_cubits.dart';
import '../models/learning_models.dart';

/// The student's courses: approved first, then pending, then past requests.
class MyLearningScreen extends StatelessWidget {
  const MyLearningScreen({required this.createController, super.key});

  final MyLearningCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: const _MyLearningView());
}

class _MyLearningView extends StatelessWidget {
  const _MyLearningView();

  static const _order = [
    EnrollmentStatus.approved,
    EnrollmentStatus.pending,
    EnrollmentStatus.rejected,
    EnrollmentStatus.revoked,
    EnrollmentStatus.withdrawn,
  ];

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<MyLearningCubit>();
    final enrollments = context.watch<MyLearningCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.navLearning)),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            LoadableView(
              loadable: enrollments,
              onRetry: cubit.load,
              builder: (items) {
                if (items.isEmpty) {
                  return Column(
                    children: [
                      AppEmptyState(message: l10n.myLearningEmpty, icon: Icons.school_outlined),
                      const SizedBox(height: AppSpacing.md),
                      FilledButton.icon(
                        onPressed: () => context.go(AppRoutes.discover),
                        icon: const Icon(Icons.search_rounded),
                        label: Text(l10n.discoverTitle),
                      ),
                    ],
                  );
                }
                final sorted = [...items]..sort((a, b) => _order.indexOf(a.status).compareTo(_order.indexOf(b.status)));
                return Column(
                  children: [
                    for (final item in sorted) ...[
                      _EnrollmentCard(
                        item: item,
                        onTap: () async {
                          await context.push(AppRoutes.studentCourse(item.courseId));
                          await cubit.load();
                        },
                      ),
                      const SizedBox(height: AppSpacing.sm),
                    ],
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}

class _EnrollmentCard extends StatelessWidget {
  const _EnrollmentCard({required this.item, required this.onTap});

  final MyEnrollment item;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final canOpen = item.status == EnrollmentStatus.approved || item.status == EnrollmentStatus.pending;

    return AppCard(
      onTap: canOpen ? onTap : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(child: Text(item.courseTitle, style: textTheme.titleMedium)),
              enrollmentStatusChip(l10n, item.status),
            ],
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text('${item.subjectName} · ${l10n.byTeacher(item.teacherName)}', style: textTheme.bodyMedium),
          if (item.decisionNote case final note? when note.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xs),
            Text(l10n.decisionNoteFromTeacher(note), style: textTheme.bodyMedium?.copyWith(color: AppColors.inkMuted)),
          ],
        ],
      ),
    );
  }
}
