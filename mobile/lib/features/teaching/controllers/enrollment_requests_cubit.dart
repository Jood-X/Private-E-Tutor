import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

import '../../../core/api/api_failure.dart';
import '../../../core/api/api_result.dart';
import '../../../core/state/loadable.dart';
import '../../content/models/content_models.dart';
import '../models/teaching_models.dart';

class EnrollmentListState extends Equatable {
  const EnrollmentListState({
    required this.status,
    this.items = const Loadable.idle(),
    this.page = 0,
    this.hasMore = false,
  });

  /// Which list this is: Pending (requests) or Approved (students).
  final EnrollmentStatus status;
  final Loadable<List<TeacherEnrollment>> items;
  final int page;
  final bool hasMore;

  @override
  List<Object?> get props => [status, items, page, hasMore];
}

/// One paginated enrollment list for the teacher (requests or students), with decisions.
class EnrollmentListCubit extends Cubit<EnrollmentListState> {
  EnrollmentListCubit({required TeachingRepository repository, required EnrollmentStatus status})
    : _repository = repository,
      super(EnrollmentListState(status: status));

  final TeachingRepository _repository;

  Future<void> load() async {
    emit(EnrollmentListState(status: state.status, items: Loadable.loading(state.items.value)));
    await _fetch(page: 1, existing: const []);
  }

  /// Loads the next page; ignored while a load is in progress or when everything is loaded.
  Future<void> loadMore() async {
    if (!state.hasMore || state.items.isLoading) return;
    final existing = state.items.value ?? const <TeacherEnrollment>[];
    emit(EnrollmentListState(status: state.status, items: Loadable.loading(existing), page: state.page, hasMore: true));
    await _fetch(page: state.page + 1, existing: existing);
  }

  /// Applies a decision; the item leaves this list when its status changes.
  Future<ApiFailure?> decide(String enrollmentId, EnrollmentDecision decision, {String? note}) async {
    final result = await _repository.decide(enrollmentId, decision, note: note);
    if (isClosed) return null;
    switch (result) {
      case ApiSuccess():
        final remaining =
            (state.items.value ?? const <TeacherEnrollment>[]).where((item) => item.id != enrollmentId).toList();
        emit(
          EnrollmentListState(
            status: state.status,
            items: Loadable.loaded(remaining),
            page: state.page,
            hasMore: state.hasMore,
          ),
        );
        return null;
      case ApiError(:final failure):
        return failure;
    }
  }

  Future<void> _fetch({required int page, required List<TeacherEnrollment> existing}) async {
    final result = await _repository.listEnrollments(status: state.status, page: page);
    if (isClosed) return;
    switch (result) {
      case ApiSuccess(:final value):
        emit(
          EnrollmentListState(
            status: state.status,
            items: Loadable.loaded([...existing, ...value.items]),
            page: value.page,
            hasMore: value.hasMore,
          ),
        );
      case ApiError(:final failure):
        emit(EnrollmentListState(status: state.status, items: Loadable.failed(failure), page: state.page));
    }
  }
}
