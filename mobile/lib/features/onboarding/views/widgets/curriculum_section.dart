import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failure_messages.dart';
import '../../../../design_system/components/app_option_tile.dart';
import '../../../../design_system/components/app_state_views.dart';
import '../../../../design_system/tokens/app_dimensions.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../controllers/curriculum_selection_cubit.dart';
import '../../controllers/curriculum_selection_state.dart';

class CurriculumSection extends StatelessWidget {
  const CurriculumSection({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = context.watch<CurriculumSelectionCubit>().state;
    final cubit = context.read<CurriculumSelectionCubit>();

    final Widget content;
    if (state.selectedCountryCode == null) {
      content = AppEmptyState(message: l10n.onboardingSelectCountryFirst, icon: Icons.public_rounded);
    } else {
      content = switch (state.curriculaStatus) {
        LoadStatus.idle || LoadStatus.loading => AppSkeletonList(semanticLabel: l10n.commonLoading),
        LoadStatus.failed => AppErrorState(
          message: state.curriculaFailure!.localizedMessage(l10n),
          reference: state.curriculaFailure!.localizedReference(l10n),
          retryLabel: l10n.commonRetry,
          onRetry: cubit.retryCurricula,
        ),
        LoadStatus.loaded when state.curricula.isEmpty => AppEmptyState(message: l10n.onboardingNoCurricula),
        LoadStatus.loaded => Column(
          children: [
            for (final curriculum in state.curricula) ...[
              AppOptionTile(
                title: curriculum.name,
                subtitle: curriculum.hasStreams ? l10n.onboardingHasStreams : null,
                selected: curriculum.id == state.selectedCurriculumId,
                onPressed: () => cubit.selectCurriculum(curriculum.id),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ],
        ),
      };
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.onboardingCurriculumLabel, style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs),
        AnimatedSwitcher(duration: AppDurations.medium, child: content),
      ],
    );
  }
}
