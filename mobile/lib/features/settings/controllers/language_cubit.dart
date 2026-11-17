import 'package:flutter_bloc/flutter_bloc.dart';

import '../data/language_preferences.dart';
import '../models/app_language.dart';

/// App-wide controller for the UI language. The whole widget tree (and text direction)
/// rebuilds from its state.
class LanguageCubit extends Cubit<AppLanguage> {
  LanguageCubit(this._preferences) : super(_preferences.read());

  final LanguagePreferences _preferences;

  Future<void> select(AppLanguage language) async {
    if (language == state) {
      return;
    }
    emit(language);
    await _preferences.write(language);
  }
}
