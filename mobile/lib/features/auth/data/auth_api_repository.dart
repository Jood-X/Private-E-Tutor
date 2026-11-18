import '../../../core/api/api_client.dart';
import '../../../core/api/api_result.dart';
import '../../../core/auth/auth_tokens.dart';
import '../models/app_user.dart';
import '../models/auth_repository.dart';

/// [AuthRepository] backed by `/api/v1/auth/*`. Uses a client without the auth interceptor, since these
/// endpoints are how tokens are obtained.
class AuthApiRepository implements AuthRepository {
  AuthApiRepository(this._client);

  static const _base = '/api/v1/auth';

  final ApiClient _client;

  @override
  Future<ApiResult<AuthSession>> registerStudent(StudentRegistration r) => _client.post('$_base/register/student', {
    'email': r.email.trim(),
    'password': r.password,
    'displayName': r.displayName.trim(),
    'preferredLanguage': r.preferredLanguage,
    'timeZoneId': r.timeZoneId,
    'countryCode': r.countryCode,
    'curriculumId': r.curriculumId,
    'gradeLevelId': r.gradeLevelId,
    'streamId': r.streamId,
    'subjectIds': r.subjectIds,
  }, decodeSession);

  @override
  Future<ApiResult<AuthSession>> registerTeacher(TeacherRegistration r) => _client.post('$_base/register/teacher', {
    'email': r.email.trim(),
    'password': r.password,
    'displayName': r.displayName.trim(),
    'preferredLanguage': r.preferredLanguage,
    'timeZoneId': r.timeZoneId,
    'countryCode': r.countryCode,
    'headline': r.headline.trim(),
  }, decodeSession);

  @override
  Future<ApiResult<AuthSession>> signIn({required String email, required String password}) =>
      _client.post('$_base/login', {'email': email.trim(), 'password': password}, decodeSession);

  @override
  Future<ApiResult<AuthSession>> refresh(String refreshToken) =>
      _client.post('$_base/refresh', {'refreshToken': refreshToken}, decodeSession);

  @override
  Future<void> signOut(String refreshToken) async {
    await _client.postNoContent('$_base/logout', {'refreshToken': refreshToken});
  }

  static AuthSession decodeSession(Object? data) {
    final json = data! as Map<String, dynamic>;
    return AuthSession(
      tokens: AuthTokens(accessToken: json['accessToken'] as String, refreshToken: json['refreshToken'] as String),
      user: decodeUser(json['user']),
    );
  }

  static AppUser decodeUser(Object? data) {
    final json = data! as Map<String, dynamic>;
    final student = json['student'] as Map<String, dynamic>?;
    final teacher = json['teacher'] as Map<String, dynamic>?;
    return AppUser(
      id: json['id'] as String,
      email: json['email'] as String,
      role: UserRole.fromName(json['role'] as String),
      displayName: json['displayName'] as String,
      preferredLanguage: json['preferredLanguage'] as String,
      timeZoneId: json['timeZoneId'] as String,
      countryCode: json['countryCode'] as String,
      student:
          student == null
              ? null
              : StudentStudy(
                curriculumId: student['curriculumId'] as int,
                gradeLevelId: student['gradeLevelId'] as int,
                streamId: student['streamId'] as int?,
                subjectIds: (student['subjectIds'] as List<Object?>).cast<int>(),
              ),
      teacher:
          teacher == null
              ? null
              : TeacherInfo(
                headline: teacher['headline'] as String,
                bio: teacher['bio'] as String,
                verification: TeacherVerification.fromName(teacher['verificationStatus'] as String),
                isAcceptingStudents: teacher['isAcceptingStudents'] as bool,
              ),
    );
  }
}
