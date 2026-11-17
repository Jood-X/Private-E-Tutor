import '../../../core/api/api_result.dart';
import 'catalog_models.dart';

/// Model-layer boundary for catalog data. Controllers depend on this interface, so they
/// can be tested with a fake and never know about HTTP.
abstract interface class CatalogRepository {
  Future<ApiResult<List<Country>>> getCountries();

  Future<ApiResult<List<Curriculum>>> getCurricula(String countryIsoCode);
}
