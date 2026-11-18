import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../core/config/branding.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';

/// Entry point for signed-out users: choose a role or sign in.
class WelcomeScreen extends StatelessWidget {
  const WelcomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: IconButton(
                  tooltip: l10n.commonSettings,
                  icon: const Icon(Icons.settings_outlined),
                  onPressed: () => context.push(AppRoutes.settings),
                ),
              ),
              const Spacer(),
              // Align keeps the 64x64 tile square; the stretching Column would otherwise widen it.
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: Container(
                  width: 64,
                  height: 64,
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.all(AppRadii.card),
                  ),
                  child: const Icon(Icons.school_outlined, color: AppColors.onPrimary, size: 34),
                ),
              ),
              const SizedBox(height: AppSpacing.xl),
              Text(Branding.productName, style: textTheme.labelLarge?.copyWith(color: AppColors.primary)),
              const SizedBox(height: AppSpacing.xs),
              Semantics(header: true, child: Text(l10n.welcomeTitle, style: textTheme.displaySmall)),
              const SizedBox(height: AppSpacing.sm),
              Text(l10n.welcomeSubtitle, style: textTheme.bodyLarge?.copyWith(color: AppColors.inkMuted)),
              const Spacer(flex: 2),
              FilledButton(onPressed: () => context.push(AppRoutes.studentSignup), child: Text(l10n.welcomeStudentCta)),
              const SizedBox(height: AppSpacing.sm),
              OutlinedButton(
                onPressed: () => context.push(AppRoutes.teacherSignup),
                child: Text(l10n.welcomeTeacherCta),
              ),
              const SizedBox(height: AppSpacing.xs),
              TextButton(onPressed: () => context.push(AppRoutes.signIn), child: Text(l10n.welcomeSignInCta)),
            ],
          ),
        ),
      ),
    );
  }
}
