import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/errors/failure_messages.dart';
import '../../../core/state/load_status.dart';
import '../../../core/state/loadable.dart';
import '../../../design_system/components/app_state_views.dart';
import '../../../design_system/components/app_status_chip.dart';
import '../../../design_system/tokens/app_colors.dart';
import '../../../design_system/tokens/app_dimensions.dart';
import '../../../l10n/generated/app_localizations.dart';
import '../../content/models/content_models.dart';
import '../controllers/learning_cubits.dart';
import 'youtube_embed.dart';

/// Builds the actual player. Injected so widget tests can replace the WebView-based YouTube player.
typedef VideoPlayerBuilder = Widget Function(BuildContext context, VideoPlayback playback);

Widget youtubePlayer(BuildContext context, VideoPlayback playback) => YouTubePlayerView(playback: playback);

/// Plays one lesson video. Playback details are fetched first (only approved students and the teacher get them).
class VideoPlayerScreen extends StatelessWidget {
  const VideoPlayerScreen({required this.createController, this.playerBuilder = youtubePlayer, super.key});

  final VideoPlaybackCubit Function() createController;
  final VideoPlayerBuilder playerBuilder;

  @override
  Widget build(BuildContext context) => BlocProvider(
    create: (_) => createController()..load(),
    child: BlocBuilder<VideoPlaybackCubit, Loadable<VideoPlayback>>(
      builder: (context, state) {
        final playback = state.value;
        if (playback != null) return playerBuilder(context, playback);
        final l10n = AppLocalizations.of(context);
        return Scaffold(
          appBar: AppBar(),
          body: Padding(
            padding: const EdgeInsets.all(AppSpacing.pageGutter),
            child:
                state.status == LoadStatus.failed
                    ? AppErrorState(
                      message: state.failure!.localizedMessage(l10n),
                      reference: state.failure!.localizedReference(l10n),
                      retryLabel: l10n.commonRetry,
                      onRetry: context.read<VideoPlaybackCubit>().load,
                    )
                    // Reserve the player's 16:9 space so nothing jumps when it appears.
                    : const AspectRatio(aspectRatio: 16 / 9, child: _PlayerPlaceholder(loading: true)),
          ),
        );
      },
    ),
  );
}

/// The video page: the YouTube player on top (its own controls: captions, quality, fullscreen), details below.
class YouTubePlayerView extends StatelessWidget {
  const YouTubePlayerView({required this.playback, super.key});

  final VideoPlayback playback;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final duration = playback.durationSeconds;
    return Scaffold(
      appBar: AppBar(title: Text(playback.title, maxLines: 1, overflow: TextOverflow.ellipsis)),
      body: SafeArea(
        top: false,
        child: ListView(
          children: [
            // Player interface and captions follow the app language.
            YouTubeEmbed(videoId: playback.assetId, languageCode: Localizations.localeOf(context).languageCode),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.pageGutter),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(playback.title, style: textTheme.titleLarge),
                  if (duration != null) ...[
                    const SizedBox(height: AppSpacing.xs),
                    Semantics(
                      label: l10n.videoDuration(formatDuration(duration)),
                      excludeSemantics: true,
                      child: AppStatusChip(label: formatDuration(duration)),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Neutral 16:9 box used while a thumbnail or the player loads, or when there is no thumbnail.
class _PlayerPlaceholder extends StatelessWidget {
  const _PlayerPlaceholder({this.loading = false});

  final bool loading;

  @override
  Widget build(BuildContext context) => DecoratedBox(
    decoration: const BoxDecoration(color: AppColors.primarySoft, borderRadius: BorderRadius.all(AppRadii.card)),
    child: Center(
      child:
          loading
              ? CircularProgressIndicator(semanticsLabel: AppLocalizations.of(context).commonLoading)
              : const Icon(Icons.smart_display_outlined, size: 40, color: AppColors.primary),
    ),
  );
}

/// The lesson's video card: 16:9 thumbnail, a large play button, duration at the trailing edge, title below.
class VideoTile extends StatelessWidget {
  const VideoTile({required this.resource, required this.onPlay, super.key});

  final LessonResource resource;
  final VoidCallback onPlay;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final textTheme = Theme.of(context).textTheme;
    final video = resource.video;
    final available = video?.isAvailable ?? true;
    final duration = video?.durationSeconds;
    final label = [
      l10n.playVideo(resource.title),
      if (duration != null) l10n.videoDuration(formatDuration(duration)),
      if (resource.isRequired) l10n.resourceRequired,
    ].join(', ');

    return Semantics(
      button: available,
      enabled: available,
      label: available ? label : '${resource.title}. ${l10n.videoUnavailableTile}',
      excludeSemantics: true,
      child: AppCard(
        onTap: available ? onPlay : null,
        padding: EdgeInsets.zero,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: AppRadii.card),
              child: AspectRatio(
                aspectRatio: 16 / 9,
                child: Stack(
                  fit: StackFit.expand,
                  children: [
                    if (video?.thumbnailUrl case final thumbnail?)
                      Image.network(
                        thumbnail,
                        fit: BoxFit.cover,
                        loadingBuilder: (_, child, progress) => progress == null ? child : const _PlayerPlaceholder(),
                        errorBuilder: (_, _, _) => const _PlayerPlaceholder(),
                      )
                    else
                      const _PlayerPlaceholder(),
                    // Scrim keeps the play button and badge readable on any thumbnail.
                    const DecoratedBox(decoration: BoxDecoration(color: Color(0x47000000))),
                    Center(
                      child: Container(
                        width: 64,
                        height: 64,
                        decoration: const BoxDecoration(color: Colors.white, shape: BoxShape.circle),
                        child: Icon(
                          available ? Icons.play_arrow_rounded : Icons.block_rounded,
                          size: 40,
                          color: available ? AppColors.primary : AppColors.inkMuted,
                        ),
                      ),
                    ),
                    if (duration != null)
                      PositionedDirectional(
                        end: AppSpacing.xs,
                        bottom: AppSpacing.xs,
                        child: DecoratedBox(
                          decoration: const BoxDecoration(
                            color: Color(0xCC000000),
                            borderRadius: BorderRadius.all(Radius.circular(6)),
                          ),
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                            child: Text(
                              formatDuration(duration),
                              textDirection: TextDirection.ltr,
                              style: textTheme.labelMedium?.copyWith(color: Colors.white),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(AppSpacing.md),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(resource.title, style: textTheme.titleSmall),
                        if (!available) Text(l10n.videoUnavailableTile, style: textTheme.bodySmall),
                      ],
                    ),
                  ),
                  if (resource.isRequired) AppStatusChip(label: l10n.resourceRequired, tone: StatusTone.info),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
