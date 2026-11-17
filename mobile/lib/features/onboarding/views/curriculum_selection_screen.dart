import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../settings/controllers/language_cubit.dart';
import '../../settings/models/app_language.dart';
import '../controllers/curriculum_selection_cubit.dart';
import 'widgets/country_section.dart';
import 'widgets/curriculum_section.dart';

/// Onboarding step 1 of 3: choose country and curriculum.
class CurriculumSelectionScreen extends StatelessWidget {
  const CurriculumSelectionScreen({required this.createController, super.key});

  static const _step = 1;
  static const _totalSteps = 3;

  /// Supplied by the router so the view never reaches into the DI container itself.
  final CurriculumSelectionCubit Function() createController;

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => createController()..loadCountries(),
      child: BlocListener<LanguageCubit, AppLanguage>(
        listener: (context, _) => context.read<CurriculumSelectionCubit>().reloadForLanguageChange(),
        child: const _CurriculumSelectionView(),
      ),
    );
  }
}

class _CurriculumSelectionView extends StatelessWidget {
  const _CurriculumSelectionView();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final stepLabel = l10n.onboardingStep(CurriculumSelectionScreen._step, CurriculumSelectionScreen._totalSteps);

    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Padding(
              padding: const EdgeInsetsDirectional.fromSTEB(AppSpacing.pageGutter, AppSpacing.xs, AppSpacing.xs, 0),
              child: Row(
                children: [
                  Expanded(child: Text(stepLabel, style: textTheme.labelMedium)),
                  IconButton(
                    tooltip: l10n.commonSettings,
                    icon: const Icon(Icons.settings_outlined),
                    onPressed: () => context.push(AppRoutes.settings),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.pageGutter),
              child: AppStepProgress(
                current: CurriculumSelectionScreen._step,
                total: CurriculumSelectionScreen._totalSteps,
                label: stepLabel,
              ),
            ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(AppSpacing.pageGutter),
                children: [
                  Text(l10n.onboardingCurriculumTitle, style: textTheme.headlineSmall),
                  const SizedBox(height: AppSpacing.xs),
                  Text(l10n.onboardingCurriculumSubtitle, style: textTheme.bodyMedium),
                  const SizedBox(height: AppSpacing.xl),
                  const CountrySection(),
                  const SizedBox(height: AppSpacing.xl),
                  const CurriculumSection(),
                ],
              ),
            ),
            const _ContinueBar(),
          ],
        ),
      ),
    );
  }
}

class _ContinueBar extends StatelessWidget {
  const _ContinueBar();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final canContinue = context.select((CurriculumSelectionCubit cubit) => cubit.state.canContinue);

    return Padding(
      padding: const EdgeInsets.fromLTRB(AppSpacing.pageGutter, AppSpacing.sm, AppSpacing.pageGutter, AppSpacing.md),
      child: FilledButton(
        onPressed:
            canContinue
                ? () =>
                    ScaffoldMessenger.of(context)
                      ..hideCurrentSnackBar()
                      ..showSnackBar(SnackBar(content: Text(l10n.onboardingSignUpComingSoon)))
                : null,
        child: Text(l10n.commonContinue),
      ),
    );
  }
}
