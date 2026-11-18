/// Client-side input rules that mirror the server's (backend `UserValidators` + Identity password policy).
/// They give instant feedback; the server remains the authority.
enum InputProblem { required, email, password, name, headline }

abstract final class CredentialRules {
  static const minPasswordLength = 8;
  static const maxPasswordLength = 128;
  static const minNameLength = 2;
  static const maxNameLength = 100;
  static const minHeadlineLength = 3;
  static const maxHeadlineLength = 150;

  static final _email = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final _letter = RegExp(r'[A-Za-z؀-ۿ]');
  static final _digit = RegExp(r'\d');

  static InputProblem? email(String value) {
    final trimmed = value.trim();
    if (trimmed.isEmpty) return InputProblem.required;
    return _email.hasMatch(trimmed) ? null : InputProblem.email;
  }

  /// For sign-in only presence is checked: the policy may have changed since the account was created.
  static InputProblem? existingPassword(String value) => value.isEmpty ? InputProblem.required : null;

  static InputProblem? newPassword(String value) {
    if (value.isEmpty) return InputProblem.required;
    final valid =
        value.length >= minPasswordLength &&
        value.length <= maxPasswordLength &&
        _letter.hasMatch(value) &&
        _digit.hasMatch(value);
    return valid ? null : InputProblem.password;
  }

  static InputProblem? displayName(String value) {
    final length = value.trim().length;
    if (length == 0) return InputProblem.required;
    return length >= minNameLength && length <= maxNameLength ? null : InputProblem.name;
  }

  static InputProblem? headline(String value) {
    final length = value.trim().length;
    if (length == 0) return InputProblem.required;
    return length >= minHeadlineLength && length <= maxHeadlineLength ? null : InputProblem.headline;
  }
}
