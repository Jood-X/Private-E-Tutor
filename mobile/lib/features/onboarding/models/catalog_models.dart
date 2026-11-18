import 'package:equatable/equatable.dart';

/// A country students can choose. Names arrive already localized by the API.
class Country extends Equatable {
  const Country({required this.isoCode, required this.name, required this.defaultTimeZoneId});

  final String isoCode;
  final String name;
  final String defaultTimeZoneId;

  @override
  List<Object?> get props => [isoCode, name, defaultTimeZoneId];
}

/// A curriculum offered in the selected country.
class Curriculum extends Equatable {
  const Curriculum({required this.id, required this.code, required this.name, required this.hasStreams});

  final int id;
  final String code;
  final String name;
  final bool hasStreams;

  @override
  List<Object?> get props => [id, code, name, hasStreams];
}

/// A named catalog item (grade level, stream or subject); names arrive localized.
class CatalogItem extends Equatable {
  const CatalogItem({required this.id, required this.code, required this.name});

  final int id;
  final String code;
  final String name;

  @override
  List<Object?> get props => [id, code, name];
}
