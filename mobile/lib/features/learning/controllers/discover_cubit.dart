import 'dart:async';

import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_result.dart';
import '../../../core/state/loadable.dart';
import '../../onboarding/models/catalog_models.dart';
import '../../onboarding/models/catalog_repository.dart';
import '../models/learning_models.dart';

class DiscoverState extends Equatable {
  const DiscoverState({
    required this.query,
    this.subjects = const Loadable.idle(),
    this.teachers = const Loadable.idle(),
    this.page = 0,
    this.hasMore = false,
  });

  final TeacherQuery query;

  /// Filter chips: the subjects of the student's grade.
  final Loadable<List<CatalogItem>> subjects;
  final Loadable<List<TeacherCard>> teachers;
  final int page;
  final bool hasMore;

  DiscoverState copyWith({
    TeacherQuery? query,
    Loadable<List<CatalogItem>>? subjects,
    Loadable<List<TeacherCard>>? teachers,
    int? page,
    bool? hasMore,
  }) => DiscoverState(
    query: query ?? this.query,
    subjects: subjects ?? this.subjects,
    teachers: teachers ?? this.teachers,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
  );

  @override
  List<Object?> get props => [query, subjects, teachers, page, hasMore];
}

/// Teacher discovery for a student: search text, subject filter and infinite scrolling.
/// Results of an outdated query are dropped, so fast typing never shows stale teachers.
class DiscoverCubit extends Cubit<DiscoverState> {
  DiscoverCubit({
    required LearningRepository learning,
    required CatalogRepository catalog,
    int? gradeLevelId,
    this.searchDelay = const Duration(milliseconds: 350),
  }) : _learning = learning,
       _catalog = catalog,
       super(DiscoverState(query: TeacherQuery(gradeLevelId: gradeLevelId)));

  final LearningRepository _learning;
  final CatalogRepository _catalog;
  final Duration searchDelay;
  Timer? _debounce;
  int _generation = 0;

  Future<void> start() async {
    await Future.wait([_loadSubjects(), load()]);
  }

  Future<void> load() async {
    final generation = ++_generation;
    emit(state.copyWith(teachers: Loadable.loading(state.teachers.value), page: 0, hasMore: false));
    await _fetch(generation, page: 1, existing: const []);
  }

  Future<void> loadMore() async {
    if (!state.hasMore || state.teachers.isLoading) return;
    final existing = state.teachers.value ?? const <TeacherCard>[];
    emit(state.copyWith(teachers: Loadable.loading(existing)));
    await _fetch(_generation, page: state.page + 1, existing: existing);
  }

  Future<void> selectSubject(int? subjectId) {
    _debounce?.cancel();
    final query = state.query;
    emit(
      state.copyWith(
        query: TeacherQuery(gradeLevelId: query.gradeLevelId, subjectId: subjectId, search: query.search),
        teachers: const Loadable.loading(),
      ),
    );
    return load();
  }

  /// Waits for a pause in typing before searching.
  void searchChanged(String text) {
    final query = state.query;
    if (text.trim() == query.search.trim()) return;
    emit(
      state.copyWith(query: TeacherQuery(gradeLevelId: query.gradeLevelId, subjectId: query.subjectId, search: text)),
    );
    _debounce?.cancel();
    _debounce = Timer(searchDelay, load);
  }

  Future<void> retrySubjects() => _loadSubjects();

  Future<void> _loadSubjects() async {
    final gradeLevelId = state.query.gradeLevelId;
    if (gradeLevelId == null) {
      emit(state.copyWith(subjects: const Loadable.loaded([])));
      return;
    }
    emit(state.copyWith(subjects: const Loadable.loading()));
    final result = await _catalog.getSubjects(gradeLevelId);
    if (!isClosed) emit(state.copyWith(subjects: Loadable.from(result)));
  }

  Future<void> _fetch(int generation, {required int page, required List<TeacherCard> existing}) async {
    final result = await _learning.searchTeachers(state.query, page: page);
    if (isClosed || generation != _generation) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(
          state.copyWith(
            teachers: Loadable.loaded([...existing, ...value.items]),
            page: value.page,
            hasMore: value.hasMore,
          ),
        );
      case ApiError(:final failure):
        // A failed next page keeps what is already shown.
        emit(
          existing.isEmpty
              ? state.copyWith(teachers: Loadable.failed(failure), hasMore: false)
              : state.copyWith(teachers: Loadable.loaded(existing)),
        );
    }
  }

  @override
  Future<void> close() {
    _debounce?.cancel();
    return super.close();
  }
}
