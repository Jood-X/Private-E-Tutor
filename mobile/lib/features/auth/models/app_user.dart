import 'package:equatable/equatable.dart';

enum UserRole {
  student,
  teacher,
  unknown;

  static UserRole fromName(String name) => switch (name) {
    'Student' => student,
    'Teacher' => teacher,
    _ => unknown,
  };
}

enum TeacherVerification {
  pending,
  verified,
  rejected;

  static TeacherVerification fromName(String name) => switch (name) {
    'Verified' => verified,
    'Rejected' => rejected,
    _ => pending,
  };
}

class StudentStudy extends Equatable {
  const StudentStudy({required this.curriculumId, required this.gradeLevelId, required this.subjectIds, this.streamId});

  final int curriculumId;
  final int gradeLevelId;
  final int? streamId;
  final List<int> subjectIds;

  @override
  List<Object?> get props => [curriculumId, gradeLevelId, streamId, subjectIds];
}

class TeacherInfo extends Equatable {
  const TeacherInfo({
    required this.headline,
    required this.bio,
    required this.verification,
    required this.isAcceptingStudents,
  });

  final String headline;
  final String bio;
  final TeacherVerification verification;
  final bool isAcceptingStudents;

  @override
  List<Object?> get props => [headline, bio, verification, isAcceptingStudents];
}

/// The signed-in user's own profile, as returned by the API (`MeDto`).
class AppUser extends Equatable {
  const AppUser({
    required this.id,
    required this.email,
    required this.role,
    required this.displayName,
    required this.preferredLanguage,
    required this.timeZoneId,
    required this.countryCode,
    this.student,
    this.teacher,
  });

  final String id;
  final String email;
  final UserRole role;
  final String displayName;
  final String preferredLanguage;
  final String timeZoneId;
  final String countryCode;
  final StudentStudy? student;
  final TeacherInfo? teacher;

  @override
  List<Object?> get props => [
    id,
    email,
    role,
    displayName,
    preferredLanguage,
    timeZoneId,
    countryCode,
    student,
    teacher,
  ];
}
