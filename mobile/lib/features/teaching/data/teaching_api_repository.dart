import '../../../core/api/api_client.dart';
import '../../../core/api/api_result.dart';
import '../../../core/api/paged.dart';
import '../../content/models/content_models.dart';
import '../models/teaching_models.dart';

/// [TeachingRepository] backed by `/api/v1/teacher/*`.
class TeachingApiRepository implements TeachingRepository {
  TeachingApiRepository(this._client);

  static const _base = '/api/v1/teacher';

  final ApiClient _client;

  @override
  Future<ApiResult<List<TeacherCourseSummary>>> listCourses() => _client.get(
    '$_base/courses',
    (data) => (data! as List<Object?>).cast<Map<String, dynamic>>().map(_summary).toList(growable: false),
  );

  @override
  Future<ApiResult<TeacherCourse>> createCourse(NewCourse course) => _client.post('$_base/courses', {
    'title': course.title.trim(),
    'description': course.description.trim(),
    'contentLanguage': course.contentLanguage,
    'curriculumId': course.curriculumId,
    'gradeLevelId': course.gradeLevelId,
    'subjectId': course.subjectId,
    'academicYearId': course.academicYearId,
  }, _course);

  @override
  Future<ApiResult<TeacherCourse>> getCourse(String courseId) => _client.get('$_base/courses/$courseId', _course);

  @override
  Future<ApiResult<TeacherCourse>> changeCourseStatus(String courseId, ContentStatus status) =>
      _client.put('$_base/courses/$courseId/status', {'status': status.apiName}, _course);

  @override
  Future<ApiResult<TeacherCourse>> addUnit(String courseId, String title) =>
      _client.post('$_base/courses/$courseId/units', {'title': title.trim(), 'description': ''}, _course);

  @override
  Future<ApiResult<TeacherCourse>> changeUnitStatus(String unitId, ContentStatus status) =>
      _client.put('$_base/units/$unitId/status', {'status': status.apiName}, _course);

  @override
  Future<ApiResult<TeacherCourse>> deleteUnit(String unitId) => _client.delete('$_base/units/$unitId', _course);

  @override
  Future<ApiResult<TeacherCourse>> addLesson(String unitId, String title) => _client.post(
    '$_base/units/$unitId/lessons',
    {'title': title.trim(), 'summary': '', 'notes': '', 'estimatedMinutes': null},
    _course,
  );

  @override
  Future<ApiResult<TeacherCourse>> changeLessonStatus(String lessonId, ContentStatus status) =>
      _client.put('$_base/lessons/$lessonId/status', {'status': status.apiName}, _course);

  @override
  Future<ApiResult<TeacherCourse>> deleteLesson(String lessonId) => _client.delete('$_base/lessons/$lessonId', _course);

  @override
  Future<ApiResult<LessonDetail>> getLesson(String lessonId) => _client.get('$_base/lessons/$lessonId', _lesson);

  @override
  Future<ApiResult<LessonDetail>> updateLesson(String lessonId, LessonEdit edit) =>
      _client.put('$_base/lessons/$lessonId', {
        'title': edit.title.trim(),
        'summary': edit.summary.trim(),
        'notes': edit.notes,
        'estimatedMinutes': edit.estimatedMinutes,
      }, _lesson);

  @override
  Future<ApiResult<LessonDetail>> addLink(String lessonId, {required String title, required String url}) =>
      _client.post('$_base/lessons/$lessonId/resources/link', {
        'title': title.trim(),
        'url': url.trim(),
        'isRequired': false,
      }, _lesson);

  @override
  Future<ApiResult<LessonDetail>> addVideo(String lessonId, {required String url, String? title}) => _client.post(
    '$_base/lessons/$lessonId/resources/video',
    {'url': url.trim(), 'title': (title?.trim().isEmpty ?? true) ? null : title!.trim(), 'isRequired': true},
    _lesson,
  );

  @override
  Future<ApiResult<LessonDetail>> deleteResource(String resourceId) =>
      _client.delete('$_base/resources/$resourceId', _lesson);

  @override
  Future<ApiResult<Paged<TeacherEnrollment>>> listEnrollments({required EnrollmentStatus status, int page = 1}) =>
      _client.get(
        '$_base/enrollments',
        (data) => Paged.decode(data, _enrollment),
        query: {'status': status.apiName, 'page': page},
      );

  @override
  Future<ApiResult<TeacherEnrollment>> decide(String enrollmentId, EnrollmentDecision decision, {String? note}) =>
      _client.post('$_base/enrollments/$enrollmentId/${decision.name}', {'note': note}, _enrollment);

  static TeacherCourseSummary _summary(Map<String, dynamic> json) => TeacherCourseSummary(
    id: json['id'] as String,
    title: json['title'] as String,
    status: ContentStatus.fromName(json['status'] as String),
    subjectName: json['subjectName'] as String,
    gradeLevelName: json['gradeLevelName'] as String,
    unitCount: json['unitCount'] as int,
    lessonCount: json['lessonCount'] as int,
    pendingRequests: json['pendingRequests'] as int,
    activeStudents: json['activeStudents'] as int,
  );

  static TeacherCourse _course(Object? data) {
    final json = data! as Map<String, dynamic>;
    return TeacherCourse(
      id: json['id'] as String,
      title: json['title'] as String,
      description: json['description'] as String,
      status: ContentStatus.fromName(json['status'] as String),
      subjectName: json['subjectName'] as String,
      gradeLevelName: json['gradeLevelName'] as String,
      units: (json['units'] as List<Object?>)
          .cast<Map<String, dynamic>>()
          .map((unit) {
            return TeacherUnit(
              id: unit['id'] as String,
              title: unit['title'] as String,
              status: ContentStatus.fromName(unit['status'] as String),
              lessons: (unit['lessons'] as List<Object?>)
                  .cast<Map<String, dynamic>>()
                  .map((lesson) {
                    return TeacherLesson(
                      id: lesson['id'] as String,
                      title: lesson['title'] as String,
                      status: ContentStatus.fromName(lesson['status'] as String),
                      resourceCount: lesson['resourceCount'] as int,
                    );
                  })
                  .toList(growable: false),
            );
          })
          .toList(growable: false),
    );
  }

  static LessonDetail _lesson(Object? data) => LessonDetail.fromJson(data! as Map<String, dynamic>);

  static TeacherEnrollment _enrollment(Object? data) {
    final json = data! as Map<String, dynamic>;
    return TeacherEnrollment(
      id: json['id'] as String,
      courseTitle: json['courseTitle'] as String,
      studentName: json['studentName'] as String,
      studentGradeName: json['studentGradeName'] as String,
      status: EnrollmentStatus.fromName(json['status'] as String),
      requestMessage: json['requestMessage'] as String?,
    );
  }
}
