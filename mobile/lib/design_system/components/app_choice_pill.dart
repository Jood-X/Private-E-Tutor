import 'package:flutter/material.dart';

import '../tokens/app_colors.dart';
import '../tokens/app_dimensions.dart';

/// A pill-shaped single-choice option (e.g. country, grade). Exposes selected state to screen readers.
class AppChoicePill extends StatelessWidget {
  const AppChoicePill({required this.label, required this.selected, required this.onPressed, super.key});

  final String label;
  final bool selected;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final textStyle = Theme.of(context).textTheme.titleSmall;
    return Semantics(
      selected: selected,
      button: true,
      excludeSemantics: true,
      label: label,
      child: Material(
        color: selected ? AppColors.primary : AppColors.surface,
        shape: StadiumBorder(side: BorderSide(color: selected ? AppColors.primary : AppColors.border, width: 1.5)),
        child: InkWell(
          customBorder: const StadiumBorder(),
          onTap: onPressed,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: AppSizes.minTouchTarget),
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md),
              child: Center(
                widthFactor: 1,
                child: Text(label, style: textStyle?.copyWith(color: selected ? AppColors.onPrimary : AppColors.ink)),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
