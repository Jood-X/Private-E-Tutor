import 'package:flutter_bloc/flutter_bloc.dart';

import '../api/api_result.dart';
import 'loadable.dart';

/// A controller for a screen that shows one value fetched from the API (a lesson, a teacher, a list).
/// Subclasses add actions; reloading keeps the previous value visible.
class LoadableCubit<T> extends Cubit<Loadable<T>> {
  LoadableCubit(this._fetch) : super(const Loadable.idle());

  final Future<ApiResult<T>> Function() _fetch;

  Future<void> load() async {
    emit(Loadable.loading(state.value));
    final result = await _fetch();
    if (!isClosed) emit(Loadable.from(result));
  }
}
