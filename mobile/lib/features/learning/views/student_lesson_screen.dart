import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../../app/routes.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../../content/views/content_labels.dart';
import '../controllers/learning_cubits.dart';
import 'video_player_screen.dart';

/// Opens a link outside the app. Injected so widget tests don't launch anything.
typedef LinkOpener = Future<bool> Function(Uri uri);

Future<bool> openExternally(Uri uri) => launchUrl(uri, mode: LaunchMode.externalApplication);

/// A lesson: summary, the teacher's notes and its resources.
class StudentLessonScreen extends StatelessWidget {
  const StudentLessonScreen({required this.createController, this.openLink = openExternally, super.key});

  final StudentLessonCubit Function() createController;
  final LinkOpener openLink;

  @override
  Widget build(BuildContext context) =>
      BlocProvider(create: (_) => createController()..load(), child: _LessonView(openLink: openLink));
}

class _LessonView extends StatelessWidget {
  const _LessonView({required this.openLink});

  final LinkOpener openLink;

  @override
  Widget build(BuildContext context) {
    final cubit = context.read<StudentLessonCubit>();
    final lesson = context.watch<StudentLessonCubit>().state;
    return Scaffold(
      appBar: AppBar(title: Text(lesson.value?.title ?? '')),
      body: RefreshIndicator(
        onRefresh: cubit.load,
        child: ListView(
          padding: const EdgeInsets.all(AppSpacing.pageGutter),
          children: [
            LoadableView(
              loadable: lesson,
              onRetry: cubit.load,
              builder: (value) => _Lesson(lesson: value, openLink: openLink),
            ),
          ],
        ),
      ),
    );
  }
}

class _Lesson extends StatelessWidget {
  const _Lesson({required this.lesson, required this.openLink});

  final LessonDetail lesson;
  final LinkOpener openLink;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (lesson.estimatedMinutes case final minutes?)
          Align(alignment: AlignmentDirectional.centerStart, child: AppStatusChip(label: l10n.lessonMinutes(minutes))),
        if (lesson.summary.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.sm),
          Text(lesson.summary, style: textTheme.titleMedium),
        ],
        if (lesson.notes.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.md),
          SelectableText(lesson.notes, style: textTheme.bodyLarge?.copyWith(height: 1.6)),
        ],
        if (lesson.resources.isNotEmpty) ...[
          const SizedBox(height: AppSpacing.xl),
          Semantics(header: true, child: Text(l10n.resourcesTitle, style: textTheme.titleLarge)),
          const SizedBox(height: AppSpacing.xs),
          for (final resource in lesson.resources) ...[
            if (resource.type == ResourceType.video)
              VideoTile(resource: resource, onPlay: () => context.push(AppRoutes.studentVideo(resource.id)))
            else
              _ResourceTile(resource: resource, openLink: openLink),
            const SizedBox(height: AppSpacing.xs),
          ],
        ],
      ],
    );
  }
}

class _ResourceTile extends StatelessWidget {
  const _ResourceTile({required this.resource, required this.openLink});

  final LessonResource resource;
  final LinkOpener openLink;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final isLink = resource.type == ResourceType.link;
    final uri = isLink ? Uri.tryParse(resource.url ?? '') : null;

    Future<void> open() async {
      if (!isLink) {
        showMessage(context, l10n.pdfViewerSoon);
        return;
      }
      // Only https links are accepted by the API; anything else is ignored defensively.
      if (uri == null || uri.scheme != 'https' || !await openLink(uri)) {
        if (context.mounted) showMessage(context, l10n.errorLinkOpen);
      }
    }

    return AppCard(
      onTap: open,
      padding: const EdgeInsets.symmetric(horizontal: AppSpacing.md, vertical: AppSpacing.sm),
      child: Row(
        children: [
          Icon(isLink ? Icons.link_rounded : Icons.picture_as_pdf_outlined, color: AppColors.primary),
          const SizedBox(width: AppSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(resource.title, style: Theme.of(context).textTheme.titleSmall),
                if (uri != null)
                  Text(
                    uri.host,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(color: AppColors.inkMuted),
                    textDirection: TextDirection.ltr,
                  ),
              ],
            ),
          ),
          if (resource.isRequired) AppStatusChip(label: l10n.resourceRequired, tone: StatusTone.info),
          const SizedBox(width: AppSpacing.xs),
          Icon(isLink ? Icons.open_in_new_rounded : Icons.chevron_right_rounded, color: AppColors.inkMuted, size: 20),
        ],
      ),
    );
  }
}
