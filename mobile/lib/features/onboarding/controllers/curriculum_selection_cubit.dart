import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_result.dart';
import '../models/catalog_repository.dart';
import 'curriculum_selection_state.dart';

/// Controller for "What are you studying?": loads countries, then the curricula of the chosen
/// country, and tracks the selection. Contains no widgets and no HTTP code.
class CurriculumSelectionCubit extends Cubit<CurriculumSelectionState> {
  CurriculumSelectionCubit(this._repository) : super(const CurriculumSelectionState());

  final CatalogRepository _repository;

  Future<void> loadCountries() async {
    emit(state.copyWith(countriesStatus: LoadStatus.loading, countriesFailure: () => null));
    final result = await _repository.getCountries();
    if (isClosed) {
      return;
    }
    switch (result) {
      case ApiSuccess(:final value):
        emit(state.copyWith(countriesStatus: LoadStatus.loaded, countries: value));
      case ApiError(:final failure):
        emit(state.copyWith(countriesStatus: LoadStatus.failed, countriesFailure: () => failure));
    }
  }

  Future<void> selectCountry(String isoCode) async {
    if (isoCode == state.selectedCountryCode && state.curriculaStatus == LoadStatus.loaded) {
      return;
    }
    emit(state.copyWith(selectedCountryCode: () => isoCode, selectedCurriculumId: () => null, curricula: const []));
    await _loadCurricula(isoCode);
  }

  void selectCurriculum(int curriculumId) => emit(state.copyWith(selectedCurriculumId: () => curriculumId));

  Future<void> retryCurricula() async {
    final code = state.selectedCountryCode;
    if (code != null) {
      await _loadCurricula(code);
    }
  }

  /// Reloads names after the UI language changes, keeping the user's selections.
  Future<void> reloadForLanguageChange() async {
    await loadCountries();
    final code = state.selectedCountryCode;
    if (code != null && !isClosed) {
      await _loadCurricula(code, keepSelection: true);
    }
  }

  Future<void> _loadCurricula(String isoCode, {bool keepSelection = false}) async {
    emit(state.copyWith(curriculaStatus: LoadStatus.loading, curriculaFailure: () => null));
    final result = await _repository.getCurricula(isoCode);

    // Ignore a late response if the user picked another country meanwhile.
    if (isClosed || state.selectedCountryCode != isoCode) {
      return;
    }
    switch (result) {
      case ApiSuccess(:final value):
        final keptId =
            keepSelection && value.any((c) => c.id == state.selectedCurriculumId) ? state.selectedCurriculumId : null;
        emit(state.copyWith(curriculaStatus: LoadStatus.loaded, curricula: value, selectedCurriculumId: () => keptId));
      case ApiError(:final failure):
        emit(state.copyWith(curriculaStatus: LoadStatus.failed, curriculaFailure: () => failure));
    }
  }
}
