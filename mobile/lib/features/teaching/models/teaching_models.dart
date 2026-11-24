import 'package:equatable/equatable.dart';

import '../../../core/api/api_result.dart';
import '../../../core/api/paged.dart';
import '../../content/models/content_models.dart';

class TeacherCourseSummary extends Equatable {
  const TeacherCourseSummary({
    required this.id,
    required this.title,
    required this.status,
    required this.subjectName,
    required this.gradeLevelName,
    required this.unitCount,
    required this.lessonCount,
    required this.pendingRequests,
    required this.activeStudents,
  });

  final String id;
  final String title;
  final ContentStatus status;
  final String subjectName;
  final String gradeLevelName;
  final int unitCount;
  final int lessonCount;
  final int pendingRequests;
  final int activeStudents;

  @override
  List<Object?> get props => [
    id,
    title,
    status,
    subjectName,
    gradeLevelName,
    unitCount,
    lessonCount,
    pendingRequests,
    activeStudents,
  ];
}

class TeacherLesson extends Equatable {
  const TeacherLesson({required this.id, required this.title, required this.status, required this.resourceCount});

  final String id;
  final String title;
  final ContentStatus status;
  final int resourceCount;

  @override
  List<Object?> get props => [id, title, status, resourceCount];
}

class TeacherUnit extends Equatable {
  const TeacherUnit({required this.id, required this.title, required this.status, required this.lessons});

  final String id;
  final String title;
  final ContentStatus status;
  final List<TeacherLesson> lessons;

  @override
  List<Object?> get props => [id, title, status, lessons];
}

/// The full outline of a course, as its teacher sees it (all statuses).
class TeacherCourse extends Equatable {
  const TeacherCourse({
    required this.id,
    required this.title,
    required this.description,
    required this.status,
    required this.subjectName,
    required this.gradeLevelName,
    required this.units,
  });

  final String id;
  final String title;
  final String description;
  final ContentStatus status;
  final String subjectName;
  final String gradeLevelName;
  final List<TeacherUnit> units;

  @override
  List<Object?> get props => [id, title, description, status, subjectName, gradeLevelName, units];
}

class TeacherEnrollment extends Equatable {
  const TeacherEnrollment({
    required this.id,
    required this.courseTitle,
    required this.studentName,
    required this.studentGradeName,
    required this.status,
    this.requestMessage,
  });

  final String id;
  final String courseTitle;
  final String studentName;
  final String studentGradeName;
  final EnrollmentStatus status;
  final String? requestMessage;

  @override
  List<Object?> get props => [id, courseTitle, studentName, studentGradeName, status, requestMessage];
}

class NewCourse {
  const NewCourse({
    required this.title,
    required this.description,
    required this.contentLanguage,
    required this.curriculumId,
    required this.gradeLevelId,
    required this.subjectId,
    required this.academicYearId,
  });

  final String title;
  final String description;
  final String contentLanguage;
  final int curriculumId;
  final int gradeLevelId;
  final int subjectId;
  final int academicYearId;
}

class LessonEdit {
  const LessonEdit({required this.title, required this.summary, required this.notes, this.estimatedMinutes});

  final String title;
  final String summary;
  final String notes;
  final int? estimatedMinutes;
}

enum EnrollmentDecision { approve, reject, revoke }

/// Model-layer boundary for everything a teacher manages.
abstract interface class TeachingRepository {
  Future<ApiResult<List<TeacherCourseSummary>>> listCourses();

  Future<ApiResult<TeacherCourse>> createCourse(NewCourse course);

  Future<ApiResult<TeacherCourse>> getCourse(String courseId);

  Future<ApiResult<TeacherCourse>> changeCourseStatus(String courseId, ContentStatus status);

  Future<ApiResult<TeacherCourse>> addUnit(String courseId, String title);

  Future<ApiResult<TeacherCourse>> changeUnitStatus(String unitId, ContentStatus status);

  Future<ApiResult<TeacherCourse>> deleteUnit(String unitId);

  Future<ApiResult<TeacherCourse>> addLesson(String unitId, String title);

  Future<ApiResult<TeacherCourse>> changeLessonStatus(String lessonId, ContentStatus status);

  Future<ApiResult<TeacherCourse>> deleteLesson(String lessonId);

  Future<ApiResult<LessonDetail>> getLesson(String lessonId);

  Future<ApiResult<LessonDetail>> updateLesson(String lessonId, LessonEdit edit);

  Future<ApiResult<LessonDetail>> addLink(String lessonId, {required String title, required String url});

  /// [url] is a YouTube link or video ID; [title] defaults to the video's own title.
  Future<ApiResult<LessonDetail>> addVideo(String lessonId, {required String url, String? title});

  Future<ApiResult<LessonDetail>> deleteResource(String resourceId);

  Future<ApiResult<Paged<TeacherEnrollment>>> listEnrollments({required EnrollmentStatus status, int page = 1});

  Future<ApiResult<TeacherEnrollment>> decide(String enrollmentId, EnrollmentDecision decision, {String? note});
}
