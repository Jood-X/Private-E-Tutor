import 'package:dio/dio.dart';

import '../config/app_config.dart';
import '../logging/app_logger.dart';
import 'api_client.dart';

/// Builds the app's single configured [Dio] instance.
abstract final class DioFactory {
  static const _connectTimeout = Duration(seconds: 10);
  static const _receiveTimeout = Duration(seconds: 20);

  static Dio create(AppConfig config, {required String Function() languageCode}) {
    final dio = Dio(
      BaseOptions(
        baseUrl: config.apiBaseUrl,
        connectTimeout: _connectTimeout,
        receiveTimeout: _receiveTimeout,
        headers: {Headers.acceptHeader: Headers.jsonContentType},
      ),
    );
    dio.interceptors
      ..add(_LanguageInterceptor(languageCode))
      ..add(_RequestLogInterceptor());
    return dio;
  }
}

/// Sends the user's chosen language so the server localizes catalog names and messages.
class _LanguageInterceptor extends Interceptor {
  _LanguageInterceptor(this._languageCode);

  final String Function() _languageCode;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.headers['Accept-Language'] = _languageCode();
    handler.next(options);
  }
}

/// Logs one line per request: method, path (no query string), status, duration and trace ID.
/// Headers and bodies are never logged because they may contain tokens or personal data.
class _RequestLogInterceptor extends Interceptor {
  static final _log = AppLogger('Http');
  static const _startedAtKey = 'startedAt';

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.extra[_startedAtKey] = DateTime.now();
    handler.next(options);
  }

  @override
  void onResponse(Response<dynamic> response, ResponseInterceptorHandler handler) {
    _log.debug(_describe(response.requestOptions, response.statusCode, response.headers));
    handler.next(response);
  }

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    final message =
        '${_describe(err.requestOptions, err.response?.statusCode, err.response?.headers)}'
        ' failed: ${err.type.name}';
    if (err.type == DioExceptionType.cancel) {
      _log.debug(message);
    } else {
      _log.warning(message);
    }
    handler.next(err);
  }

  String _describe(RequestOptions options, int? statusCode, Headers? headers) {
    final startedAt = options.extra[_startedAtKey];
    final elapsed = startedAt is DateTime ? DateTime.now().difference(startedAt).inMilliseconds : -1;
    final traceId = headers?.value(ApiClient.traceIdHeader) ?? '-';
    return '${options.method} ${options.uri.path} -> ${statusCode ?? '-'} in ${elapsed}ms trace=$traceId';
  }
}
