import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';
import 'app_state_views.dart';

/// Layout for task screens and multi-step flows: a top bar (back, optional step label, trailing
/// action), optional step progress, a headline with subtitle, scrollable content and one pinned
/// primary action at the bottom.
class AppFlowScaffold extends StatelessWidget {
  const AppFlowScaffold({
    required this.title,
    required this.children,
    required this.primaryAction,
    this.subtitle,
    this.stepLabel,
    this.step,
    this.totalSteps,
    this.showBack = true,
    this.trailing,
    super.key,
  });

  final String title;
  final String? subtitle;
  final List<Widget> children;

  /// Usually a full-width FilledButton; pinned above the bottom safe area.
  final Widget primaryAction;
  final String? stepLabel;
  final int? step;
  final int? totalSteps;
  final bool showBack;
  final Widget? trailing;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    final canPop = Navigator.of(context).canPop();
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            SizedBox(
              height: AppSizes.minTouchTarget + AppSpacing.xs,
              child: Row(
                children: [
                  if (showBack && canPop) const BackButton() else const SizedBox(width: AppSpacing.pageGutter),
                  Expanded(child: Text(stepLabel ?? '', style: textTheme.labelMedium)),
                  if (trailing != null) trailing!,
                  const SizedBox(width: AppSpacing.xs),
                ],
              ),
            ),
            if (step != null && totalSteps != null)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: AppSpacing.pageGutter),
                child: AppStepProgress(current: step!, total: totalSteps!, label: stepLabel ?? ''),
              ),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(AppSpacing.pageGutter),
                children: [
                  Semantics(header: true, child: Text(title, style: textTheme.headlineSmall)),
                  if (subtitle != null) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Text(subtitle!, style: textTheme.bodyMedium),
                  ],
                  const SizedBox(height: AppSpacing.xl),
                  ...children,
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.pageGutter,
                AppSpacing.sm,
                AppSpacing.pageGutter,
                AppSpacing.md,
              ),
              child: primaryAction,
            ),
          ],
        ),
      ),
    );
  }
}

/// A FilledButton that shows a spinner and ignores taps while [busy]. It keeps its enabled colors
/// while busy (a disabled grey button would hide the white spinner).
class AppPrimaryButton extends StatelessWidget {
  const AppPrimaryButton({required this.label, required this.onPressed, this.busy = false, super.key});

  final String label;
  final VoidCallback? onPressed;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final action = onPressed;
    return Semantics(
      liveRegion: busy,
      child: FilledButton(
        onPressed: action == null ? null : (busy ? () {} : action),
        child:
            busy
                ? const SizedBox.square(
                  dimension: 22,
                  child: CircularProgressIndicator(strokeWidth: 2.5, color: AppColors.onPrimary),
                )
                : Text(label),
      ),
    );
  }
}
