import 'package:equatable/equatable.dart';

/// Shared by the teacher and student sides: statuses, lesson detail and resources.
enum ContentStatus {
  draft,
  published,
  archived;

  static ContentStatus fromName(String name) => switch (name) {
    'Published' => published,
    'Archived' => archived,
    _ => draft,
  };

  /// The API's enum name (PascalCase), used in request bodies.
  String get apiName => switch (this) {
    draft => 'Draft',
    published => 'Published',
    archived => 'Archived',
  };
}

enum EnrollmentStatus {
  pending,
  approved,
  rejected,
  withdrawn,
  revoked;

  static EnrollmentStatus fromName(String name) =>
      values.firstWhere((status) => status.name == name.toLowerCase(), orElse: () => pending);

  String get apiName => '${name[0].toUpperCase()}${name.substring(1)}';
}

enum ResourceType {
  document,
  link,
  video;

  static ResourceType fromName(String name) => switch (name) {
    'Document' => document,
    'Video' => video,
    _ => link,
  };
}

/// Display details of a video resource. Playback details come from a separate, authorized endpoint.
class VideoInfo extends Equatable {
  const VideoInfo({required this.isAvailable, this.durationSeconds, this.thumbnailUrl});

  factory VideoInfo.fromJson(Map<String, dynamic> json) => VideoInfo(
    durationSeconds: json['durationSeconds'] as int?,
    thumbnailUrl: json['thumbnailUrl'] as String?,
    isAvailable: json['status'] == 'Ready',
  );

  final int? durationSeconds;
  final String? thumbnailUrl;
  final bool isAvailable;

  @override
  List<Object?> get props => [durationSeconds, thumbnailUrl, isAvailable];
}

/// What the in-app player needs to play one video resource.
class VideoPlayback extends Equatable {
  const VideoPlayback({
    required this.resourceId,
    required this.provider,
    required this.assetId,
    required this.title,
    this.durationSeconds,
  });

  factory VideoPlayback.fromJson(Map<String, dynamic> json) => VideoPlayback(
    resourceId: json['resourceId'] as String,
    provider: json['provider'] as String,
    assetId: json['assetId'] as String,
    title: json['title'] as String,
    durationSeconds: json['durationSeconds'] as int?,
  );

  final String resourceId;
  final String provider;
  final String assetId;
  final String title;
  final int? durationSeconds;

  @override
  List<Object?> get props => [resourceId, provider, assetId, title, durationSeconds];
}

/// "4:05" or "1:02:03" (Western digits in both languages, decision D15).
String formatDuration(int seconds) {
  final hours = seconds ~/ 3600;
  final minutes = (seconds % 3600) ~/ 60;
  final secs = (seconds % 60).toString().padLeft(2, '0');
  return hours > 0 ? '$hours:${minutes.toString().padLeft(2, '0')}:$secs' : '$minutes:$secs';
}

class LessonResource extends Equatable {
  const LessonResource({
    required this.id,
    required this.type,
    required this.title,
    required this.isRequired,
    this.url,
    this.fileName,
    this.sizeBytes,
    this.video,
  });

  factory LessonResource.fromJson(Map<String, dynamic> json) => LessonResource(
    id: json['id'] as String,
    type: ResourceType.fromName(json['type'] as String),
    title: json['title'] as String,
    isRequired: json['isRequired'] as bool,
    url: json['url'] as String?,
    fileName: json['fileName'] as String?,
    sizeBytes: json['sizeBytes'] as int?,
    video: switch (json['video']) {
      final Map<String, dynamic> video => VideoInfo.fromJson(video),
      _ => null,
    },
  );

  final String id;
  final ResourceType type;
  final String title;
  final bool isRequired;
  final String? url;
  final String? fileName;
  final int? sizeBytes;
  final VideoInfo? video;

  @override
  List<Object?> get props => [id, type, title, isRequired, url, fileName, sizeBytes, video];
}

class LessonDetail extends Equatable {
  const LessonDetail({
    required this.id,
    required this.courseId,
    required this.title,
    required this.summary,
    required this.notes,
    required this.status,
    required this.resources,
    this.estimatedMinutes,
  });

  factory LessonDetail.fromJson(Map<String, dynamic> json) => LessonDetail(
    id: json['id'] as String,
    courseId: json['courseId'] as String,
    title: json['title'] as String,
    summary: json['summary'] as String,
    notes: json['notes'] as String,
    status: ContentStatus.fromName(json['status'] as String),
    estimatedMinutes: json['estimatedMinutes'] as int?,
    resources: (json['resources'] as List<Object?>)
        .cast<Map<String, dynamic>>()
        .map(LessonResource.fromJson)
        .toList(growable: false),
  );

  final String id;
  final String courseId;
  final String title;
  final String summary;
  final String notes;
  final ContentStatus status;
  final int? estimatedMinutes;
  final List<LessonResource> resources;

  @override
  List<Object?> get props => [id, courseId, title, summary, notes, status, estimatedMinutes, resources];
}
