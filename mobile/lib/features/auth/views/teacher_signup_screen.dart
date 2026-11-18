import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../core/errors/failure_messages.dart';
import '../../../core/state/load_status.dart';
import '../../../design_system/components/app_choice_pill.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../settings/controllers/language_cubit.dart';
import '../controllers/teacher_signup_cubit.dart';
import 'widgets/form_errors.dart';

class TeacherSignupScreen extends StatelessWidget {
  const TeacherSignupScreen({required this.createController, super.key});

  final TeacherSignupCubit Function() createController;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..loadCountries(), child: const _TeacherSignupView());
}

class _TeacherSignupView extends StatefulWidget {
  const _TeacherSignupView();

  @override
  State<_TeacherSignupView> createState() => _TeacherSignupViewState();
}

class _TeacherSignupViewState extends State<_TeacherSignupView> {
  static const _fields = {'displayName', 'email', 'password', 'headline', 'countryCode'};

  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _headline = TextEditingController();

  @override
  void dispose() {
    for (final controller in [_name, _email, _password, _headline]) {
      controller.dispose();
    }
    super.dispose();
  }

  void _submit() {
    FocusScope.of(context).unfocus();
    context.read<TeacherSignupCubit>().submit(
      displayName: _name.text,
      email: _email.text,
      password: _password.text,
      headline: _headline.text,
      languageCode: context.read<LanguageCubit>().state.code,
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = context.watch<TeacherSignupCubit>().state;
    final form = state.form;

    return AppFlowScaffold(
      title: l10n.teacherSignupTitle,
      subtitle: l10n.teacherSignupSubtitle,
      primaryAction: AppPrimaryButton(label: l10n.onboardingCreateAccount, busy: form.isSubmitting, onPressed: _submit),
      children: [
        FormFailureBanner(form: form, fields: _fields),
        AutofillGroup(
          child: Column(
            children: [
              AppTextField(
                label: l10n.fieldName,
                controller: _name,
                error: fieldError(l10n, form, 'displayName'),
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.name],
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: l10n.fieldHeadline,
                controller: _headline,
                helper: l10n.fieldHeadlineHelper,
                error: fieldError(l10n, form, 'headline'),
                textInputAction: TextInputAction.next,
              ),
              const SizedBox(height: AppSpacing.lg),
              _CountryPicker(state: state, error: fieldError(l10n, form, 'countryCode')),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: l10n.fieldEmail,
                controller: _email,
                error: fieldError(l10n, form, 'email'),
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email],
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: l10n.fieldPassword,
                controller: _password,
                helper: l10n.fieldNewPasswordHelper,
                error: fieldError(l10n, form, 'password'),
                obscure: true,
                showPasswordLabel: l10n.showPassword,
                hidePasswordLabel: l10n.hidePassword,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.newPassword],
                onSubmitted: (_) => _submit(),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        Center(
          child: TextButton(onPressed: () => context.go(AppRoutes.signIn), child: Text(l10n.onboardingHaveAccount)),
        ),
      ],
    );
  }
}

class _CountryPicker extends StatelessWidget {
  const _CountryPicker({required this.state, required this.error});

  final TeacherSignupState state;
  final String? error;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final cubit = context.read<TeacherSignupCubit>();
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.onboardingCountryLabel, style: textTheme.titleSmall),
        const SizedBox(height: AppSpacing.xs),
        switch (state.countriesStatus) {
          LoadStatus.loaded => Wrap(
            spacing: AppSpacing.xs,
            runSpacing: AppSpacing.xs,
            children: [
              for (final country in state.countries)
                AppChoicePill(
                  label: country.name,
                  selected: country.isoCode == state.country?.isoCode,
                  onPressed: () => cubit.selectCountry(country),
                ),
            ],
          ),
          LoadStatus.failed => AppErrorState(
            message: state.countriesFailure!.localizedMessage(l10n),
            retryLabel: l10n.commonRetry,
            onRetry: cubit.loadCountries,
          ),
          _ => AppSkeletonList(semanticLabel: l10n.commonLoading, itemCount: 1, itemHeight: AppSizes.minTouchTarget),
        },
        if (error != null) ...[
          const SizedBox(height: AppSpacing.xs),
          Text(error!, style: textTheme.bodyMedium?.copyWith(color: AppColors.error)),
        ],
      ],
    );
  }
}
