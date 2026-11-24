import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/content/models/content_models.dart';
import 'package:etutor_app/features/learning/controllers/learning_cubits.dart';
import 'package:etutor_app/features/learning/models/learning_models.dart';
import 'package:etutor_app/features/learning/views/student_course_screen.dart';
import 'package:etutor_app/features/learning/views/student_lesson_screen.dart';
import 'package:etutor_app/features/learning/views/teacher_profile_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_content_repositories.dart';
import '../../helpers/pump_screen.dart';

void main() {
  late FakeLearningRepository learning;

  setUp(() => learning = FakeLearningRepository());

  testWidgets('requesting to join a course sends the message and shows it as pending', (tester) async {
    await pumpScreen(
      tester,
      TeacherProfileScreen(createController: () => TeacherProfileCubit(repository: learning, teacherId: 't-1')),
    );
    expect(find.text('Ten years of Tawjihi physics.'), findsOneWidget);

    await tester.tap(find.widgetWithText(FilledButton, 'Request to join'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'I am in Grade 12');
    await tester.tap(find.text('Send request'));
    await tester.pumpAndSettle();

    expect(learning.requests.single, ('c-1', 'I am in Grade 12'));
    expect(find.text('Pending'), findsOneWidget);
    expect(find.widgetWithText(FilledButton, 'Request to join'), findsNothing);
  });

  testWidgets('a pending course shows the waiting banner instead of lessons', (tester) async {
    learning.courseResult = const ApiSuccess(
      StudentCourse(
        id: 'c-1',
        title: 'Physics 12',
        description: '',
        subjectName: 'Physics',
        gradeLevelName: 'Grade 12',
        teacherId: 't-1',
        teacherName: 'Khaled',
        enrollmentId: 'e-1',
        enrollmentStatus: EnrollmentStatus.pending,
      ),
    );
    await pumpScreen(
      tester,
      StudentCourseScreen(createController: () => StudentCourseCubit(repository: learning, courseId: 'c-1')),
    );

    expect(find.text("Your request is waiting for the teacher's approval."), findsOneWidget);
    expect(find.text('Withdraw'), findsOneWidget);
  });

  testWidgets('an approved course lists its units and lessons', (tester) async {
    learning.courseResult = const ApiSuccess(
      StudentCourse(
        id: 'c-1',
        title: 'Physics 12',
        description: '',
        subjectName: 'Physics',
        gradeLevelName: 'Grade 12',
        teacherId: 't-1',
        teacherName: 'Khaled',
        enrollmentId: 'e-1',
        enrollmentStatus: EnrollmentStatus.approved,
        units: [
          StudentUnit(
            id: 'u-1',
            title: 'Mechanics',
            lessons: [StudentLesson(id: 'l-1', title: "Newton's laws", summary: '', estimatedMinutes: 20)],
          ),
        ],
      ),
    );
    await pumpScreen(
      tester,
      StudentCourseScreen(createController: () => StudentCourseCubit(repository: learning, courseId: 'c-1')),
    );

    expect(find.text('Mechanics'), findsOneWidget);
    expect(find.text("Newton's laws"), findsOneWidget);
    expect(find.text('20 min'), findsOneWidget);
  });

  testWidgets('lesson links open externally; PDFs say viewing is coming soon', (tester) async {
    final opened = <Uri>[];
    await pumpScreen(
      tester,
      StudentLessonScreen(
        createController: () => StudentLessonCubit(repository: learning, lessonId: 'l-1'),
        openLink: (uri) async {
          opened.add(uri);
          return true;
        },
      ),
    );
    expect(find.text('F = ma'), findsOneWidget);

    await tester.tap(find.text('Simulation'));
    await tester.pumpAndSettle();
    expect(opened.single.toString(), 'https://phet.colorado.edu/sim');

    await tester.tap(find.text('Worksheet'));
    await tester.pumpAndSettle();
    expect(find.text('Viewing PDFs in the app is coming soon.'), findsOneWidget);
  });
}
