import 'package:etutor_app/app/routes.dart';
import 'package:etutor_app/features/auth/controllers/session_cubit.dart';
import 'package:etutor_app/features/auth/data/auth_api_repository.dart';
import 'package:etutor_app/features/auth/models/app_user.dart';
import 'package:etutor_app/features/auth/models/credential_rules.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('CredentialRules', () {
    test('email', () {
      expect(CredentialRules.email(''), InputProblem.required);
      expect(CredentialRules.email('lina@'), InputProblem.email);
      expect(CredentialRules.email(' lina@etutor.test '), isNull);
    });

    test('new password mirrors the server policy (8+, letter and digit)', () {
      expect(CredentialRules.newPassword(''), InputProblem.required);
      expect(CredentialRules.newPassword('abc1'), InputProblem.password);
      expect(CredentialRules.newPassword('abcdefgh'), InputProblem.password);
      expect(CredentialRules.newPassword('12345678'), InputProblem.password);
      expect(CredentialRules.newPassword('abcdefg1'), isNull);
      expect(CredentialRules.newPassword('كلمةسرية1'), isNull, reason: 'Arabic letters count as letters');
    });

    test('sign-in password only needs to be present', () {
      expect(CredentialRules.existingPassword(''), InputProblem.required);
      expect(CredentialRules.existingPassword('x'), isNull);
    });

    test('display name and headline lengths', () {
      expect(CredentialRules.displayName(' A '), InputProblem.name);
      expect(CredentialRules.displayName('Lina'), isNull);
      expect(CredentialRules.headline('ab'), InputProblem.headline);
      expect(CredentialRules.headline('Physics teacher'), isNull);
    });
  });

  group('redirectFor', () {
    test('everything goes to the splash while the session is being checked', () {
      expect(redirectFor(SessionStatus.restoring, AppRoutes.home), AppRoutes.splash);
      expect(redirectFor(SessionStatus.unavailable, AppRoutes.welcome), AppRoutes.splash);
      expect(redirectFor(SessionStatus.restoring, AppRoutes.splash), isNull);
    });

    test('signed-out users cannot reach signed-in screens', () {
      expect(redirectFor(SessionStatus.signedOut, AppRoutes.home), AppRoutes.welcome);
      expect(redirectFor(SessionStatus.signedOut, AppRoutes.studentDetails), isNull);
      expect(redirectFor(SessionStatus.signedOut, AppRoutes.settings), isNull);
    });

    test('signed-in users skip sign-in and sign-up screens', () {
      expect(redirectFor(SessionStatus.signedIn, AppRoutes.signIn), AppRoutes.home);
      expect(redirectFor(SessionStatus.signedIn, AppRoutes.studentSignup), AppRoutes.home);
      expect(redirectFor(SessionStatus.signedIn, AppRoutes.settings), isNull);
    });
  });

  test('decodes the API user, including role and teacher verification', () {
    final user = AuthApiRepository.decodeUser({
      'id': 'u-2',
      'email': 'khaled@etutor.test',
      'role': 'Teacher',
      'displayName': 'Khaled',
      'preferredLanguage': 'ar',
      'timeZoneId': 'Asia/Amman',
      'countryCode': 'JO',
      'student': null,
      'teacher': {'headline': 'Physics', 'bio': '', 'verificationStatus': 'Verified', 'isAcceptingStudents': true},
    });

    expect(user.role, UserRole.teacher);
    expect(user.teacher!.verification, TeacherVerification.verified);
    expect(user.student, isNull);
  });
}
