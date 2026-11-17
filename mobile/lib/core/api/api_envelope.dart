import 'api_failure.dart';

/// Reads the server's standard envelope: `{ success, message, data, errors, traceId }`.
/// See docs/api-conventions.md.
class ApiEnvelope {
  const ApiEnvelope({required this.success, required this.data, required this.errors, this.message, this.traceId});

  /// Returns `null` when [json] is not an envelope (e.g. an HTML error page from a proxy).
  static ApiEnvelope? tryParse(Object? json) {
    if (json is! Map<String, dynamic> || json['success'] is! bool) {
      return null;
    }

    final rawErrors = json['errors'];
    final errors =
        rawErrors is List
            ? rawErrors
                .whereType<Map<String, dynamic>>()
                .map(
                  (error) => ApiFieldError(
                    code: error['code'] as String? ?? 'unknown',
                    message: error['message'] as String? ?? '',
                    field: error['field'] as String?,
                  ),
                )
                .toList()
            : const <ApiFieldError>[];

    return ApiEnvelope(
      success: json['success'] as bool,
      message: json['message'] as String?,
      data: json['data'],
      errors: errors,
      traceId: json['traceId'] as String?,
    );
  }

  final bool success;
  final String? message;
  final Object? data;
  final List<ApiFieldError> errors;
  final String? traceId;
}
