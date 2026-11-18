import 'package:equatable/equatable.dart';

import '../../../core/api/api_result.dart';
import '../../../core/auth/auth_tokens.dart';
import 'app_user.dart';

/// A signed-in session: tokens plus the user's profile.
class AuthSession extends Equatable {
  const AuthSession({required this.tokens, required this.user});

  final AuthTokens tokens;
  final AppUser user;

  @override
  List<Object?> get props => [tokens, user];
}

class StudentRegistration {
  const StudentRegistration({
    required this.email,
    required this.password,
    required this.displayName,
    required this.preferredLanguage,
    required this.timeZoneId,
    required this.countryCode,
    required this.curriculumId,
    required this.gradeLevelId,
    required this.subjectIds,
    this.streamId,
  });

  final String email;
  final String password;
  final String displayName;
  final String preferredLanguage;
  final String timeZoneId;
  final String countryCode;
  final int curriculumId;
  final int gradeLevelId;
  final int? streamId;
  final List<int> subjectIds;
}

class TeacherRegistration {
  const TeacherRegistration({
    required this.email,
    required this.password,
    required this.displayName,
    required this.preferredLanguage,
    required this.timeZoneId,
    required this.countryCode,
    required this.headline,
  });

  final String email;
  final String password;
  final String displayName;
  final String preferredLanguage;
  final String timeZoneId;
  final String countryCode;
  final String headline;
}

/// Model-layer boundary for authentication and the user's own profile.
abstract interface class AuthRepository {
  Future<ApiResult<AuthSession>> registerStudent(StudentRegistration registration);

  Future<ApiResult<AuthSession>> registerTeacher(TeacherRegistration registration);

  Future<ApiResult<AuthSession>> signIn({required String email, required String password});

  Future<ApiResult<AuthSession>> refresh(String refreshToken);

  /// Best effort: the local session is cleared even if this fails.
  Future<void> signOut(String refreshToken);
}
