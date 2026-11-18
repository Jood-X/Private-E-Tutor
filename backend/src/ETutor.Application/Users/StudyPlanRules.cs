using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Users;

/// <summary>
/// Catalog-consistency rules for a student's study plan: the curriculum is offered in their country,
/// and the grade, stream and subjects all belong to it. The database enforces grade/stream ownership
/// too (composite FKs); checking here first gives field-level errors instead of a constraint failure.
/// </summary>
public sealed class StudyPlanRules(IAppDbContext db)
{
    /// <summary>Resolves an active country's ID from its ISO code, or returns a field error.</summary>
    public async Task<Result<int>> ResolveCountryAsync(string isoCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(isoCode);
        var normalized = isoCode.ToUpperInvariant();
        var countryId = await db.Countries.AsNoTracking()
            .Where(country => country.IsoCode == normalized && country.IsActive)
            .Select(country => (int?)country.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return countryId is { } id
            ? id
            : Error.Validation(ErrorCodes.Catalog.CountryNotFound, "Country not found.", "countryCode");
    }

    public async Task<IReadOnlyList<Error>> ValidateAsync(
        int countryId, int curriculumId, int gradeLevelId, int? streamId, IReadOnlyList<int> subjectIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subjectIds);
        var errors = new List<Error>();

        var curriculumOffered = await db.CurriculumAvailabilities.AsNoTracking().AnyAsync(
            link => link.CountryId == countryId && link.CurriculumId == curriculumId && link.Curriculum!.IsActive,
            cancellationToken);
        if (!curriculumOffered)
        {
            // Everything else depends on the curriculum, so stop here.
            return [Error.Validation(ErrorCodes.Profile.CurriculumNotAvailable,
                "This curriculum is not offered in the selected country.", "curriculumId")];
        }

        var gradeMatches = await db.GradeLevels.AsNoTracking()
            .AnyAsync(grade => grade.Id == gradeLevelId && grade.CurriculumId == curriculumId, cancellationToken);
        if (!gradeMatches)
        {
            errors.Add(Error.Validation(ErrorCodes.Profile.GradeNotInCurriculum,
                "This grade does not belong to the selected curriculum.", "gradeLevelId"));
        }

        if (streamId is { } stream && !await db.CurriculumStreams.AsNoTracking()
                .AnyAsync(item => item.Id == stream && item.CurriculumId == curriculumId, cancellationToken))
        {
            errors.Add(Error.Validation(ErrorCodes.Profile.StreamNotInCurriculum,
                "This stream does not belong to the selected curriculum.", "streamId"));
        }

        if (gradeMatches && subjectIds.Count > 0)
        {
            var taughtCount = await db.GradeSubjects.AsNoTracking()
                .CountAsync(link => link.GradeLevelId == gradeLevelId && subjectIds.Contains(link.SubjectId), cancellationToken);
            if (taughtCount != subjectIds.Count)
            {
                errors.Add(Error.Validation(ErrorCodes.Profile.SubjectNotInGrade,
                    "One or more subjects are not taught at this grade.", "subjectIds"));
            }
        }

        return errors;
    }
}
