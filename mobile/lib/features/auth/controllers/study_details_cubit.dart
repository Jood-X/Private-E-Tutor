import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/load_status.dart';
import '../../onboarding/models/catalog_models.dart';
import '../../onboarding/models/catalog_repository.dart';

class StudyDetailsState extends Equatable {
  const StudyDetailsState({
    this.optionsStatus = LoadStatus.idle,
    this.grades = const [],
    this.streams = const [],
    this.optionsFailure,
    this.gradeLevelId,
    this.streamId,
    this.subjectsStatus = LoadStatus.idle,
    this.subjects = const [],
    this.subjectsFailure,
    this.subjectIds = const {},
  });

  final LoadStatus optionsStatus;
  final List<CatalogItem> grades;
  final List<CatalogItem> streams;
  final ApiFailure? optionsFailure;
  final int? gradeLevelId;
  final int? streamId;
  final LoadStatus subjectsStatus;
  final List<CatalogItem> subjects;
  final ApiFailure? subjectsFailure;
  final Set<int> subjectIds;

  /// A grade and at least one subject are required; a stream too when the curriculum has streams.
  bool get canContinue => gradeLevelId != null && subjectIds.isNotEmpty && (streams.isEmpty || streamId != null);

  StudyDetailsState copyWith({
    LoadStatus? optionsStatus,
    List<CatalogItem>? grades,
    List<CatalogItem>? streams,
    ApiFailure? Function()? optionsFailure,
    int? Function()? gradeLevelId,
    int? Function()? streamId,
    LoadStatus? subjectsStatus,
    List<CatalogItem>? subjects,
    ApiFailure? Function()? subjectsFailure,
    Set<int>? subjectIds,
  }) => StudyDetailsState(
    optionsStatus: optionsStatus ?? this.optionsStatus,
    grades: grades ?? this.grades,
    streams: streams ?? this.streams,
    optionsFailure: optionsFailure != null ? optionsFailure() : this.optionsFailure,
    gradeLevelId: gradeLevelId != null ? gradeLevelId() : this.gradeLevelId,
    streamId: streamId != null ? streamId() : this.streamId,
    subjectsStatus: subjectsStatus ?? this.subjectsStatus,
    subjects: subjects ?? this.subjects,
    subjectsFailure: subjectsFailure != null ? subjectsFailure() : this.subjectsFailure,
    subjectIds: subjectIds ?? this.subjectIds,
  );

  @override
  List<Object?> get props => [
    optionsStatus,
    grades,
    streams,
    optionsFailure,
    gradeLevelId,
    streamId,
    subjectsStatus,
    subjects,
    subjectsFailure,
    subjectIds,
  ];
}

/// Controller for sign-up step 2: grade, optional stream, and subjects of that grade.
class StudyDetailsCubit extends Cubit<StudyDetailsState> {
  StudyDetailsCubit({required CatalogRepository repository, required Curriculum curriculum})
    : _repository = repository,
      _curriculum = curriculum,
      super(const StudyDetailsState());

  final CatalogRepository _repository;
  final Curriculum _curriculum;

  Future<void> load() async {
    emit(state.copyWith(optionsStatus: LoadStatus.loading, optionsFailure: () => null));
    final grades = await _repository.getGradeLevels(_curriculum.id);
    final streams =
        _curriculum.hasStreams ? await _repository.getStreams(_curriculum.id) : const ApiSuccess(<CatalogItem>[]);
    if (isClosed) return;

    switch ((grades, streams)) {
      case (ApiSuccess(value: final gradeList), ApiSuccess(value: final streamList)):
        emit(state.copyWith(optionsStatus: LoadStatus.loaded, grades: gradeList, streams: streamList));
      case (ApiError(:final failure), _) || (_, ApiError(:final failure)):
        emit(state.copyWith(optionsStatus: LoadStatus.failed, optionsFailure: () => failure));
    }
  }

  Future<void> selectGrade(int gradeLevelId) async {
    if (gradeLevelId == state.gradeLevelId && state.subjectsStatus == LoadStatus.loaded) return;
    emit(
      state.copyWith(
        gradeLevelId: () => gradeLevelId,
        subjectIds: const {},
        subjects: const [],
        subjectsStatus: LoadStatus.loading,
        subjectsFailure: () => null,
      ),
    );
    final result = await _repository.getSubjects(gradeLevelId);
    if (isClosed || state.gradeLevelId != gradeLevelId) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(state.copyWith(subjectsStatus: LoadStatus.loaded, subjects: value));
      case ApiError(:final failure):
        emit(state.copyWith(subjectsStatus: LoadStatus.failed, subjectsFailure: () => failure));
    }
  }

  Future<void> retrySubjects() async {
    final grade = state.gradeLevelId;
    if (grade != null) {
      emit(state.copyWith(gradeLevelId: () => null));
      await selectGrade(grade);
    }
  }

  void selectStream(int streamId) => emit(state.copyWith(streamId: () => streamId));

  void toggleSubject(int subjectId) {
    final next = {...state.subjectIds};
    if (!next.remove(subjectId)) next.add(subjectId);
    emit(state.copyWith(subjectIds: next));
  }
}
