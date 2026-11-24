import 'dart:async';

import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/core/api/paged.dart';
import 'package:etutor_app/core/state/load_status.dart';
import 'package:etutor_app/features/learning/controllers/discover_cubit.dart';
import 'package:etutor_app/features/learning/models/learning_models.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_catalog_repository.dart';
import '../../helpers/fake_content_repositories.dart';

Paged<TeacherCard> _page(List<TeacherCard> items, {int page = 1, int totalPages = 1}) =>
    Paged(items: items, page: page, totalPages: totalPages, totalCount: items.length);

void main() {
  late FakeLearningRepository learning;
  late DiscoverCubit cubit;

  setUp(() {
    learning = FakeLearningRepository();
    cubit = DiscoverCubit(
      learning: learning,
      catalog: FakeCatalogRepository(),
      gradeLevelId: 112,
      searchDelay: const Duration(milliseconds: 10),
    );
  });

  tearDown(() => cubit.close());

  test('start loads the grade subjects and the first page, filtered by grade', () async {
    learning.onSearch = (_, page) async => ApiSuccess(_page([teacherCard('t-1', 'Khaled')]));

    await cubit.start();

    expect(cubit.state.subjects.value, isNotEmpty);
    expect(cubit.state.teachers.value!.single.displayName, 'Khaled');
    expect(learning.searches.single.$1.gradeLevelId, 112);
  });

  test('loadMore appends the next page and stops at the last one', () async {
    learning.onSearch =
        (_, page) async => ApiSuccess(_page([teacherCard('t-$page', 'Teacher $page')], page: page, totalPages: 2));

    await cubit.load();
    await cubit.loadMore();
    await cubit.loadMore(); // no third page

    expect(cubit.state.teachers.value!.map((t) => t.id), ['t-1', 't-2']);
    expect(learning.searches.map((s) => s.$2), [1, 2]);
    expect(cubit.state.hasMore, isFalse);
  });

  test('selecting a subject restarts from page 1 with the filter', () async {
    await cubit.load();
    await cubit.selectSubject(2);

    expect(learning.searches.last.$1.subjectId, 2);
    expect(learning.searches.last.$2, 1);
  });

  test('typing is debounced into one search', () async {
    cubit
      ..searchChanged('k')
      ..searchChanged('kh')
      ..searchChanged('kha');
    await Future<void>.delayed(const Duration(milliseconds: 50));

    expect(learning.searches.map((s) => s.$1.search), ['kha']);
  });

  test('a slow, outdated search never overwrites newer results', () async {
    final slow = Completer<ApiResult<Paged<TeacherCard>>>();
    learning.onSearch =
        (query, _) =>
            query.subjectId == null ? slow.future : Future.value(ApiSuccess(_page([teacherCard('t-2', 'New')])));

    final first = cubit.load(); // outdated, still waiting
    await cubit.selectSubject(2);
    slow.complete(ApiSuccess(_page([teacherCard('t-1', 'Old')])));
    await first;

    expect(cubit.state.teachers.value!.single.displayName, 'New');
  });

  test('a failed first page shows the failure; a failed next page keeps the list', () async {
    learning.onSearch = (_, _) async => const ApiError(networkFailure);
    await cubit.load();
    expect(cubit.state.teachers.status, LoadStatus.failed);

    learning.onSearch =
        (_, page) async =>
            page == 1
                ? ApiSuccess(_page([teacherCard('t-1', 'Khaled')], totalPages: 2))
                : const ApiError(networkFailure);
    await cubit.load();
    await cubit.loadMore();
    expect(cubit.state.teachers.value!.single.id, 't-1');
  });
}
