import '../../../core/api/api_client.dart';
import '../../../core/api/api_result.dart';
import '../models/catalog_models.dart';
import '../models/catalog_repository.dart';

/// [CatalogRepository] backed by `GET /api/v1/catalog/...`.
class CatalogApiRepository implements CatalogRepository {
  CatalogApiRepository(this._client);

  static const _base = '/api/v1/catalog';

  final ApiClient _client;

  @override
  Future<ApiResult<List<Country>>> getCountries() => _client.get('$_base/countries', (data) => _list(data, _country));

  @override
  Future<ApiResult<List<Curriculum>>> getCurricula(String countryIsoCode) => _client.get(
    '$_base/countries/${Uri.encodeComponent(countryIsoCode)}/curricula',
    (data) => _list(data, _curriculum),
  );

  @override
  Future<ApiResult<List<CatalogItem>>> getGradeLevels(int curriculumId) =>
      _client.get('$_base/curricula/$curriculumId/grade-levels', (data) => _list(data, _item));

  @override
  Future<ApiResult<List<CatalogItem>>> getStreams(int curriculumId) =>
      _client.get('$_base/curricula/$curriculumId/streams', (data) => _list(data, _item));

  @override
  Future<ApiResult<List<CatalogItem>>> getSubjects(int gradeLevelId) =>
      _client.get('$_base/grade-levels/$gradeLevelId/subjects', (data) => _list(data, _item));

  static CatalogItem _item(Map<String, dynamic> json) =>
      CatalogItem(id: json['id'] as int, code: json['code'] as String, name: json['name'] as String);

  static List<T> _list<T>(Object? data, T Function(Map<String, dynamic>) decode) =>
      (data! as List<Object?>).cast<Map<String, dynamic>>().map(decode).toList(growable: false);

  static Country _country(Map<String, dynamic> json) => Country(
    isoCode: json['isoCode'] as String,
    name: json['name'] as String,
    defaultTimeZoneId: json['defaultTimeZoneId'] as String,
  );

  static Curriculum _curriculum(Map<String, dynamic> json) => Curriculum(
    id: json['id'] as int,
    code: json['code'] as String,
    name: json['name'] as String,
    hasStreams: json['hasStreams'] as bool,
  );
}
