import 'package:bloc_test/bloc_test.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/onboarding/controllers/curriculum_selection_cubit.dart';
import 'package:etutor_app/features/onboarding/controllers/curriculum_selection_state.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fake_catalog_repository.dart';

void main() {
  late FakeCatalogRepository repository;

  setUp(() => repository = FakeCatalogRepository());

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'loadCountries emits loading then loaded',
    build: () => CurriculumSelectionCubit(repository),
    act: (cubit) => cubit.loadCountries(),
    expect:
        () => const [
          CurriculumSelectionState(countriesStatus: LoadStatus.loading),
          CurriculumSelectionState(countriesStatus: LoadStatus.loaded, countries: [jordan, uae]),
        ],
  );

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'loadCountries keeps the failure so the view can offer a retry',
    setUp: () => repository.countriesResult = const ApiError(networkFailure),
    build: () => CurriculumSelectionCubit(repository),
    act: (cubit) => cubit.loadCountries(),
    expect:
        () => const [
          CurriculumSelectionState(countriesStatus: LoadStatus.loading),
          CurriculumSelectionState(countriesStatus: LoadStatus.failed, countriesFailure: networkFailure),
        ],
  );

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'selecting a country loads its curricula and clears an earlier curriculum choice',
    build: () => CurriculumSelectionCubit(repository),
    seed:
        () => const CurriculumSelectionState(
          countriesStatus: LoadStatus.loaded,
          countries: [jordan, uae],
          selectedCountryCode: 'AE',
          curriculaStatus: LoadStatus.loaded,
          curricula: [uaeMoe],
          selectedCurriculumId: 3,
        ),
    act: (cubit) => cubit.selectCountry('JO'),
    skip: 2,
    expect:
        () => const [
          CurriculumSelectionState(
            countriesStatus: LoadStatus.loaded,
            countries: [jordan, uae],
            selectedCountryCode: 'JO',
            curriculaStatus: LoadStatus.loaded,
            curricula: [tawjihi, cambridge],
          ),
        ],
    verify: (cubit) => expect(cubit.state.canContinue, isFalse),
  );

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'choosing a curriculum enables Continue',
    build: () => CurriculumSelectionCubit(repository),
    seed:
        () => const CurriculumSelectionState(
          selectedCountryCode: 'JO',
          curriculaStatus: LoadStatus.loaded,
          curricula: [tawjihi, cambridge],
        ),
    act: (cubit) => cubit.selectCurriculum(8),
    verify: (cubit) {
      expect(cubit.state.selectedCurriculumId, 8);
      expect(cubit.state.canContinue, isTrue);
    },
  );

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'a curricula failure can be retried for the same country',
    setUp: () => repository.curriculaResults['JO'] = const ApiError(serverFailure),
    build: () => CurriculumSelectionCubit(repository),
    act: (cubit) async {
      await cubit.selectCountry('JO');
      repository.curriculaResults['JO'] = const ApiSuccess([tawjihi]);
      await cubit.retryCurricula();
    },
    verify: (cubit) {
      expect(repository.curriculaCalls, ['JO', 'JO']);
      expect(cubit.state.curriculaStatus, LoadStatus.loaded);
      expect(cubit.state.curriculaFailure, isNull);
    },
  );

  blocTest<CurriculumSelectionCubit, CurriculumSelectionState>(
    'reloading after a language change keeps a still-valid selection',
    build: () => CurriculumSelectionCubit(repository),
    seed:
        () => const CurriculumSelectionState(
          countriesStatus: LoadStatus.loaded,
          countries: [jordan, uae],
          selectedCountryCode: 'JO',
          curriculaStatus: LoadStatus.loaded,
          curricula: [tawjihi, cambridge],
          selectedCurriculumId: 1,
        ),
    act: (cubit) => cubit.reloadForLanguageChange(),
    verify: (cubit) {
      expect(repository.countriesCalls, 1);
      expect(cubit.state.selectedCountryCode, 'JO');
      expect(cubit.state.selectedCurriculumId, 1);
    },
  );
}
