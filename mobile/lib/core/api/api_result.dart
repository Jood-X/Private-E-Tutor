import 'api_failure.dart';

/// The outcome of an API call: exactly one of [ApiSuccess] or [ApiError].
/// Callers handle both cases with an exhaustive `switch`.
sealed class ApiResult<T> {
  const ApiResult();
}

final class ApiSuccess<T> extends ApiResult<T> {
  const ApiSuccess(this.value);

  final T value;
}

final class ApiError<T> extends ApiResult<T> {
  const ApiError(this.failure);

  final ApiFailure failure;
}
