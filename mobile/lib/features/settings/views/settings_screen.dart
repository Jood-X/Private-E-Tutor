import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../design_system/components/app_option_tile.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../controllers/language_cubit.dart';
import '../models/app_language.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final current = context.watch<LanguageCubit>().state;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.commonSettings)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            Text(l10n.settingsLanguage, style: textTheme.titleLarge),
            const SizedBox(height: AppSpacing.xxs),
            Text(l10n.settingsLanguageHint, style: textTheme.bodyMedium),
            const SizedBox(height: AppSpacing.md),
            for (final language in AppLanguage.values) ...[
              AppOptionTile(
                // Each name is shown in its own script and direction.
                title: language.nativeName,
                selected: language == current,
                onPressed: () => context.read<LanguageCubit>().select(language),
              ),
              const SizedBox(height: AppSpacing.sm),
            ],
          ],
        ),
      ),
    );
  }
}
