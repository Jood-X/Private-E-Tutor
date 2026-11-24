import 'package:etutor_app/core/api/api_failure.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/content/models/content_models.dart';
import 'package:etutor_app/features/teaching/controllers/enrollment_requests_cubit.dart';
import 'package:etutor_app/features/teaching/models/teaching_models.dart';
import 'package:etutor_app/features/teaching/views/enrollment_requests_screen.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_content_repositories.dart';
import '../../helpers/pump_screen.dart';

void main() {
  late FakeTeachingRepository teaching;

  setUp(() {
    teaching =
        FakeTeachingRepository()
          ..enrollmentPages[EnrollmentStatus.pending] = [
            [pendingRequest('e-1'), pendingRequest('e-2', student: 'Omar')],
            [pendingRequest('e-3', student: 'Sara')],
          ];
  });

  test('pages load in order and a decided request leaves the list', () async {
    final cubit = EnrollmentListCubit(repository: teaching, status: EnrollmentStatus.pending);
    await cubit.load();
    await cubit.loadMore();
    expect(cubit.state.items.value!.map((e) => e.id), ['e-1', 'e-2', 'e-3']);
    expect(cubit.state.hasMore, isFalse);

    expect(await cubit.decide('e-2', EnrollmentDecision.approve), isNull);
    expect(cubit.state.items.value!.map((e) => e.id), ['e-1', 'e-3']);
    await cubit.close();
  });

  test('a failed decision keeps the request and returns the failure', () async {
    const conflict = ApiFailure(kind: ApiFailureKind.conflict, code: 'enrollment.invalid_transition', statusCode: 409);
    teaching.decideResult = const ApiError(conflict);
    final cubit = EnrollmentListCubit(repository: teaching, status: EnrollmentStatus.pending);
    await cubit.load();

    expect(await cubit.decide('e-1', EnrollmentDecision.approve), conflict);
    expect(cubit.state.items.value!.length, 2);
    await cubit.close();
  });

  testWidgets('approve and decline (with a note) from the requests screen', (tester) async {
    await pumpScreen(
      tester,
      EnrollmentRequestsScreen(createController: (status) => EnrollmentListCubit(repository: teaching, status: status)),
    );

    expect(find.text('Lina'), findsOneWidget);
    expect(find.text('“Please accept me”'), findsNWidgets(2));

    await tester.tap(find.widgetWithText(FilledButton, 'Approve').first);
    await tester.pumpAndSettle();
    expect(teaching.decisions.single, ('e-1', EnrollmentDecision.approve, null));
    expect(find.text('Lina'), findsNothing);

    await tester.tap(find.widgetWithText(OutlinedButton, 'Decline'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextField), 'Course is full');
    await tester.tap(find.widgetWithText(FilledButton, 'Decline'));
    await tester.pumpAndSettle();

    expect(teaching.decisions.last, ('e-2', EnrollmentDecision.reject, 'Course is full'));
    expect(find.text('Omar'), findsNothing);
  });
}
