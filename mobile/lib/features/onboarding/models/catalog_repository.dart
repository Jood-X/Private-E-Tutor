import '../../../core/api/api_result.dart';
import 'catalog_models.dart';

/// Model-layer boundary for catalog data. Controllers depend on this interface, so they
/// can be tested with a fake and never know about HTTP.
abstract interface class CatalogRepository {
  Future<ApiResult<List<Country>>> getCountries();

  Future<ApiResult<List<Curriculum>>> getCurricula(String countryIsoCode);

  Future<ApiResult<List<CatalogItem>>> getGradeLevels(int curriculumId);

  Future<ApiResult<List<CatalogItem>>> getStreams(int curriculumId);

  Future<ApiResult<List<CatalogItem>>> getSubjects(int gradeLevelId);

  /// Newest first; the item's name is the label (e.g. "2026/2027").
  Future<ApiResult<List<CatalogItem>>> getAcademicYears(int curriculumId);
}
