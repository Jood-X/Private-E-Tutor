import 'package:etutor_app/core/api/api_failure.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/core/auth/auth_tokens.dart';
import 'package:etutor_app/features/auth/models/app_user.dart';
import 'package:etutor_app/features/auth/models/auth_repository.dart';

const studentUser = AppUser(
  id: 'u-1',
  email: 'lina@etutor.test',
  role: UserRole.student,
  displayName: 'Lina',
  preferredLanguage: 'en',
  timeZoneId: 'Asia/Amman',
  countryCode: 'JO',
  student: StudentStudy(curriculumId: 1, gradeLevelId: 112, streamId: 1, subjectIds: [1, 2]),
);

const teacherUser = AppUser(
  id: 'u-2',
  email: 'khaled@etutor.test',
  role: UserRole.teacher,
  displayName: 'Khaled',
  preferredLanguage: 'ar',
  timeZoneId: 'Asia/Amman',
  countryCode: 'JO',
  teacher: TeacherInfo(
    headline: 'Physics',
    bio: '',
    verification: TeacherVerification.pending,
    isAcceptingStudents: true,
  ),
);

const tokensA = AuthTokens(accessToken: 'access-a', refreshToken: 'refresh-a');
const tokensB = AuthTokens(accessToken: 'access-b', refreshToken: 'refresh-b');

const invalidCredentials = ApiFailure(
  kind: ApiFailureKind.unauthorized,
  code: 'auth.invalid_credentials',
  statusCode: 401,
);
const emailTaken = ApiFailure(
  kind: ApiFailureKind.conflict,
  code: 'auth.email_taken',
  statusCode: 409,
  errors: [
    ApiFieldError(code: 'auth.email_taken', message: 'An account with this email already exists.', field: 'email'),
  ],
);
const sessionRejected = ApiFailure(
  kind: ApiFailureKind.unauthorized,
  code: 'auth.invalid_refresh_token',
  statusCode: 401,
);

/// In-memory [AuthRepository] whose answers each test can set; records what it received.
class FakeAuthRepository implements AuthRepository {
  ApiResult<AuthSession> signInResult = const ApiSuccess(AuthSession(tokens: tokensA, user: studentUser));
  ApiResult<AuthSession> registerResult = const ApiSuccess(AuthSession(tokens: tokensA, user: studentUser));
  ApiResult<AuthSession> refreshResult = const ApiSuccess(AuthSession(tokens: tokensB, user: studentUser));

  StudentRegistration? lastStudentRegistration;
  TeacherRegistration? lastTeacherRegistration;
  final List<String> signedOutTokens = [];
  int signInCalls = 0;

  @override
  Future<ApiResult<AuthSession>> registerStudent(StudentRegistration registration) async {
    lastStudentRegistration = registration;
    return registerResult;
  }

  @override
  Future<ApiResult<AuthSession>> registerTeacher(TeacherRegistration registration) async {
    lastTeacherRegistration = registration;
    return registerResult;
  }

  @override
  Future<ApiResult<AuthSession>> signIn({required String email, required String password}) async {
    signInCalls++;
    return signInResult;
  }

  @override
  Future<ApiResult<AuthSession>> refresh(String refreshToken) async => refreshResult;

  @override
  Future<void> signOut(String refreshToken) async => signedOutTokens.add(refreshToken);
}
