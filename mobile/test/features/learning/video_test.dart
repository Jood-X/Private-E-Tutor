import 'package:etutor_app/core/api/api_failure.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/content/models/content_models.dart';
import 'package:etutor_app/features/learning/controllers/learning_cubits.dart';
import 'package:etutor_app/features/learning/views/video_player_screen.dart';
import 'package:etutor_app/features/learning/views/youtube_embed.dart';
import 'package:etutor_app/features/teaching/controllers/teacher_courses_cubit.dart';
import 'package:etutor_app/features/teaching/views/lesson_editor_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_content_repositories.dart';
import '../../helpers/pump_screen.dart';

void main() {
  group('models', () {
    test('formats durations with Western digits', () {
      expect(formatDuration(45), '0:45');
      expect(formatDuration(725), '12:05');
      expect(formatDuration(3723), '1:02:03');
    });

    test('reads video resources and their display details', () {
      final resource = LessonResource.fromJson({
        'id': 'r-1',
        'type': 'Video',
        'title': 'Waves',
        'isRequired': true,
        'url': null,
        'fileName': null,
        'sizeBytes': null,
        'video': {'durationSeconds': 600, 'thumbnailUrl': 'https://i.ytimg.com/x.jpg', 'status': 'Unavailable'},
      });

      expect(resource.type, ResourceType.video);
      expect(
        resource.video,
        const VideoInfo(isAvailable: false, durationSeconds: 600, thumbnailUrl: 'https://i.ytimg.com/x.jpg'),
      );
    });
  });

  test('the embed uses privacy-enhanced mode, autoplays after the tap, and follows the app language', () {
    final uri = YouTubeEmbed.embedUri('M7lc1UVf-VE', 'ar');

    expect(uri.host, 'www.youtube-nocookie.com');
    expect(uri.path, '/embed/M7lc1UVf-VE');
    expect(uri.queryParameters, containsPair('autoplay', '1'));
    expect(uri.queryParameters, containsPair('hl', 'ar'));
    expect(uri.queryParameters, containsPair('cc_lang_pref', 'ar'));
    expect(youtubeEmbedReferer, startsWith('https://'));
  });

  group('video tile', () {
    testWidgets('shows the duration and plays on tap, with one spoken label', (tester) async {
      var played = 0;
      await pumpScreen(tester, Scaffold(body: VideoTile(resource: sampleVideo, onPlay: () => played++)));

      expect(find.text('12:05'), findsOneWidget);
      expect(find.bySemanticsLabel('Play video: Forces explained, Duration 12:05, Required'), findsOneWidget);
      await tester.tap(find.byType(VideoTile));
      expect(played, 1);
    });

    testWidgets('an unavailable video cannot be played and says why', (tester) async {
      var played = 0;
      const removed = LessonResource(
        id: 'r-9',
        type: ResourceType.video,
        title: 'Old lecture',
        isRequired: false,
        video: VideoInfo(isAvailable: false),
      );
      await pumpScreen(tester, Scaffold(body: VideoTile(resource: removed, onPlay: () => played++)));

      expect(find.text('This video is no longer available.'), findsOneWidget);
      await tester.tap(find.byType(VideoTile));
      expect(played, 0);
    });
  });

  group('player screen', () {
    testWidgets('loads playback details and hands them to the player', (tester) async {
      final learning = FakeLearningRepository();
      await pumpScreen(
        tester,
        VideoPlayerScreen(
          createController: () => VideoPlaybackCubit(repository: learning, resourceId: 'r-3'),
          playerBuilder: (_, playback) => Text('playing ${playback.assetId}'),
        ),
      );

      expect(find.text('playing dQw4w9WgXcQ'), findsOneWidget);
    });

    testWidgets('a video that is no longer available shows a localized error with retry', (tester) async {
      final learning =
          FakeLearningRepository()
            ..playbackResult = const ApiError(
              ApiFailure(kind: ApiFailureKind.businessRule, code: 'video.unavailable', statusCode: 422),
            );
      await pumpScreen(
        tester,
        VideoPlayerScreen(
          createController: () => VideoPlaybackCubit(repository: learning, resourceId: 'r-3'),
          playerBuilder: (_, _) => const Text('player'),
        ),
      );

      expect(find.text('This video is no longer available. Ask your teacher about it.'), findsOneWidget);
      expect(find.text('Try again'), findsOneWidget);
      expect(find.text('player'), findsNothing);
    });
  });

  group('teacher add video', () {
    Future<FakeTeachingRepository> openDialog(WidgetTester tester) async {
      final teaching = FakeTeachingRepository();
      await pumpScreen(
        tester,
        LessonEditorScreen(createController: () => LessonEditorCubit(repository: teaching, lessonId: 'l-1')),
      );
      await tester.scrollUntilVisible(find.text('Add video'), 200, scrollable: find.byType(Scrollable).first);
      await tester.tap(find.text('Add video'));
      await tester.pumpAndSettle();
      return teaching;
    }

    testWidgets('a refused video keeps the dialog open with the reason under the link', (tester) async {
      final teaching = await openDialog(tester);
      teaching.addVideoResult = const ApiError(
        ApiFailure(kind: ApiFailureKind.businessRule, code: 'video.not_embeddable', statusCode: 422),
      );

      await tester.enterText(find.widgetWithText(TextField, 'YouTube link'), 'https://youtu.be/abcdefghijk');
      await tester.tap(find.widgetWithText(FilledButton, 'Add'));
      await tester.pumpAndSettle();

      expect(teaching.addedVideos.single, ('https://youtu.be/abcdefghijk', ''));
      expect(find.textContaining("doesn't allow it to play in other apps"), findsWidgets);
      expect(find.byType(AlertDialog), findsOneWidget);
    });

    testWidgets('an accepted video closes the dialog and appears with its duration', (tester) async {
      final teaching = await openDialog(tester);

      await tester.enterText(find.widgetWithText(TextField, 'YouTube link'), 'abcdefghijk');
      await tester.enterText(find.widgetWithText(TextField, 'Title (optional)'), 'Forces explained');
      await tester.tap(find.widgetWithText(FilledButton, 'Add'));
      await tester.pumpAndSettle();

      expect(teaching.addedVideos.single, ('abcdefghijk', 'Forces explained'));
      expect(find.byType(AlertDialog), findsNothing);
      expect(find.text('Forces explained'), findsOneWidget);
      expect(find.text('12:05'), findsOneWidget);
    });
  });
}
