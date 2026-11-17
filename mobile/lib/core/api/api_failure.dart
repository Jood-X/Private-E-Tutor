import 'package:equatable/equatable.dart';

/// Why a request failed, independent of transport details. Controllers branch on this
/// (e.g. offer "retry" for [network]); views turn it into a localized message.
enum ApiFailureKind {
  network,
  timeout,
  cancelled,
  validation,
  unauthorized,
  forbidden,
  notFound,
  conflict,
  businessRule,
  rateLimited,
  server,
  invalidResponse,
  unexpected;

  /// Whether repeating the same request may succeed without the user changing anything.
  bool get isRetryable => switch (this) {
    network || timeout || rateLimited || server => true,
    _ => false,
  };

  static ApiFailureKind fromStatusCode(int statusCode) => switch (statusCode) {
    400 => validation,
    401 => unauthorized,
    403 => forbidden,
    404 => notFound,
    409 => conflict,
    422 => businessRule,
    429 => rateLimited,
    >= 500 => server,
    _ => unexpected,
  };
}

/// One field-level or general error returned by the API envelope.
class ApiFieldError extends Equatable {
  const ApiFieldError({required this.code, required this.message, this.field});

  final String code;
  final String message;
  final String? field;

  @override
  List<Object?> get props => [code, message, field];
}

/// A failed API call. [code] is the server's stable error code (e.g. `catalog.country_not_found`)
/// when the server answered; [traceId] lets support find the matching server logs.
class ApiFailure extends Equatable {
  const ApiFailure({
    required this.kind,
    this.code,
    this.message,
    this.statusCode,
    this.traceId,
    this.errors = const [],
  });

  final ApiFailureKind kind;
  final String? code;
  final String? message;
  final int? statusCode;
  final String? traceId;
  final List<ApiFieldError> errors;

  @override
  List<Object?> get props => [kind, code, message, statusCode, traceId, errors];

  @override
  String toString() => 'ApiFailure(kind: ${kind.name}, code: $code, status: $statusCode, traceId: $traceId)';
}
