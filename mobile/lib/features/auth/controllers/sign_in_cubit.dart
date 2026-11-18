import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_result.dart';
import '../models/auth_repository.dart';
import '../models/credential_rules.dart';
import 'form_submission.dart';
import 'session_cubit.dart';

class SignInCubit extends Cubit<FormSubmission> {
  SignInCubit({required AuthRepository repository, required SessionCubit session})
    : _repository = repository,
      _session = session,
      super(const FormSubmission());

  final AuthRepository _repository;
  final SessionCubit _session;

  Future<void> submit({required String email, required String password}) async {
    if (state.isSubmitting) return;
    final problems = collectProblems({
      'email': CredentialRules.email(email),
      'password': CredentialRules.existingPassword(password),
    });
    if (problems.isNotEmpty) {
      emit(state.invalid(problems));
      return;
    }

    emit(FormSubmission.submitting);
    final result = await _repository.signIn(email: email, password: password);
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(const FormSubmission());
        await _session.startSession(value);
      case ApiError(:final failure):
        emit(FormSubmission.failed(failure));
    }
  }
}
