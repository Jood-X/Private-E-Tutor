import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';

/// A large single-choice card with a title, optional subtitle and a radio indicator.
class AppOptionTile extends StatelessWidget {
  const AppOptionTile({required this.title, required this.selected, required this.onPressed, this.subtitle, super.key});

  final String title;
  final String? subtitle;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Semantics(
      selected: selected,
      inMutuallyExclusiveGroup: true,
      button: true,
      child: Material(
        color: selected ? AppColors.primarySoft : AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: const BorderRadius.all(AppRadii.card),
          side: BorderSide(color: selected ? AppColors.primary : AppColors.border, width: selected ? 2 : 1.5),
        ),
        child: InkWell(
          borderRadius: const BorderRadius.all(AppRadii.card),
          onTap: onPressed,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 64),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
              child: Row(
                children: [
                  _RadioMark(selected: selected),
                  const SizedBox(width: AppSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(title, style: textTheme.titleMedium),
                        if (subtitle != null) ...[
                          const SizedBox(height: AppSpacing.xxs / 2),
                          Text(subtitle!, style: textTheme.bodyMedium),
                        ],
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _RadioMark extends StatelessWidget {
  const _RadioMark({required this.selected});

  final bool selected;

  @override
  Widget build(BuildContext context) {
    const size = 24.0;
    return AnimatedContainer(
      duration: AppDurations.fast,
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: selected ? AppColors.primary : AppColors.surface,
        border: Border.all(color: selected ? AppColors.primary : AppColors.inputBorder, width: 2),
      ),
      child: selected ? const Icon(Icons.check_rounded, size: 16, color: AppColors.onPrimary) : null,
    );
  }
}
