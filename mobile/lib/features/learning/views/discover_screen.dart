import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_choice_pill.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/views/content_labels.dart';
import '../controllers/discover_cubit.dart';
import '../models/learning_models.dart';

/// Find a teacher: search, filter by subject, scroll for more.
class DiscoverScreen extends StatelessWidget {
  const DiscoverScreen({required this.createController, super.key});

  final DiscoverCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..start(), child: const _DiscoverView());
}

class _DiscoverView extends StatelessWidget {
  const _DiscoverView();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<DiscoverCubit>();
    final state = context.watch<DiscoverCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.discoverTitle)),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: NotificationListener<ScrollNotification>(
          onNotification: (notification) {
            if (notification.metrics.extentAfter < 300) cubit.loadMore();
            return false;
          },
          child: ListView(
            padding: const EdgeInsets.fromLTRB(
              AppSpacing.pageGutter,
              AppSpacing.xs,
              AppSpacing.pageGutter,
              AppSpacing.xl,
            ),
            children: [
              TextField(
                onChanged: cubit.searchChanged,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(hintText: l10n.searchHint, prefixIcon: const Icon(Icons.search_rounded)),
              ),
              const SizedBox(height: AppSpacing.sm),
              _SubjectFilter(state: state),
              const SizedBox(height: AppSpacing.md),
              LoadableView(
                loadable: state.teachers,
                onRetry: cubit.load,
                builder:
                    (teachers) =>
                        teachers.isEmpty
                            ? AppEmptyState(message: l10n.discoverEmpty, icon: Icons.person_search_outlined)
                            : Column(
                              children: [
                                for (final teacher in teachers) ...[
                                  _TeacherCardView(teacher: teacher),
                                  const SizedBox(height: AppSpacing.sm),
                                ],
                                if (state.teachers.isLoading)
                                  const Padding(
                                    padding: EdgeInsets.all(AppSpacing.md),
                                    child: CircularProgressIndicator(),
                                  ),
                              ],
                            ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _SubjectFilter extends StatelessWidget {
  const _SubjectFilter({required this.state});

  final DiscoverState state;

  @override
  Widget build(BuildContext context) {
    final subjects = state.subjects.value;
    if (subjects == null || subjects.isEmpty) return const SizedBox.shrink();
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<DiscoverCubit>();
    final selected = state.query.subjectId;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          AppChoicePill(
            label: l10n.allSubjects,
            selected: selected == null,
            onPressed: () => cubit.selectSubject(null),
          ),
          for (final subject in subjects) ...[
            const SizedBox(width: AppSpacing.xs),
            AppChoicePill(
              label: subject.name,
              selected: selected == subject.id,
              onPressed: () => cubit.selectSubject(subject.id),
            ),
          ],
        ],
      ),
    );
  }
}

class _TeacherCardView extends StatelessWidget {
  const _TeacherCardView({required this.teacher});

  final TeacherCard teacher;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return AppCard(
      onTap: () => context.push(AppRoutes.teacherProfile(teacher.id)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _Initials(name: teacher.displayName),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(teacher.displayName, style: textTheme.titleMedium),
                if (teacher.headline.isNotEmpty) ...[
                  const SizedBox(height: AppSpacing.xxs),
                  Text(teacher.headline, style: textTheme.bodyMedium, maxLines: 2, overflow: TextOverflow.ellipsis),
                ],
                const SizedBox(height: AppSpacing.xs),
                Wrap(
                  spacing: AppSpacing.xs,
                  runSpacing: AppSpacing.xs,
                  children: [
                    for (final subject in teacher.subjects) AppStatusChip(label: subject, tone: StatusTone.info),
                  ],
                ),
              ],
            ),
          ),
          const Icon(Icons.chevron_right_rounded, color: AppColors.inkMuted),
        ],
      ),
    );
  }
}

/// A plain initials avatar (no photos in the MVP).
class _Initials extends StatelessWidget {
  const _Initials({required this.name});

  final String name;

  @override
  Widget build(BuildContext context) {
    final initials = name.trim().split(RegExp(r'\s+')).take(2).map((part) => part.isEmpty ? '' : part[0]).join();
    return ExcludeSemantics(
      child: CircleAvatar(
        radius: 24,
        backgroundColor: AppColors.primarySoft,
        child: Text(
          initials.toUpperCase(),
          style: Theme.of(context).textTheme.titleMedium?.copyWith(color: AppColors.primaryStrong),
        ),
      ),
    );
  }
}
