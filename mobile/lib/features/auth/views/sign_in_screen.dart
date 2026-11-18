import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_flow_scaffold.dart';
import '../../../design_system/components/app_text_field.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../controllers/form_submission.dart';
import '../controllers/sign_in_cubit.dart';
import 'widgets/form_errors.dart';

class SignInScreen extends StatelessWidget {
  const SignInScreen({required this.createController, super.key});

  final SignInCubit Function() createController;

  @override
  Widget build(BuildContext context) => BlocProvider(create: (_) => createController(), child: const _SignInView());
}

class _SignInView extends StatefulWidget {
  const _SignInView();

  @override
  State<_SignInView> createState() => _SignInViewState();
}

class _SignInViewState extends State<_SignInView> {
  final _email = TextEditingController();
  final _password = TextEditingController();

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  void _submit() {
    FocusScope.of(context).unfocus();
    context.read<SignInCubit>().submit(email: _email.text, password: _password.text);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final form = context.watch<SignInCubit>().state;

    return AppFlowScaffold(
      title: l10n.signInTitle,
      primaryAction: AppPrimaryButton(label: l10n.signInSubmit, busy: form.isSubmitting, onPressed: _submit),
      children: [
        // Wrong credentials are a form-level message, not an error on one field.
        FormFailureBanner(form: form, fields: const {}),
        AutofillGroup(
          child: Column(
            children: [
              AppTextField(
                label: l10n.fieldEmail,
                controller: _email,
                error: _clientError(l10n, form, 'email'),
                keyboardType: TextInputType.emailAddress,
                textInputAction: TextInputAction.next,
                autofillHints: const [AutofillHints.email, AutofillHints.username],
              ),
              const SizedBox(height: AppSpacing.lg),
              AppTextField(
                label: l10n.fieldPassword,
                controller: _password,
                error: _clientError(l10n, form, 'password'),
                obscure: true,
                showPasswordLabel: l10n.showPassword,
                hidePasswordLabel: l10n.hidePassword,
                textInputAction: TextInputAction.done,
                autofillHints: const [AutofillHints.password],
                onSubmitted: (_) => _submit(),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppSpacing.md),
        Center(child: TextButton(onPressed: () => context.go(AppRoutes.welcome), child: Text(l10n.signInNoAccount))),
      ],
    );
  }

  String? _clientError(AppLocalizations l10n, FormSubmission form, String field) =>
      form.problems.containsKey(field) ? fieldError(l10n, form, field) : null;
}
