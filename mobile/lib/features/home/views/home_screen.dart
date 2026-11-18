import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../auth/controllers/session_cubit.dart';
import '../../auth/models/app_user.dart';

/// Signed-in landing screen. Courses, schedule and progress arrive in later phases; for now it shows
/// the user's profile and the teacher verification status.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final user = context.select((SessionCubit cubit) => cubit.state.user);
    if (user == null) return const SizedBox.shrink();

    return Scaffold(
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            Row(
              children: [
                Expanded(
                  child: Semantics(
                    header: true,
                    child: Text(l10n.homeGreeting(user.displayName), style: textTheme.headlineSmall),
                  ),
                ),
                IconButton(
                  tooltip: l10n.commonSettings,
                  icon: const Icon(Icons.settings_outlined),
                  onPressed: () => context.push(AppRoutes.settings),
                ),
              ],
            ),
            const SizedBox(height: AppSpacing.xs),
            Align(alignment: AlignmentDirectional.centerStart, child: _RoleChip(role: user.role)),
            const SizedBox(height: AppSpacing.lg),
            if (user.teacher case final teacher?) ...[
              _VerificationBanner(teacher: teacher),
              const SizedBox(height: AppSpacing.lg),
            ],
            AppEmptyState(message: l10n.homeComingSoon, icon: Icons.menu_book_outlined),
            const SizedBox(height: AppSpacing.xl),
            Text(l10n.profileTitle, style: textTheme.titleLarge),
            const SizedBox(height: AppSpacing.sm),
            _ProfileCard(user: user),
            const SizedBox(height: AppSpacing.xl),
            OutlinedButton.icon(
              onPressed: context.read<SessionCubit>().signOut,
              icon: const Icon(Icons.logout_rounded),
              label: Text(l10n.profileSignOut),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.error,
                side: const BorderSide(color: AppColors.error, width: 1.5),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _RoleChip extends StatelessWidget {
  const _RoleChip({required this.role});

  final UserRole role;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final label = role == UserRole.teacher ? l10n.homeRoleTeacher : l10n.homeRoleStudent;
    return DecoratedBox(
      decoration: const BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.all(AppRadii.pill)),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: AppSpacing.sm, vertical: AppSpacing.xxs),
        child: Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(color: AppColors.primaryStrong)),
      ),
    );
  }
}

class _VerificationBanner extends StatelessWidget {
  const _VerificationBanner({required this.teacher});

  final TeacherInfo teacher;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final verified = teacher.verification == TeacherVerification.verified;
    final (background, foreground, icon, message) =
        verified
            ? (AppColors.successSoft, AppColors.onSuccessSoft, Icons.verified_outlined, l10n.homeTeacherVerified)
            : (AppColors.accentSoft, AppColors.onAccentSoft, Icons.hourglass_top_rounded, l10n.homeTeacherPending);
    return Container(
      padding: const EdgeInsets.all(AppSpacing.md),
      decoration: BoxDecoration(color: background, borderRadius: const BorderRadius.all(AppRadii.card)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, color: foreground, size: AppSizes.iconMd),
          const SizedBox(width: AppSpacing.sm),
          Expanded(child: Text(message, style: Theme.of(context).textTheme.bodyLarge?.copyWith(color: foreground))),
        ],
      ),
    );
  }
}

class _ProfileCard extends StatelessWidget {
  const _ProfileCard({required this.user});

  final AppUser user;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.xs),
      decoration: const BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.all(AppRadii.card),
        boxShadow: AppShadows.card,
      ),
      child: Column(
        children: [
          _Row(label: l10n.fieldEmail, value: user.email),
          const Divider(),
          _Row(label: l10n.profileCountry, value: user.countryCode),
          const Divider(),
          // Time-zone IDs are technical; shown left-to-right in both languages.
          _Row(label: l10n.profileTimeZone, value: user.timeZoneId, forceLtr: true),
        ],
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.label, required this.value, this.forceLtr = false});

  final String label;
  final String value;
  final bool forceLtr;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AppSpacing.sm),
      child: Row(
        children: [
          Expanded(child: Text(label, style: textTheme.bodyMedium)),
          Flexible(
            flex: 2,
            child: Text(
              value,
              style: textTheme.bodyLarge,
              // Align to the layout's end edge even when the value itself is forced LTR.
              textAlign: Directionality.of(context) == TextDirection.rtl ? TextAlign.left : TextAlign.right,
              textDirection: forceLtr || value.contains('@') ? TextDirection.ltr : null,
              overflow: TextOverflow.ellipsis,
            ),
          ),
        ],
      ),
    );
  }
}
