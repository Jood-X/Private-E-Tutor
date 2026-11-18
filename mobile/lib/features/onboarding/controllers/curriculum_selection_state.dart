import 'package:equatable/equatable.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/state/load_status.dart';
import '../models/catalog_models.dart';

export '../../../core/state/load_status.dart';

/// Immutable state of the curriculum-selection step.
class CurriculumSelectionState extends Equatable {
  const CurriculumSelectionState({
    this.countriesStatus = LoadStatus.idle,
    this.countries = const [],
    this.countriesFailure,
    this.selectedCountryCode,
    this.curriculaStatus = LoadStatus.idle,
    this.curricula = const [],
    this.curriculaFailure,
    this.selectedCurriculumId,
  });

  final LoadStatus countriesStatus;
  final List<Country> countries;
  final ApiFailure? countriesFailure;
  final String? selectedCountryCode;
  final LoadStatus curriculaStatus;
  final List<Curriculum> curricula;
  final ApiFailure? curriculaFailure;
  final int? selectedCurriculumId;

  bool get canContinue => selectedCurriculumId != null;

  CurriculumSelectionState copyWith({
    LoadStatus? countriesStatus,
    List<Country>? countries,
    ApiFailure? Function()? countriesFailure,
    String? Function()? selectedCountryCode,
    LoadStatus? curriculaStatus,
    List<Curriculum>? curricula,
    ApiFailure? Function()? curriculaFailure,
    int? Function()? selectedCurriculumId,
  }) => CurriculumSelectionState(
    countriesStatus: countriesStatus ?? this.countriesStatus,
    countries: countries ?? this.countries,
    countriesFailure: countriesFailure != null ? countriesFailure() : this.countriesFailure,
    selectedCountryCode: selectedCountryCode != null ? selectedCountryCode() : this.selectedCountryCode,
    curriculaStatus: curriculaStatus ?? this.curriculaStatus,
    curricula: curricula ?? this.curricula,
    curriculaFailure: curriculaFailure != null ? curriculaFailure() : this.curriculaFailure,
    selectedCurriculumId: selectedCurriculumId != null ? selectedCurriculumId() : this.selectedCurriculumId,
  );

  @override
  List<Object?> get props => [
    countriesStatus,
    countries,
    countriesFailure,
    selectedCountryCode,
    curriculaStatus,
    curricula,
    curriculaFailure,
    selectedCurriculumId,
  ];
}
