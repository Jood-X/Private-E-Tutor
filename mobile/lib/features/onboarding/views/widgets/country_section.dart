import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../../core/errors/failure_messages.dart';
import '../../../../design_system/components/app_choice_pill.dart';
import '../../../../design_system/components/app_state_views.dart';
import '../../../../design_system/tokens/app_dimensions.dart';
import '../../../../l10n/generated/app_localizations.dart';
import '../../controllers/curriculum_selection_cubit.dart';
import '../../controllers/curriculum_selection_state.dart';

class CountrySection extends StatelessWidget {
  const CountrySection({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = context.watch<CurriculumSelectionCubit>().state;
    final cubit = context.read<CurriculumSelectionCubit>();

    final Widget content = switch (state.countriesStatus) {
      LoadStatus.idle || LoadStatus.loading => AppSkeletonList(
        semanticLabel: l10n.commonLoading,
        itemCount: 2,
        itemHeight: AppSizes.minTouchTarget,
      ),
      LoadStatus.failed => AppErrorState(
        message: state.countriesFailure!.localizedMessage(l10n),
        reference: state.countriesFailure!.localizedReference(l10n),
        retryLabel: l10n.commonRetry,
        onRetry: cubit.loadCountries,
      ),
      LoadStatus.loaded => Wrap(
        spacing: AppSpacing.xs,
        runSpacing: AppSpacing.xs,
        children: [
          for (final country in state.countries)
            AppChoicePill(
              label: country.name,
              selected: country.isoCode == state.selectedCountryCode,
              onPressed: () => cubit.selectCountry(country.isoCode),
            ),
        ],
      ),
    };

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.onboardingCountryLabel, style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs),
        AnimatedSwitcher(duration: AppDurations.medium, child: content),
      ],
    );
  }
}
