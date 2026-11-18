import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/errors/failure_messages.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../controllers/session_cubit.dart';

/// Shown while a saved session is restored, or when it could not be checked (e.g. offline).
class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final state = context.watch<SessionCubit>().state;
    final session = context.read<SessionCubit>();

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          child: Center(
            child:
                state.status == SessionStatus.unavailable
                    ? Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(l10n.splashUnavailable, style: Theme.of(context).textTheme.titleLarge),
                        const SizedBox(height: AppSpacing.md),
                        AppErrorState(
                          message: state.failure!.localizedMessage(l10n),
                          reference: state.failure!.localizedReference(l10n),
                          retryLabel: l10n.commonRetry,
                          onRetry: session.restore,
                        ),
                        const SizedBox(height: AppSpacing.sm),
                        TextButton(onPressed: session.signOut, child: Text(l10n.splashSignInInstead)),
                      ],
                    )
                    : Semantics(
                      label: l10n.commonLoading,
                      child: const CircularProgressIndicator(color: AppColors.primary),
                    ),
          ),
        ),
      ),
    );
  }
}
