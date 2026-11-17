using System.Globalization;
using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Catalog;

/// <summary>
/// Read-only access to the configurable education catalog (countries, curricula, grades, subjects).
/// All queries are no-tracking projections; names are resolved to the request language.
/// The catalog is small and bounded, so these lists are not paginated.
/// </summary>
public sealed class CatalogService(IAppDbContext db, IRequestLanguage language)
{
    private const int IsoCodeLength = 2;

    public async Task<IReadOnlyList<CountryDto>> GetCountriesAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Countries.AsNoTracking()
            .Where(country => country.IsActive)
            .OrderBy(country => country.SortOrder)
            .Select(country => new { country.IsoCode, country.Name, country.DefaultTimeZoneId })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new CountryDto(row.IsoCode, row.Name.Resolve(language.Code), row.DefaultTimeZoneId));
    }

    public async Task<Result<IReadOnlyList<CurriculumDto>>> GetCurriculaForCountryAsync(
        string isoCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(isoCode);
        if (isoCode.Length != IsoCodeLength || !isoCode.All(char.IsAsciiLetter))
        {
            return Error.Validation(
                ErrorCodes.Catalog.InvalidCountryCode,
                "Country code must be two letters (ISO 3166-1 alpha-2).",
                "isoCode");
        }

        var normalizedCode = isoCode.ToUpperInvariant();
        var countryExists = await db.Countries.AsNoTracking()
            .AnyAsync(country => country.IsoCode == normalizedCode && country.IsActive, cancellationToken);
        if (!countryExists)
        {
            return Error.NotFound(ErrorCodes.Catalog.CountryNotFound, "Country not found.");
        }

        var rows = await db.CurriculumAvailabilities.AsNoTracking()
            .Where(link => link.Country!.IsoCode == normalizedCode && link.Curriculum!.IsActive)
            .Select(link => link.Curriculum!)
            .OrderBy(curriculum => curriculum.SortOrder)
            .Select(curriculum => new
            {
                curriculum.Id,
                curriculum.Code,
                curriculum.Name,
                HasStreams = curriculum.Streams.Any(),
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new CurriculumDto(row.Id, row.Code, row.Name.Resolve(language.Code), row.HasStreams));
    }

    public async Task<Result<IReadOnlyList<CurriculumStreamDto>>> GetStreamsAsync(
        int curriculumId, CancellationToken cancellationToken)
    {
        if (!await CurriculumExistsAsync(curriculumId, cancellationToken))
        {
            return CurriculumNotFound();
        }

        var rows = await db.CurriculumStreams.AsNoTracking()
            .Where(stream => stream.CurriculumId == curriculumId)
            .OrderBy(stream => stream.SortOrder)
            .Select(stream => new { stream.Id, stream.Code, stream.Name })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new CurriculumStreamDto(row.Id, row.Code, row.Name.Resolve(language.Code)));
    }

    public async Task<Result<IReadOnlyList<GradeLevelDto>>> GetGradeLevelsAsync(
        int curriculumId, CancellationToken cancellationToken)
    {
        if (!await CurriculumExistsAsync(curriculumId, cancellationToken))
        {
            return CurriculumNotFound();
        }

        var rows = await db.GradeLevels.AsNoTracking()
            .Where(grade => grade.CurriculumId == curriculumId)
            .OrderBy(grade => grade.Ordinal)
            .Select(grade => new { grade.Id, grade.Code, grade.Name, grade.Ordinal })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new GradeLevelDto(row.Id, row.Code, row.Name.Resolve(language.Code), row.Ordinal));
    }

    public async Task<Result<IReadOnlyList<AcademicYearDto>>> GetAcademicYearsAsync(
        int curriculumId, CancellationToken cancellationToken)
    {
        if (!await CurriculumExistsAsync(curriculumId, cancellationToken))
        {
            return CurriculumNotFound();
        }

        return await db.AcademicYears.AsNoTracking()
            .Where(year => year.CurriculumId == curriculumId)
            .OrderByDescending(year => year.StartDate)
            .Select(year => new AcademicYearDto(year.Id, year.Label, year.StartDate, year.EndDate))
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyList<SubjectDto>>> GetSubjectsForGradeLevelAsync(
        int gradeLevelId, CancellationToken cancellationToken)
    {
        var gradeExists = await db.GradeLevels.AsNoTracking()
            .AnyAsync(grade => grade.Id == gradeLevelId && grade.Curriculum!.IsActive, cancellationToken);
        if (!gradeExists)
        {
            return Error.NotFound(ErrorCodes.Catalog.GradeLevelNotFound, "Grade level not found.");
        }

        var rows = await db.GradeSubjects.AsNoTracking()
            .Where(link => link.GradeLevelId == gradeLevelId && link.Subject!.IsActive)
            .Select(link => new { link.Subject!.Id, link.Subject.Code, link.Subject.Name })
            .ToListAsync(cancellationToken);

        var comparer = StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true);
        return rows
            .Select(row => new SubjectDto(row.Id, row.Code, row.Name.Resolve(language.Code)))
            .OrderBy(subject => subject.Name, comparer)
            .ToList();
    }

    private Task<bool> CurriculumExistsAsync(int curriculumId, CancellationToken cancellationToken) =>
        db.Curricula.AsNoTracking()
            .AnyAsync(curriculum => curriculum.Id == curriculumId && curriculum.IsActive, cancellationToken);

    private static Error CurriculumNotFound() =>
        Error.NotFound(ErrorCodes.Catalog.CurriculumNotFound, "Curriculum not found.");
}
