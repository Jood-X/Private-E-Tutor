import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/loadable.dart';
import '../../onboarding/models/catalog_models.dart';
import '../../onboarding/models/catalog_repository.dart';
import '../models/teaching_models.dart';

class CreateCourseState extends Equatable {
  const CreateCourseState({
    this.curricula = const Loadable.idle(),
    this.grades = const Loadable.idle(),
    this.subjects = const Loadable.idle(),
    this.curriculumId,
    this.gradeLevelId,
    this.subjectId,
    this.academicYearId,
    this.contentLanguage = 'en',
    this.submitting = false,
    this.failure,
    this.created,
  });

  final Loadable<List<Curriculum>> curricula;
  final Loadable<List<CatalogItem>> grades;
  final Loadable<List<CatalogItem>> subjects;
  final int? curriculumId;
  final int? gradeLevelId;
  final int? subjectId;

  /// The curriculum's current academic year, chosen automatically.
  final int? academicYearId;
  final String contentLanguage;
  final bool submitting;
  final ApiFailure? failure;

  /// Set once the course exists, so the view can open its editor.
  final TeacherCourse? created;

  bool get canSubmit => subjectId != null && academicYearId != null && !submitting;

  CreateCourseState copyWith({
    Loadable<List<Curriculum>>? curricula,
    Loadable<List<CatalogItem>>? grades,
    Loadable<List<CatalogItem>>? subjects,
    int? Function()? curriculumId,
    int? Function()? gradeLevelId,
    int? Function()? subjectId,
    int? Function()? academicYearId,
    String? contentLanguage,
    bool? submitting,
    ApiFailure? Function()? failure,
    TeacherCourse? created,
  }) => CreateCourseState(
    curricula: curricula ?? this.curricula,
    grades: grades ?? this.grades,
    subjects: subjects ?? this.subjects,
    curriculumId: curriculumId != null ? curriculumId() : this.curriculumId,
    gradeLevelId: gradeLevelId != null ? gradeLevelId() : this.gradeLevelId,
    subjectId: subjectId != null ? subjectId() : this.subjectId,
    academicYearId: academicYearId != null ? academicYearId() : this.academicYearId,
    contentLanguage: contentLanguage ?? this.contentLanguage,
    submitting: submitting ?? this.submitting,
    failure: failure != null ? failure() : this.failure,
    created: created ?? this.created,
  );

  @override
  List<Object?> get props => [
    curricula,
    grades,
    subjects,
    curriculumId,
    gradeLevelId,
    subjectId,
    academicYearId,
    contentLanguage,
    submitting,
    failure,
    created,
  ];
}

/// Creating a course: curriculum (offered in the teacher's country) → grade → subject, plus title and language.
class CreateCourseCubit extends Cubit<CreateCourseState> {
  CreateCourseCubit({
    required CatalogRepository catalog,
    required TeachingRepository teaching,
    required String countryCode,
  }) : _catalog = catalog,
       _teaching = teaching,
       _countryCode = countryCode,
       super(const CreateCourseState());

  final CatalogRepository _catalog;
  final TeachingRepository _teaching;
  final String _countryCode;

  Future<void> load() async {
    emit(state.copyWith(curricula: const Loadable.loading()));
    final result = await _catalog.getCurricula(_countryCode);
    if (!isClosed) emit(state.copyWith(curricula: Loadable.from(result)));
  }

  Future<void> selectCurriculum(int curriculumId) async {
    emit(
      state.copyWith(
        curriculumId: () => curriculumId,
        gradeLevelId: () => null,
        subjectId: () => null,
        academicYearId: () => null,
        grades: const Loadable.loading(),
        subjects: const Loadable.idle(),
      ),
    );
    final grades = await _catalog.getGradeLevels(curriculumId);
    final years = await _catalog.getAcademicYears(curriculumId);
    if (isClosed || state.curriculumId != curriculumId) return;
    final currentYear = switch (years) {
      ApiSuccess(:final value) when value.isNotEmpty => value.first.id,
      _ => null,
    };
    emit(state.copyWith(grades: Loadable.from(grades), academicYearId: () => currentYear));
  }

  Future<void> selectGrade(int gradeLevelId) async {
    emit(state.copyWith(gradeLevelId: () => gradeLevelId, subjectId: () => null, subjects: const Loadable.loading()));
    final subjects = await _catalog.getSubjects(gradeLevelId);
    if (!isClosed && state.gradeLevelId == gradeLevelId) emit(state.copyWith(subjects: Loadable.from(subjects)));
  }

  void selectSubject(int subjectId) => emit(state.copyWith(subjectId: () => subjectId));

  void selectLanguage(String code) => emit(state.copyWith(contentLanguage: code));

  Future<void> submit({required String title, required String description}) async {
    if (!state.canSubmit || title.trim().isEmpty) return;
    emit(state.copyWith(submitting: true, failure: () => null));
    final result = await _teaching.createCourse(
      NewCourse(
        title: title,
        description: description,
        contentLanguage: state.contentLanguage,
        curriculumId: state.curriculumId!,
        gradeLevelId: state.gradeLevelId!,
        subjectId: state.subjectId!,
        academicYearId: state.academicYearId!,
      ),
    );
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(state.copyWith(submitting: false, created: value));
      case ApiError(:final failure):
        emit(state.copyWith(submitting: false, failure: () => failure));
    }
  }
}
