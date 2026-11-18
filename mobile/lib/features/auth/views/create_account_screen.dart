import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../onboarding/views/curriculum_selection_screen.dart';
import '../../settings/controllers/language_cubit.dart';
import '../controllers/student_signup_cubit.dart';
import 'widgets/form_errors.dart';

/// Student sign-up step 3 of 3: name, email and password. Submitting creates the account and signs in.
class CreateAccountScreen extends StatefulWidget {
  const CreateAccountScreen({super.key});

  @override
  State<CreateAccountScreen> createState() => _CreateAccountScreenState();
}

class _CreateAccountScreenState extends State<CreateAccountScreen> {
  static const _fields = {'displayName', 'email', 'password'};

  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  void _submit() {
    FocusScope.of(context).unfocus();
    context.read<StudentSignupCubit>().submit(
      displayName: _name.text,
      email: _email.text,
      password: _password.text,
      languageCode: context.read<LanguageCubit>().state.code,
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final signup = context.watch<StudentSignupCubit>().state;
    if (!signup.hasStudyPlan) {
      WidgetsBinding.instance.addPostFrameCallback((_) => context.go(AppRoutes.studentSignup));
      return const SizedBox.shrink();
    }
    final form = signup.form;

    return AppFlowScaffold(
      title: l10n.onboardingAccountTitle,
      stepLabel: l10n.onboardingStep(3, CurriculumSelectionScreen.totalSteps),
      step: 3,
      totalSteps: CurriculumSelectionScreen.totalSteps,
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
