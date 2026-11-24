import 'package:equatable/equatable.dart';

import '../api/api_failure.dart';
import '../api/api_result.dart';
import 'load_status.dart';

/// Data loaded from the API for one part of a screen: status, value (when loaded) and failure (when failed).
/// Keeps controllers free of repeated status/value/failure triples.
class Loadable<T> extends Equatable {
  const Loadable._(this.status, this.value, this.failure);

  const Loadable.idle() : this._(LoadStatus.idle, null, null);

  const Loadable.loading([T? previous]) : this._(LoadStatus.loading, previous, null);

  const Loadable.loaded(T value) : this._(LoadStatus.loaded, value, null);

  const Loadable.failed(ApiFailure failure) : this._(LoadStatus.failed, null, failure);

  factory Loadable.from(ApiResult<T> result) => switch (result) {
    ApiSuccess(:final value) => Loadable.loaded(value),
    ApiError(:final failure) => Loadable.failed(failure),
  };

  final LoadStatus status;

  /// The loaded value; also kept while reloading so the screen doesn't flash to a skeleton.
  final T? value;
  final ApiFailure? failure;

  bool get isLoading => status == LoadStatus.loading;

  @override
  List<Object?> get props => [status, value, failure];
}
