import '../../../core/api/api_client.dart';
import '../../../core/api/api_result.dart';
import '../../../core/api/paged.dart';
import '../../content/models/content_models.dart';
import '../models/learning_models.dart';

/// [LearningRepository] backed by the student endpoints (`/api/v1/teachers`, `/api/v1/learning/*`).
class LearningApiRepository implements LearningRepository {
  LearningApiRepository(this._client);

  static const _base = '/api/v1';

  final ApiClient _client;

  @override
  Future<ApiResult<Paged<TeacherCard>>> searchTeachers(TeacherQuery query, {int page = 1}) => _client.get(
    '$_base/teachers',
    (data) => Paged.decode(data, _teacherCard),
    query: {
      if (query.gradeLevelId != null) 'gradeLevelId': query.gradeLevelId,
      if (query.subjectId != null) 'subjectId': query.subjectId,
      if (query.search.trim().isNotEmpty) 'search': query.search.trim(),
      'page': page,
    },
  );

  @override
  Future<ApiResult<TeacherDetail>> getTeacher(String teacherId) => _client.get('$_base/teachers/$teacherId', (data) {
    final json = data! as Map<String, dynamic>;
    return TeacherDetail(
      id: json['id'] as String,
      displayName: json['displayName'] as String,
      headline: json['headline'] as String,
      bio: json['bio'] as String,
      isAcceptingStudents: json['isAcceptingStudents'] as bool,
      courses: (json['courses'] as List<Object?>)
          .cast<Map<String, dynamic>>()
          .map(_publicCourse)
          .toList(growable: false),
    );
  });

  @override
  Future<ApiResult<MyEnrollment>> requestEnrollment(String courseId, {String? message}) => _client.post(
    '$_base/courses/$courseId/enrollments',
    {'message': (message?.trim().isEmpty ?? true) ? null : message!.trim()},
    _myEnrollment,
  );

  @override
  Future<ApiResult<List<MyEnrollment>>> myEnrollments() => _client.get(
    '$_base/learning/enrollments',
    (data) => (data! as List<Object?>).map(_myEnrollment).toList(growable: false),
  );

  @override
  Future<ApiResult<MyEnrollment>> withdraw(String enrollmentId) =>
      _client.post('$_base/learning/enrollments/$enrollmentId/withdraw', const <String, Object?>{}, _myEnrollment);

  @override
  Future<ApiResult<StudentCourse>> getCourse(String courseId) =>
      _client.get('$_base/learning/courses/$courseId', (data) {
        final json = data! as Map<String, dynamic>;
        final status = json['enrollmentStatus'] as String?;
        return StudentCourse(
          id: json['id'] as String,
          title: json['title'] as String,
          description: json['description'] as String,
          subjectName: json['subjectName'] as String,
          gradeLevelName: json['gradeLevelName'] as String,
          teacherId: json['teacherId'] as String,
          teacherName: json['teacherName'] as String,
          enrollmentId: json['enrollmentId'] as String?,
          enrollmentStatus: status == null ? null : EnrollmentStatus.fromName(status),
          units: (json['units'] as List<Object?>?)
              ?.cast<Map<String, dynamic>>()
              .map(
                (unit) => StudentUnit(
                  id: unit['id'] as String,
                  title: unit['title'] as String,
                  lessons: (unit['lessons'] as List<Object?>)
                      .cast<Map<String, dynamic>>()
                      .map(
                        (lesson) => StudentLesson(
                          id: lesson['id'] as String,
                          title: lesson['title'] as String,
                          summary: lesson['summary'] as String,
                          estimatedMinutes: lesson['estimatedMinutes'] as int?,
                        ),
                      )
                      .toList(growable: false),
                ),
              )
              .toList(growable: false),
        );
      });

  @override
  Future<ApiResult<LessonDetail>> getLesson(String lessonId) =>
      _client.get('$_base/learning/lessons/$lessonId', (data) => LessonDetail.fromJson(data! as Map<String, dynamic>));

  @override
  Future<ApiResult<VideoPlayback>> getPlayback(String resourceId) => _client.get(
    '$_base/resources/$resourceId/playback',
    (data) => VideoPlayback.fromJson(data! as Map<String, dynamic>),
  );

  static TeacherCard _teacherCard(Map<String, dynamic> json) => TeacherCard(
    id: json['id'] as String,
    displayName: json['displayName'] as String,
    headline: json['headline'] as String,
    subjects: (json['subjects'] as List<Object?>).cast<String>(),
    courseCount: json['courseCount'] as int,
  );

  static PublicCourse _publicCourse(Map<String, dynamic> json) {
    final status = json['myEnrollmentStatus'] as String?;
    return PublicCourse(
      id: json['id'] as String,
      title: json['title'] as String,
      description: json['description'] as String,
      subjectName: json['subjectName'] as String,
      gradeLevelName: json['gradeLevelName'] as String,
      unitCount: json['unitCount'] as int,
      lessonCount: json['lessonCount'] as int,
      myStatus: status == null ? null : EnrollmentStatus.fromName(status),
    );
  }

  static MyEnrollment _myEnrollment(Object? data) {
    final json = data! as Map<String, dynamic>;
    return MyEnrollment(
      id: json['id'] as String,
      courseId: json['courseId'] as String,
      courseTitle: json['courseTitle'] as String,
      subjectName: json['subjectName'] as String,
      teacherName: json['teacherName'] as String,
      status: EnrollmentStatus.fromName(json['status'] as String),
      decisionNote: json['decisionNote'] as String?,
    );
  }
}
