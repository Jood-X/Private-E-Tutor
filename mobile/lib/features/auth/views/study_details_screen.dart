import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../core/errors/failure_messages.dart';
import '../../../core/state/load_status.dart';
import '../../../design_system/components/app_choice_pill.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../onboarding/models/catalog_models.dart';
import '../../onboarding/views/curriculum_selection_screen.dart';
import '../controllers/student_signup_cubit.dart';
import '../controllers/study_details_cubit.dart';

/// Student sign-up step 2 of 3: grade, stream (when the curriculum has streams) and subjects.
class StudyDetailsScreen extends StatelessWidget {
  const StudyDetailsScreen({required this.createController, super.key});

  final StudyDetailsCubit Function(Curriculum curriculum) createController;

  @override
  Widget build(BuildContext context) {
    final curriculum = context.read<StudentSignupCubit>().state.curriculum;
    if (curriculum == null) {
      // Deep link into the middle of the flow: start again from step 1.
      WidgetsBinding.instance.addPostFrameCallback((_) => context.go(AppRoutes.studentSignup));
      return const SizedBox.shrink();
    }
    return BlocProvider(create: (_) => createController(curriculum)..load(), child: const _StudyDetailsView());
  }
}

class _StudyDetailsView extends StatelessWidget {
  const _StudyDetailsView();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = context.watch<StudyDetailsCubit>().state;
    final cubit = context.read<StudyDetailsCubit>();
    final stepLabel = l10n.onboardingStep(2, CurriculumSelectionScreen.totalSteps);

    return AppFlowScaffold(
      title: l10n.onboardingDetailsTitle,
      stepLabel: stepLabel,
      step: 2,
      totalSteps: CurriculumSelectionScreen.totalSteps,
      primaryAction: FilledButton(
        onPressed: state.canContinue ? () => _continue(context, state) : null,
        child: Text(l10n.commonContinue),
      ),
      children: [
        if (state.optionsStatus == LoadStatus.failed)
          AppErrorState(
            message: state.optionsFailure!.localizedMessage(l10n),
            reference: state.optionsFailure!.localizedReference(l10n),
            retryLabel: l10n.commonRetry,
            onRetry: cubit.load,
          )
        else if (state.optionsStatus != LoadStatus.loaded)
          AppSkeletonList(semanticLabel: l10n.commonLoading, itemCount: 2, itemHeight: AppSizes.minTouchTarget)
        else ...[
          _PillGroup(
            label: l10n.onboardingGradeLabel,
            items: state.grades,
            isSelected: (item) => item.id == state.gradeLevelId,
            onPressed: (item) => cubit.selectGrade(item.id),
          ),
          if (state.streams.isNotEmpty) ...[
            const SizedBox(height: AppSpacing.xl),
            _PillGroup(
              label: l10n.onboardingStreamLabel,
              items: state.streams,
              isSelected: (item) => item.id == state.streamId,
              onPressed: (item) => cubit.selectStream(item.id),
            ),
          ],
          const SizedBox(height: AppSpacing.xl),
          _SubjectsSection(state: state),
        ],
      ],
    );
  }

  void _continue(BuildContext context, StudyDetailsState state) {
    context.read<StudentSignupCubit>().chooseStudy(
      gradeLevelId: state.gradeLevelId!,
      streamId: state.streamId,
      subjectIds: state.subjectIds.toList()..sort(),
    );
    context.push(AppRoutes.studentAccount);
  }
}

class _SubjectsSection extends StatelessWidget {
  const _SubjectsSection({required this.state});

  final StudyDetailsState state;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<StudyDetailsCubit>();
    final Widget content = switch (state.subjectsStatus) {
      _ when state.gradeLevelId == null => AppEmptyState(message: l10n.onboardingChooseGradeFirst),
      LoadStatus.failed => AppErrorState(
        message: state.subjectsFailure!.localizedMessage(l10n),
        reference: state.subjectsFailure!.localizedReference(l10n),
        retryLabel: l10n.commonRetry,
        onRetry: cubit.retrySubjects,
      ),
      LoadStatus.loaded => Wrap(
        spacing: AppSpacing.xs,
        runSpacing: AppSpacing.xs,
        children: [
          for (final subject in state.subjects)
            AppChoicePill(
              label: subject.name,
              selected: state.subjectIds.contains(subject.id),
              onPressed: () => cubit.toggleSubject(subject.id),
            ),
        ],
      ),
      _ => AppSkeletonList(semanticLabel: l10n.commonLoading, itemCount: 2, itemHeight: AppSizes.minTouchTarget),
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.onboardingSubjectsLabel, style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xxs),
        Text(l10n.onboardingSubjectsHint, style: Theme.of(context).textTheme.bodyMedium),
        const SizedBox(height: AppSpacing.sm),
        content,
      ],
    );
  }
}

class _PillGroup extends StatelessWidget {
  const _PillGroup({required this.label, required this.items, required this.isSelected, required this.onPressed});

  final String label;
  final List<CatalogItem> items;
  final bool Function(CatalogItem) isSelected;
  final void Function(CatalogItem) onPressed;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs),
        Wrap(
          spacing: AppSpacing.xs,
          runSpacing: AppSpacing.xs,
          children: [
            for (final item in items)
              AppChoicePill(label: item.name, selected: isSelected(item), onPressed: () => onPressed(item)),
          ],
        ),
      ],
    );
  }
}
