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
  link;

  static ResourceType fromName(String name) => name == 'Document' ? document : link;
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
  });

  factory LessonResource.fromJson(Map<String, dynamic> json) => LessonResource(
    id: json['id'] as String,
    type: ResourceType.fromName(json['type'] as String),
    title: json['title'] as String,
    isRequired: json['isRequired'] as bool,
    url: json['url'] as String?,
    fileName: json['fileName'] as String?,
    sizeBytes: json['sizeBytes'] as int?,
  );

  final String id;
  final ResourceType type;
  final String title;
  final bool isRequired;
  final String? url;
  final String? fileName;
  final int? sizeBytes;

  @override
  List<Object?> get props => [id, type, title, isRequired, url, fileName, sizeBytes];
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
