import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_result.dart';
import '../../onboarding/models/catalog_models.dart';
import '../models/auth_repository.dart';
import '../models/credential_rules.dart';
import 'form_submission.dart';
import 'session_cubit.dart';

/// Everything chosen so far in the 3-step student sign-up.
class StudentSignupState extends Equatable {
  const StudentSignupState({
    this.country,
    this.curriculum,
    this.gradeLevelId,
    this.streamId,
    this.subjectIds = const [],
    this.form = const FormSubmission(),
  });

  final Country? country;
  final Curriculum? curriculum;
  final int? gradeLevelId;
  final int? streamId;
  final List<int> subjectIds;
  final FormSubmission form;

  bool get hasStudyPlan => country != null && curriculum != null && gradeLevelId != null;

  @override
  List<Object?> get props => [country, curriculum, gradeLevelId, streamId, subjectIds, form];
}

/// Flow controller shared by the three sign-up screens (lives as long as the sign-up routes).
class StudentSignupCubit extends Cubit<StudentSignupState> {
  StudentSignupCubit({required AuthRepository repository, required SessionCubit session})
    : _repository = repository,
      _session = session,
      super(const StudentSignupState());

  final AuthRepository _repository;
  final SessionCubit _session;

  /// Step 1. Changing the curriculum resets the later choices.
  void chooseCurriculum(Country country, Curriculum curriculum) {
    if (state.curriculum?.id == curriculum.id && state.country?.isoCode == country.isoCode) return;
    emit(StudentSignupState(country: country, curriculum: curriculum));
  }

  /// Step 2.
  void chooseStudy({required int gradeLevelId, required int? streamId, required List<int> subjectIds}) => emit(
    StudentSignupState(
      country: state.country,
      curriculum: state.curriculum,
      gradeLevelId: gradeLevelId,
      streamId: streamId,
      subjectIds: subjectIds,
    ),
  );

  /// Step 3. The time zone defaults to the country's; students can change it later in their profile.
  Future<void> submit({
    required String displayName,
    required String email,
    required String password,
    required String languageCode,
  }) async {
    if (state.form.isSubmitting || !state.hasStudyPlan) return;
    final problems = collectProblems({
      'displayName': CredentialRules.displayName(displayName),
      'email': CredentialRules.email(email),
      'password': CredentialRules.newPassword(password),
    });
    if (problems.isNotEmpty) {
      emit(_withForm(state.form.invalid(problems)));
      return;
    }

    emit(_withForm(FormSubmission.submitting));
    final result = await _repository.registerStudent(
      StudentRegistration(
        email: email,
        password: password,
        displayName: displayName,
        preferredLanguage: languageCode,
        timeZoneId: state.country!.defaultTimeZoneId,
        countryCode: state.country!.isoCode,
        curriculumId: state.curriculum!.id,
        gradeLevelId: state.gradeLevelId!,
        streamId: state.streamId,
        subjectIds: state.subjectIds,
      ),
    );
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(_withForm(const FormSubmission()));
        await _session.startSession(value);
      case ApiError(:final failure):
        emit(_withForm(FormSubmission.failed(failure)));
    }
  }

  StudentSignupState _withForm(FormSubmission form) => StudentSignupState(
    country: state.country,
    curriculum: state.curriculum,
    gradeLevelId: state.gradeLevelId,
    streamId: state.streamId,
    subjectIds: state.subjectIds,
    form: form,
  );
}
