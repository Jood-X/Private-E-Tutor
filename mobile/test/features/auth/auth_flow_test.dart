import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/auth/controllers/session_cubit.dart';
import 'package:etutor_app/features/auth/models/auth_repository.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_auth_repository.dart';
import '../../helpers/test_app.dart';

Future<void> _tapText(WidgetTester tester, String text) async {
  await tester.ensureVisible(find.text(text).last);
  await tester.tap(find.text(text).last);
  await tester.pumpAndSettle();
}

Future<void> _enter(WidgetTester tester, String label, String value) async {
  final field = find.descendant(of: find.bySemanticsLabel(label), matching: find.byType(TextField));
  await tester.enterText(field.first, value);
}

void main() {
  testWidgets('a signed-out user starts on the welcome screen', (tester) async {
    await TestApp.pump(tester);

    expect(find.text('Learn with the right teacher'), findsOneWidget);
    expect(find.text("I'm a student"), findsOneWidget);
    expect(find.text("I'm a teacher"), findsOneWidget);

    // Regression: the logo tile was stretched to full width by the parent column.
    final logo = find.ancestor(of: find.byIcon(Icons.school_outlined), matching: find.byType(Container)).first;
    expect(tester.getSize(logo), const Size(64, 64));
  });

  testWidgets('student sign-up: 3 steps create the account and land on home', (tester) async {
    final app = await TestApp.pump(tester);

    await _tapText(tester, "I'm a student");
    await _tapText(tester, 'Jordan');
    await _tapText(tester, 'Jordanian Tawjihi');
    await _tapText(tester, 'Continue');

    expect(find.text('Tell us about your studies'), findsOneWidget);
    await _tapText(tester, 'Grade 12');
    await _tapText(tester, 'Scientific');
    await _tapText(tester, 'Physics');
    await _tapText(tester, 'Continue');

    expect(find.text('Create your account'), findsOneWidget);
    await _enter(tester, 'Your name', 'Lina');
    await _enter(tester, 'Email', 'lina@etutor.test');
    await _enter(tester, 'Password', 'Correct-horse-42');
    await _tapText(tester, 'Create account');

    final sent = app.auth.lastStudentRegistration!;
    expect((sent.countryCode, sent.curriculumId, sent.gradeLevelId, sent.streamId), ('JO', 1, 112, 1));
    expect(sent.subjectIds, [2]);
    expect(sent.timeZoneId, 'Asia/Amman', reason: 'defaults to the chosen country');
    expect(sent.preferredLanguage, 'en');
    expect(find.text('Hello, Lina'), findsOneWidget);
    expect(app.tokens.current, tokensA);
  });

  testWidgets('step 2 requires a stream when the curriculum has streams', (tester) async {
    await TestApp.pump(tester);
    await _tapText(tester, "I'm a student");
    await _tapText(tester, 'Jordan');
    await _tapText(tester, 'Jordanian Tawjihi');
    await _tapText(tester, 'Continue');

    await _tapText(tester, 'Grade 12');
    await _tapText(tester, 'Mathematics');
    final continueButton = find.widgetWithText(FilledButton, 'Continue');
    expect(tester.widget<FilledButton>(continueButton).onPressed, isNull);

    await _tapText(tester, 'Scientific');
    expect(tester.widget<FilledButton>(continueButton).onPressed, isNotNull);
  });

  testWidgets('sign-up shows inline validation before calling the server', (tester) async {
    final app = await TestApp.pump(tester);
    await _tapText(tester, "I'm a teacher");

    await _enter(tester, 'Email', 'not-an-email');
    await _enter(tester, 'Password', 'short');
    await _tapText(tester, 'Create account');

    expect(find.text('Enter a valid email address.'), findsOneWidget);
    expect(find.text('Use at least 8 characters, including a letter and a number.'), findsOneWidget);
    expect(app.auth.lastTeacherRegistration, isNull);
  });

  testWidgets('a duplicate email from the server is shown on the email field', (tester) async {
    final auth =
        FakeAuthRepository()
          ..registerResult = const ApiError(emailTaken); // field-level server error, as the API returns it
    await TestApp.pump(tester, auth: auth);
    await _tapText(tester, "I'm a teacher");

    await _enter(tester, 'Your name', 'Khaled');
    await _enter(tester, 'Headline', 'Physics teacher');
    await _tapText(tester, 'Jordan');
    await _enter(tester, 'Email', 'khaled@etutor.test');
    await _enter(tester, 'Password', 'Correct-horse-42');
    await _tapText(tester, 'Create account');

    expect(find.text('An account with this email already exists. Try signing in.'), findsOneWidget);
  });

  testWidgets('sign-in with a wrong password shows a form message and stays signed out', (tester) async {
    final auth = FakeAuthRepository()..signInResult = const ApiError(invalidCredentials);
    final app = await TestApp.pump(tester, auth: auth);
    await _tapText(tester, 'I already have an account');

    await _enter(tester, 'Email', 'lina@etutor.test');
    await _enter(tester, 'Password', 'wrong-password-1');
    await _tapText(tester, 'Sign in');

    expect(find.text('The email or password is incorrect.'), findsOneWidget);
    expect(app.session.state.status, SessionStatus.signedOut);
  });

  testWidgets('sign-in as a teacher adopts the profile language and shows the review banner', (tester) async {
    final auth = FakeAuthRepository()..signInResult = const ApiSuccess(AuthSession(tokens: tokensA, user: teacherUser));
    await TestApp.pump(tester, auth: auth);
    await _tapText(tester, 'I already have an account');

    await _enter(tester, 'Email', 'khaled@etutor.test');
    await _enter(tester, 'Password', 'Correct-horse-42');
    await _tapText(tester, 'Sign in');

    // teacherUser prefers Arabic, so the app switches to Arabic (RTL).
    expect(find.text('مرحبًا، Khaled'), findsOneWidget);
    expect(find.text('ملفك كمعلّم قيد المراجعة. لا يمكن للطلاب العثور عليك بعد.'), findsOneWidget);
  });

  testWidgets('a saved session is restored to home, and sign-out returns to welcome', (tester) async {
    final auth = FakeAuthRepository();
    final app = await TestApp.pump(tester, auth: auth, restoreSession: false);
    await app.tokens.save(tokensA);
    await app.session.restore();
    await tester.pumpAndSettle();

    expect(find.text('Hello, Lina'), findsOneWidget);

    await _tapText(tester, 'Sign out');

    expect(find.text('Learn with the right teacher'), findsOneWidget);
    expect(app.tokens.current, isNull);
    expect(auth.signedOutTokens, [tokensB.refreshToken], reason: 'the rotated token from restore is revoked');
  });
}
