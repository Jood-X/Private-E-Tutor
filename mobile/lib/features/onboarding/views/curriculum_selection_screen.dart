import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../auth/controllers/student_signup_cubit.dart';
import '../../settings/controllers/language_cubit.dart';
import '../../settings/models/app_language.dart';
import '../controllers/curriculum_selection_cubit.dart';
import 'widgets/country_section.dart';
import 'widgets/curriculum_section.dart';

/// Student sign-up step 1 of 3: choose country and curriculum.
class CurriculumSelectionScreen extends StatelessWidget {
  const CurriculumSelectionScreen({required this.createController, super.key});

  static const step = 1;
  static const totalSteps = 3;

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
    final canContinue = context.select((CurriculumSelectionCubit cubit) => cubit.state.canContinue);

    return AppFlowScaffold(
      title: l10n.onboardingCurriculumTitle,
      subtitle: l10n.onboardingCurriculumSubtitle,
      stepLabel: l10n.onboardingStep(CurriculumSelectionScreen.step, CurriculumSelectionScreen.totalSteps),
      step: CurriculumSelectionScreen.step,
      totalSteps: CurriculumSelectionScreen.totalSteps,
      trailing: IconButton(
        tooltip: l10n.commonSettings,
        icon: const Icon(Icons.settings_outlined),
        onPressed: () => context.push(AppRoutes.settings),
      ),
      primaryAction: FilledButton(
        onPressed: canContinue ? () => _continue(context) : null,
        child: Text(l10n.commonContinue),
      ),
      children: const [CountrySection(), SizedBox(height: AppSpacing.xl), CurriculumSection()],
    );
  }

  void _continue(BuildContext context) {
    final state = context.read<CurriculumSelectionCubit>().state;
    final country = state.countries.firstWhere((item) => item.isoCode == state.selectedCountryCode);
    final curriculum = state.curricula.firstWhere((item) => item.id == state.selectedCurriculumId);
    context.read<StudentSignupCubit>().chooseCurriculum(country, curriculum);
    context.push(AppRoutes.studentDetails);
  }
}
