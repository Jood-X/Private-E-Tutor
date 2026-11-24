import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';

enum StatusTone { neutral, info, success, warning, danger }

/// Small status label. Tone colors are paired with text (never color alone) and meet 4.5:1 contrast.
class AppStatusChip extends StatelessWidget {
  const AppStatusChip({required this.label, this.tone = StatusTone.neutral, super.key});

  final String label;
  final StatusTone tone;

  @override
  Widget build(BuildContext context) {
    final (background, foreground) = switch (tone) {
      StatusTone.neutral => (AppColors.surfaceMuted, AppColors.inkMuted),
      StatusTone.info => (AppColors.primarySoft, AppColors.primaryStrong),
      StatusTone.success => (AppColors.successSoft, AppColors.onSuccessSoft),
      StatusTone.warning => (AppColors.accentSoft, AppColors.onAccentSoft),
      StatusTone.danger => (AppColors.errorSoft, AppColors.onErrorSoft),
    };
    return DecoratedBox(
      decoration: BoxDecoration(color: background, borderRadius: const BorderRadius.all(AppRadii.pill)),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm - 2, vertical: AppSpacing.xxs - 1),
        child: Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(color: foreground)),
      ),
    );
  }
}

/// A white rounded card with the standard shadow and optional tap behavior.
class AppCard extends StatelessWidget {
  const AppCard({required this.child, this.onTap, this.padding = const EdgeInsets.all(AppSpacing.md), super.key});

  final Widget child;
  final VoidCallback? onTap;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: const BoxDecoration(borderRadius: BorderRadius.all(AppRadii.card), boxShadow: AppShadows.card),
      child: Material(
        color: AppColors.surface,
        borderRadius: const BorderRadius.all(AppRadii.card),
        clipBehavior: Clip.antiAlias,
        child: InkWell(onTap: onTap, child: Padding(padding: padding, child: child)),
      ),
    );
  }
}
