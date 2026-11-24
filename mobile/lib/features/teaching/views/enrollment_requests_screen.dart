import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/enrollment_requests_cubit.dart';
import '../models/teaching_models.dart';

/// Two tabs: pending requests (approve/decline) and current students (remove).
class EnrollmentRequestsScreen extends StatelessWidget {
  const EnrollmentRequestsScreen({required this.createController, super.key});

  final EnrollmentListCubit Function(EnrollmentStatus status) createController;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: Text(l10n.requestsTitle),
          bottom: TabBar(tabs: [Tab(text: l10n.tabPending), Tab(text: l10n.tabStudents)]),
        ),
        body: TabBarView(
          children: [
            BlocProvider(
              create: (_) => createController(EnrollmentStatus.pending)..load(),
              child: _EnrollmentList(emptyMessage: l10n.requestsEmpty),
            ),
            BlocProvider(
              create: (_) => createController(EnrollmentStatus.approved)..load(),
              child: _EnrollmentList(emptyMessage: l10n.studentsEmpty),
            ),
          ],
        ),
      ),
    );
  }
}

class _EnrollmentList extends StatelessWidget {
  const _EnrollmentList({required this.emptyMessage});

  final String emptyMessage;

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<EnrollmentListCubit>();
    final state = context.watch<EnrollmentListCubit>().state;

    return RefreshIndicator(
      onRefresh: cubit.load,
      child: NotificationListener<ScrollNotification>(
        // Infinite scroll: load the next page near the end of the list.
        onNotification: (notification) {
          if (notification.metrics.extentAfter < 300) cubit.loadMore();
          return false;
        },
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            LoadableView(
              loadable: state.items,
              onRetry: cubit.load,
              builder:
                  (items) =>
                      items.isEmpty
                          ? AppEmptyState(message: emptyMessage, icon: Icons.inbox_outlined)
                          : Column(
                            children: [
                              for (final item in items) ...[
                                _EnrollmentCard(item: item),
                                const SizedBox(height: AppSpacing.sm),
                              ],
                              if (state.items.isLoading)
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
    );
  }
}

class _EnrollmentCard extends StatelessWidget {
  const _EnrollmentCard({required this.item});

  final TeacherEnrollment item;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final cubit = context.read<EnrollmentListCubit>();
    final pending = item.status == EnrollmentStatus.pending;

    Future<void> decide(EnrollmentDecision decision, {String? note}) async {
      final failure = await cubit.decide(item.id, decision, note: note);
      if (failure != null && context.mounted) showFailure(context, failure);
    }

    return AppCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(child: Text(item.studentName, style: textTheme.titleMedium)),
              AppStatusChip(label: item.studentGradeName),
            ],
          ),
          const SizedBox(height: AppSpacing.xxs),
          Text(item.courseTitle, style: textTheme.bodyMedium),
          if (item.requestMessage case final message?) ...[
            const SizedBox(height: AppSpacing.xs),
            Text('“$message”', style: textTheme.bodyLarge),
          ],
          const SizedBox(height: AppSpacing.sm),
          if (pending)
            Row(
              children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () async {
                      final note = await askForText(
                        context,
                        title: l10n.actionReject,
                        label: l10n.decisionNoteLabel,
                        confirmLabel: l10n.actionReject,
                        allowEmpty: true,
                      );
                      if (note != null) await decide(EnrollmentDecision.reject, note: note);
                    },
                    child: Text(l10n.actionReject),
                  ),
                ),
                const SizedBox(width: AppSpacing.sm),
                Expanded(
                  child: FilledButton(
                    onPressed: () => decide(EnrollmentDecision.approve),
                    child: Text(l10n.actionApprove),
                  ),
                ),
              ],
            )
          else
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton(
                style: TextButton.styleFrom(foregroundColor: AppColors.error),
                onPressed: () async {
                  if (await confirm(context, message: l10n.confirmRevoke, confirmLabel: l10n.actionRevoke)) {
                    await decide(EnrollmentDecision.revoke);
                  }
                },
                child: Text(l10n.actionRevoke),
              ),
            ),
        ],
      ),
    );
  }
}
