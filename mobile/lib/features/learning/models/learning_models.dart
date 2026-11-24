import 'package:equatable/equatable.dart';

import '../../../core/api/api_result.dart';
import '../../../core/api/paged.dart';
import '../../content/models/content_models.dart';

/// A teacher in discovery results.
class TeacherCard extends Equatable {
  const TeacherCard({
    required this.id,
    required this.displayName,
    required this.headline,
    required this.subjects,
    required this.courseCount,
  });

  final String id;
  final String displayName;
  final String headline;
  final List<String> subjects;
  final int courseCount;

  @override
  List<Object?> get props => [id, displayName, headline, subjects, courseCount];
}

/// A published course as a student sees it before joining; [myStatus] is the student's active enrollment.
class PublicCourse extends Equatable {
  const PublicCourse({
    required this.id,
    required this.title,
    required this.description,
    required this.subjectName,
    required this.gradeLevelName,
    required this.unitCount,
    required this.lessonCount,
    this.myStatus,
  });

  final String id;
  final String title;
  final String description;
  final String subjectName;
  final String gradeLevelName;
  final int unitCount;
  final int lessonCount;
  final EnrollmentStatus? myStatus;

  PublicCourse withStatus(EnrollmentStatus status) => PublicCourse(
    id: id,
    title: title,
    description: description,
    subjectName: subjectName,
    gradeLevelName: gradeLevelName,
    unitCount: unitCount,
    lessonCount: lessonCount,
    myStatus: status,
  );

  @override
  List<Object?> get props => [id, title, description, subjectName, gradeLevelName, unitCount, lessonCount, myStatus];
}

class TeacherDetail extends Equatable {
  const TeacherDetail({
    required this.id,
    required this.displayName,
    required this.headline,
    required this.bio,
    required this.isAcceptingStudents,
    required this.courses,
  });

  final String id;
  final String displayName;
  final String headline;
  final String bio;
  final bool isAcceptingStudents;
  final List<PublicCourse> courses;

  TeacherDetail withCourse(PublicCourse course) => TeacherDetail(
    id: id,
    displayName: displayName,
    headline: headline,
    bio: bio,
    isAcceptingStudents: isAcceptingStudents,
    courses: [for (final existing in courses) existing.id == course.id ? course : existing],
  );

  @override
  List<Object?> get props => [id, displayName, headline, bio, isAcceptingStudents, courses];
}

/// One of the student's own enrollments ("My learning").
class MyEnrollment extends Equatable {
  const MyEnrollment({
    required this.id,
    required this.courseId,
    required this.courseTitle,
    required this.subjectName,
    required this.teacherName,
    required this.status,
    this.decisionNote,
  });

  final String id;
  final String courseId;
  final String courseTitle;
  final String subjectName;
  final String teacherName;
  final EnrollmentStatus status;
  final String? decisionNote;

  @override
  List<Object?> get props => [id, courseId, courseTitle, subjectName, teacherName, status, decisionNote];
}

class StudentLesson extends Equatable {
  const StudentLesson({required this.id, required this.title, required this.summary, this.estimatedMinutes});

  final String id;
  final String title;
  final String summary;
  final int? estimatedMinutes;

  @override
  List<Object?> get props => [id, title, summary, estimatedMinutes];
}

class StudentUnit extends Equatable {
  const StudentUnit({required this.id, required this.title, required this.lessons});

  final String id;
  final String title;
  final List<StudentLesson> lessons;

  @override
  List<Object?> get props => [id, title, lessons];
}

/// A course for a student: the summary always; [units] only once the enrollment is approved.
class StudentCourse extends Equatable {
  const StudentCourse({
    required this.id,
    required this.title,
    required this.description,
    required this.subjectName,
    required this.gradeLevelName,
    required this.teacherId,
    required this.teacherName,
    this.enrollmentId,
    this.enrollmentStatus,
    this.units,
  });

  final String id;
  final String title;
  final String description;
  final String subjectName;
  final String gradeLevelName;
  final String teacherId;
  final String teacherName;
  final String? enrollmentId;
  final EnrollmentStatus? enrollmentStatus;
  final List<StudentUnit>? units;

  @override
  List<Object?> get props => [
    id,
    title,
    description,
    subjectName,
    gradeLevelName,
    teacherId,
    teacherName,
    enrollmentId,
    enrollmentStatus,
    units,
  ];
}

class TeacherQuery extends Equatable {
  const TeacherQuery({this.gradeLevelId, this.subjectId, this.search = ''});

  final int? gradeLevelId;
  final int? subjectId;
  final String search;

  @override
  List<Object?> get props => [gradeLevelId, subjectId, search];
}

/// Model-layer boundary for everything a student does: discovery, enrollment and learning.
abstract interface class LearningRepository {
  Future<ApiResult<Paged<TeacherCard>>> searchTeachers(TeacherQuery query, {int page = 1});

  Future<ApiResult<TeacherDetail>> getTeacher(String teacherId);

  Future<ApiResult<MyEnrollment>> requestEnrollment(String courseId, {String? message});

  Future<ApiResult<List<MyEnrollment>>> myEnrollments();

  Future<ApiResult<MyEnrollment>> withdraw(String enrollmentId);

  Future<ApiResult<StudentCourse>> getCourse(String courseId);

  Future<ApiResult<LessonDetail>> getLesson(String lessonId);
}
