import 'package:etutor_app/core/api/api_failure.dart';
import 'package:etutor_app/core/api/api_result.dart';
import 'package:etutor_app/features/onboarding/models/catalog_models.dart';
import 'package:etutor_app/features/onboarding/models/catalog_repository.dart';

const jordan = Country(isoCode: 'JO', name: 'Jordan', defaultTimeZoneId: 'Asia/Amman');
const uae = Country(isoCode: 'AE', name: 'United Arab Emirates', defaultTimeZoneId: 'Asia/Dubai');
const tawjihi = Curriculum(id: 1, code: 'jo-tawjihi', name: 'Jordanian Tawjihi', hasStreams: true);
const cambridge = Curriculum(id: 8, code: 'cambridge', name: 'Cambridge IGCSE / A Level', hasStreams: false);
const uaeMoe = Curriculum(id: 3, code: 'ae-moe', name: 'UAE Ministry of Education', hasStreams: false);

const networkFailure = ApiFailure(kind: ApiFailureKind.network);
const serverFailure = ApiFailure(kind: ApiFailureKind.server, statusCode: 500, traceId: 'trace-123');

/// In-memory [CatalogRepository] whose answers each test can set.
class FakeCatalogRepository implements CatalogRepository {
  ApiResult<List<Country>> countriesResult = const ApiSuccess([jordan, uae]);
  final Map<String, ApiResult<List<Curriculum>>> curriculaResults = {
    'JO': const ApiSuccess([tawjihi, cambridge]),
    'AE': const ApiSuccess([uaeMoe, cambridge]),
  };
  int countriesCalls = 0;
  final List<String> curriculaCalls = [];

  @override
  Future<ApiResult<List<Country>>> getCountries() async {
    countriesCalls++;
    return countriesResult;
  }

  @override
  Future<ApiResult<List<Curriculum>>> getCurricula(String countryIsoCode) async {
    curriculaCalls.add(countryIsoCode);
    return curriculaResults[countryIsoCode] ?? const ApiError(ApiFailure(kind: ApiFailureKind.notFound));
  }
}
