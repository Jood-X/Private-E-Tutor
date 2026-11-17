import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';

Map<String, dynamic> _readArb(String locale) =>
    jsonDecode(File('lib/l10n/app_$locale.arb').readAsStringSync()) as Map<String, dynamic>;

Set<String> _messageKeys(Map<String, dynamic> arb) => arb.keys.where((key) => !key.startsWith('@')).toSet();

Set<String> _placeholders(String message) =>
    RegExp(r'\{(\w+)\}').allMatches(message).map((match) => match.group(1)!).toSet();

void main() {
  final english = _readArb('en');
  final arabic = _readArb('ar');

  test('Arabic translates every English message and adds none', () {
    expect(_messageKeys(arabic), _messageKeys(english));
  });

  test('every English message has a description for translators', () {
    for (final key in _messageKeys(english)) {
      final metadata = english['@$key'];
      expect(metadata, isA<Map<String, dynamic>>(), reason: '@$key is missing');
      expect((metadata as Map<String, dynamic>)['description'], isNotEmpty, reason: '@$key has no description');
    }
  });

  test('translations keep the same placeholders', () {
    for (final key in _messageKeys(english)) {
      expect(
        _placeholders(arabic[key] as String),
        _placeholders(english[key] as String),
        reason: 'placeholders differ for $key',
      );
    }
  });
}
