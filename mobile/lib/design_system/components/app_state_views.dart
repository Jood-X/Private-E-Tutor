import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';

/// Placeholder blocks shown while content loads (skeleton, not a blocking spinner).
class AppSkeletonList extends StatelessWidget {
  const AppSkeletonList({required this.semanticLabel, this.itemCount = 3, this.itemHeight = 64, super.key});

  /// Localized "Loading" announcement for screen readers.
  final String semanticLabel;
  final int itemCount;
  final double itemHeight;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: semanticLabel,
      child: Column(
        children: [
          for (var index = 0; index < itemCount; index++) ...[
            if (index > 0) const SizedBox(height: AppSpacing.sm),
            Container(
              height: itemHeight,
              decoration: const BoxDecoration(
                color: AppColors.surfaceMuted,
                borderRadius: BorderRadius.all(AppRadii.card),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// A calm message with an optional action, used when a list has nothing to show.
class AppEmptyState extends StatelessWidget {
  const AppEmptyState({required this.message, this.icon = Icons.inbox_outlined, super.key});

  final String message;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(AppSpacing.lg),
      decoration: const BoxDecoration(color: AppColors.surface, borderRadius: BorderRadius.all(AppRadii.card)),
      child: Row(
        children: [
          Icon(icon, color: AppColors.inkMuted, size: AppSizes.iconMd),
          const SizedBox(width: AppSpacing.sm),
          Expanded(child: Text(message, style: Theme.of(context).textTheme.bodyLarge)),
        ],
      ),
    );
  }
}

/// A recoverable error: what happened, a retry action and a support reference.
class AppErrorState extends StatelessWidget {
  const AppErrorState({required this.message, required this.retryLabel, this.onRetry, this.reference, super.key});

  final String message;
  final String retryLabel;
  final VoidCallback? onRetry;

  /// Already-localized "Reference: …" text, shown only when the server returned a trace ID.
  final String? reference;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Semantics(
      liveRegion: true,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(AppSpacing.md),
        decoration: const BoxDecoration(color: AppColors.errorSoft, borderRadius: BorderRadius.all(AppRadii.card)),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.error_outline_rounded, color: AppColors.onErrorSoft, size: AppSizes.iconMd),
                const SizedBox(width: AppSpacing.sm),
                Expanded(child: Text(message, style: textTheme.bodyLarge?.copyWith(color: AppColors.onErrorSoft))),
              ],
            ),
            if (reference != null) ...[
              const SizedBox(height: AppSpacing.xs),
              SelectableText(reference!, style: textTheme.labelSmall?.copyWith(color: AppColors.onErrorSoft)),
            ],
            if (onRetry != null) ...[
              const SizedBox(height: AppSpacing.sm),
              Align(
                alignment: AlignmentDirectional.centerStart,
                child: TextButton.icon(
                  onPressed: onRetry,
                  icon: const Icon(Icons.refresh_rounded),
                  label: Text(retryLabel),
                  style: TextButton.styleFrom(foregroundColor: AppColors.onErrorSoft),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Segmented step indicator for multi-step flows (e.g. onboarding).
class AppStepProgress extends StatelessWidget {
  const AppStepProgress({required this.current, required this.total, required this.label, super.key});

  final int current;
  final int total;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: label,
      excludeSemantics: true,
      child: Row(
        children: [
          for (var step = 1; step <= total; step++) ...[
            if (step > 1) const SizedBox(width: AppSpacing.xs - 2),
            Expanded(
              child: Container(
                height: AppSizes.progressHeight,
                decoration: BoxDecoration(
                  color: step <= current ? AppColors.primary : AppColors.border,
                  borderRadius: const BorderRadius.all(AppRadii.pill),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
