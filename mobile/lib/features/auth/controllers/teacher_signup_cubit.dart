import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/load_status.dart';
import '../../onboarding/models/catalog_models.dart';
import '../../onboarding/models/catalog_repository.dart';
import '../models/auth_repository.dart';
import '../models/credential_rules.dart';
import 'form_submission.dart';
import 'session_cubit.dart';

class TeacherSignupState extends Equatable {
  const TeacherSignupState({
    this.countriesStatus = LoadStatus.idle,
    this.countries = const [],
    this.countriesFailure,
    this.country,
    this.form = const FormSubmission(),
  });

  final LoadStatus countriesStatus;
  final List<Country> countries;
  final ApiFailure? countriesFailure;
  final Country? country;
  final FormSubmission form;

  TeacherSignupState copyWith({
    LoadStatus? countriesStatus,
    List<Country>? countries,
    ApiFailure? Function()? countriesFailure,
    Country? country,
    FormSubmission? form,
  }) => TeacherSignupState(
    countriesStatus: countriesStatus ?? this.countriesStatus,
    countries: countries ?? this.countries,
    countriesFailure: countriesFailure != null ? countriesFailure() : this.countriesFailure,
    country: country ?? this.country,
    form: form ?? this.form,
  );

  @override
  List<Object?> get props => [countriesStatus, countries, countriesFailure, country, form];
}

class TeacherSignupCubit extends Cubit<TeacherSignupState> {
  TeacherSignupCubit({
    required AuthRepository repository,
    required CatalogRepository catalog,
    required SessionCubit session,
  }) : _repository = repository,
       _catalog = catalog,
       _session = session,
       super(const TeacherSignupState());

  final AuthRepository _repository;
  final CatalogRepository _catalog;
  final SessionCubit _session;

  Future<void> loadCountries() async {
    emit(state.copyWith(countriesStatus: LoadStatus.loading, countriesFailure: () => null));
    final result = await _catalog.getCountries();
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(state.copyWith(countriesStatus: LoadStatus.loaded, countries: value));
      case ApiError(:final failure):
        emit(state.copyWith(countriesStatus: LoadStatus.failed, countriesFailure: () => failure));
    }
  }

  void selectCountry(Country country) => emit(state.copyWith(country: country));

  Future<void> submit({
    required String displayName,
    required String email,
    required String password,
    required String headline,
    required String languageCode,
  }) async {
    if (state.form.isSubmitting) return;
    final problems = collectProblems({
      'displayName': CredentialRules.displayName(displayName),
      'email': CredentialRules.email(email),
      'password': CredentialRules.newPassword(password),
      'headline': CredentialRules.headline(headline),
      'countryCode': state.country == null ? InputProblem.required : null,
    });
    if (problems.isNotEmpty) {
      emit(state.copyWith(form: state.form.invalid(problems)));
      return;
    }

    emit(state.copyWith(form: FormSubmission.submitting));
    final result = await _repository.registerTeacher(
      TeacherRegistration(
        email: email,
        password: password,
        displayName: displayName,
        preferredLanguage: languageCode,
        timeZoneId: state.country!.defaultTimeZoneId,
        countryCode: state.country!.isoCode,
        headline: headline,
      ),
    );
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(state.copyWith(form: const FormSubmission()));
        await _session.startSession(value);
      case ApiError(:final failure):
        emit(state.copyWith(form: FormSubmission.failed(failure)));
    }
  }
}
