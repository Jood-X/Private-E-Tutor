import 'package:flutter_bloc/flutter_bloc.dart';

import 'app_logger.dart';

/// Logs controller (Cubit) errors centrally. State contents are not logged because they may
/// contain personal data; only the controller type is recorded.
class AppBlocObserver extends BlocObserver {
  static final _log = AppLogger('Controllers');

  @override
  void onError(BlocBase<dynamic> bloc, Object error, StackTrace stackTrace) {
    _log.error('Unhandled error in ${bloc.runtimeType}', error, stackTrace);
    super.onError(bloc, error, stackTrace);
  }
}
