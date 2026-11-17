import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../core/config/branding.dart';
import '../design_system/theme/app_theme.dart';
import '../features/settings/controllers/language_cubit.dart';
import '../features/settings/models/app_language.dart';
import '../l10n/generated/app_localizations.dart';

/// Root widget. Locale (and therefore text direction) and theme follow [LanguageCubit].
class EtutorApp extends StatelessWidget {
  const EtutorApp({required this.languageCubit, required this.router, super.key});

  final LanguageCubit languageCubit;
  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return BlocProvider.value(
      value: languageCubit,
      child: BlocBuilder<LanguageCubit, AppLanguage>(
        builder:
            (context, language) => MaterialApp.router(
              title: Branding.productName,
              debugShowCheckedModeBanner: false,
              routerConfig: router,
              locale: language.locale,
              supportedLocales: AppLocalizations.supportedLocales,
              localizationsDelegates: AppLocalizations.localizationsDelegates,
              theme: AppTheme.light(language.locale),
            ),
      ),
    );
  }
}
