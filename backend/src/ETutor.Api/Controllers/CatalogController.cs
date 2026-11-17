using ETutor.Api.Common;
using ETutor.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.Controllers;

/// <summary>
/// Public, read-only education catalog used during onboarding (before sign-in).
/// Names are returned in the language requested with the Accept-Language header (en default, ar).
/// </summary>
[AllowAnonymous]
[Route(ApiRoutes.V1 + "/catalog")]
public sealed class CatalogController(CatalogService catalog) : ApiControllerBase
{
    [HttpGet("countries")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CountryDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CountryDto>>>> GetCountries(CancellationToken cancellationToken) =>
        OkResponse(await catalog.GetCountriesAsync(cancellationToken));

    [HttpGet("countries/{isoCode}/curricula")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CurriculumDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CurriculumDto>>>> GetCurricula(
        string isoCode, CancellationToken cancellationToken) =>
        FromResult(await catalog.GetCurriculaForCountryAsync(isoCode, cancellationToken));

    [HttpGet("curricula/{curriculumId:int}/streams")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CurriculumStreamDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CurriculumStreamDto>>>> GetStreams(
        int curriculumId, CancellationToken cancellationToken) =>
        FromResult(await catalog.GetStreamsAsync(curriculumId, cancellationToken));

    [HttpGet("curricula/{curriculumId:int}/grade-levels")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<GradeLevelDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GradeLevelDto>>>> GetGradeLevels(
        int curriculumId, CancellationToken cancellationToken) =>
        FromResult(await catalog.GetGradeLevelsAsync(curriculumId, cancellationToken));

    [HttpGet("curricula/{curriculumId:int}/academic-years")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AcademicYearDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AcademicYearDto>>>> GetAcademicYears(
        int curriculumId, CancellationToken cancellationToken) =>
        FromResult(await catalog.GetAcademicYearsAsync(curriculumId, cancellationToken));

    [HttpGet("grade-levels/{gradeLevelId:int}/subjects")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<SubjectDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubjectDto>>>> GetSubjects(
        int gradeLevelId, CancellationToken cancellationToken) =>
        FromResult(await catalog.GetSubjectsForGradeLevelAsync(gradeLevelId, cancellationToken));
}
