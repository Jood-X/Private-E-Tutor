import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../core/config/branding.dart';
import '../design_system/theme/app_theme.dart';
import '../features/auth/controllers/session_cubit.dart';
import '../features/settings/controllers/language_cubit.dart';
import '../features/settings/models/app_language.dart';
import '../l10n/generated/app_localizations.dart';

/// Root widget. Locale (and therefore text direction) and theme follow [LanguageCubit]; navigation
/// follows [SessionCubit] through the router's redirect.
class EtutorApp extends StatelessWidget {
  const EtutorApp({required this.languageCubit, required this.sessionCubit, required this.router, super.key});

  final LanguageCubit languageCubit;
  final SessionCubit sessionCubit;
  final GoRouter router;

  @override
  Widget build(BuildContext context) {
    return MultiBlocProvider(
      providers: [BlocProvider.value(value: languageCubit), BlocProvider.value(value: sessionCubit)],
      child: BlocListener<SessionCubit, SessionState>(
        // On sign-in (e.g. on a new device) use the language saved in the user's profile.
        listenWhen:
            (previous, current) =>
                current.status == SessionStatus.signedIn && previous.status != SessionStatus.signedIn,
        listener: (_, state) => languageCubit.select(AppLanguage.fromCode(state.user?.preferredLanguage)),
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
      ),
    );
  }
}
