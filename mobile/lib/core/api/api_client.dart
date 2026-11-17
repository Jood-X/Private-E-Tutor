import 'package:dio/dio.dart';

import '../logging/app_logger.dart';
import 'api_envelope.dart';
import 'api_failure.dart';
import 'api_result.dart';

/// Converts the envelope's `data` into a model. Throwing [FormatException] or [TypeError]
/// marks the response as invalid.
typedef JsonDecoder<T> = T Function(Object? data);

/// The single entry point for HTTP calls. It never throws for expected failures: every call
/// returns an [ApiResult], so controllers cannot forget to handle errors.
class ApiClient {
  ApiClient(this._dio);

  static const traceIdHeader = 'x-trace-id';
  static final _log = AppLogger('ApiClient');

  final Dio _dio;

  Future<ApiResult<T>> get<T>(
    String path,
    JsonDecoder<T> decode, {
    Map<String, Object?>? query,
    CancelToken? cancelToken,
  }) => _send(() => _dio.get<Object?>(path, queryParameters: query, cancelToken: cancelToken), decode);

  Future<ApiResult<T>> _send<T>(Future<Response<Object?>> Function() request, JsonDecoder<T> decode) async {
    final Response<Object?> response;
    try {
      response = await request();
    } on DioException catch (exception) {
      return ApiError(_failureFromDio(exception));
    }

    final envelope = ApiEnvelope.tryParse(response.data);
    if (envelope == null || !envelope.success) {
      return ApiError(_invalidResponse(response, 'Missing or unsuccessful envelope'));
    }

    try {
      return ApiSuccess(decode(envelope.data));
    } on FormatException catch (error, stackTrace) {
      _log.error('Could not decode ${response.requestOptions.path}', error, stackTrace);
      return ApiError(_invalidResponse(response, 'Decode failed'));
    } on TypeError catch (error, stackTrace) {
      _log.error('Could not decode ${response.requestOptions.path}', error, stackTrace);
      return ApiError(_invalidResponse(response, 'Decode failed'));
    }
  }

  ApiFailure _failureFromDio(DioException exception) {
    switch (exception.type) {
      // The server accepted the request but answered too slowly.
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
        return const ApiFailure(kind: ApiFailureKind.timeout);
      // No connection could be established at all: offline, server down or wrong address.
      // (On the Android emulator a stopped host server shows up as a connect timeout, not a refusal.)
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.connectionError:
      case DioExceptionType.badCertificate:
        return const ApiFailure(kind: ApiFailureKind.network);
      case DioExceptionType.cancel:
        return const ApiFailure(kind: ApiFailureKind.cancelled);
      case DioExceptionType.badResponse:
        return _failureFromResponse(exception.response!);
      case DioExceptionType.unknown:
        _log.error('Unexpected transport error', exception.error, exception.stackTrace);
        return const ApiFailure(kind: ApiFailureKind.unexpected);
    }
  }

  ApiFailure _failureFromResponse(Response<Object?> response) {
    final statusCode = response.statusCode ?? 0;
    final envelope = ApiEnvelope.tryParse(response.data);
    final firstError = envelope?.errors.firstOrNull;
    return ApiFailure(
      kind: ApiFailureKind.fromStatusCode(statusCode),
      code: firstError?.code,
      message: envelope?.message,
      statusCode: statusCode,
      traceId: envelope?.traceId ?? response.headers.value(traceIdHeader),
      errors: envelope?.errors ?? const [],
    );
  }

  ApiFailure _invalidResponse(Response<Object?> response, String reason) {
    final traceId = response.headers.value(traceIdHeader);
    _log.warning('Invalid response from ${response.requestOptions.path}: $reason (trace $traceId)');
    return ApiFailure(kind: ApiFailureKind.invalidResponse, statusCode: response.statusCode, traceId: traceId);
  }
}
