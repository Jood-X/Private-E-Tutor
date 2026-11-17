using ETutor.Domain.Common;
using ETutor.Infrastructure.Persistence.Seed;

namespace ETutor.UnitTests.Infrastructure;

/// <summary>Guards the integrity of reference data before it reaches a migration.</summary>
public sealed class CatalogSeedDataTests
{
    [Fact]
    public void Ids_AndCodes_AreUnique()
    {
        AssertUnique(CatalogSeedData.Countries.Select(country => country.Id));
        AssertUnique(CatalogSeedData.Countries.Select(country => country.IsoCode));
        AssertUnique(CatalogSeedData.Curricula.Select(curriculum => curriculum.Id));
        AssertUnique(CatalogSeedData.Curricula.Select(curriculum => curriculum.Code));
        AssertUnique(CatalogSeedData.GradeLevels.Select(grade => grade.Id));
        AssertUnique(CatalogSeedData.GradeLevels.Select(grade => (grade.CurriculumId, grade.Code)));
        AssertUnique(CatalogSeedData.GradeLevels.Select(grade => (grade.CurriculumId, grade.Ordinal)));
        AssertUnique(CatalogSeedData.Subjects.Select(subject => subject.Id));
        AssertUnique(CatalogSeedData.Subjects.Select(subject => subject.Code));
        AssertUnique(CatalogSeedData.GradeSubjects.Select(link => (link.GradeLevelId, link.SubjectId)));
        AssertUnique(CatalogSeedData.CurriculumAvailabilities.Select(link => (link.CountryId, link.CurriculumId)));
    }

    [Fact]
    public void Every_Curriculum_IsAvailableSomewhere_AndHasGradesAndAnAcademicYear()
    {
        foreach (var curriculum in CatalogSeedData.Curricula)
        {
            Assert.Contains(CatalogSeedData.CurriculumAvailabilities, link => link.CurriculumId == curriculum.Id);
            Assert.Contains(CatalogSeedData.GradeLevels, grade => grade.CurriculumId == curriculum.Id);
            Assert.Contains(CatalogSeedData.AcademicYears, year => year.CurriculumId == curriculum.Id);
        }
    }

    [Fact]
    public void ForeignKeys_ReferenceExistingRows()
    {
        var countryIds = CatalogSeedData.Countries.Select(country => country.Id).ToHashSet();
        var curriculumIds = CatalogSeedData.Curricula.Select(curriculum => curriculum.Id).ToHashSet();
        var gradeIds = CatalogSeedData.GradeLevels.Select(grade => grade.Id).ToHashSet();
        var subjectIds = CatalogSeedData.Subjects.Select(subject => subject.Id).ToHashSet();

        Assert.All(CatalogSeedData.CurriculumAvailabilities, link =>
        {
            Assert.Contains(link.CountryId, countryIds);
            Assert.Contains(link.CurriculumId, curriculumIds);
        });
        Assert.All(CatalogSeedData.Streams, stream => Assert.Contains(stream.CurriculumId, curriculumIds));
        Assert.All(CatalogSeedData.GradeLevels, grade => Assert.Contains(grade.CurriculumId, curriculumIds));
        Assert.All(CatalogSeedData.GradeSubjects, link =>
        {
            Assert.Contains(link.GradeLevelId, gradeIds);
            Assert.Contains(link.SubjectId, subjectIds);
        });
    }

    [Fact]
    public void All_Names_HaveEnglishAndArabic()
    {
        var names = CatalogSeedData.Countries.Select(item => item.Name)
            .Concat(CatalogSeedData.Curricula.Select(item => item.Name))
            .Concat(CatalogSeedData.Streams.Select(item => item.Name))
            .Concat(CatalogSeedData.GradeLevels.Select(item => item.Name))
            .Concat(CatalogSeedData.Subjects.Select(item => item.Name));

        Assert.All(names, name =>
        {
            Assert.True(name.Values.ContainsKey(LanguageCodes.English));
            Assert.True(name.Values.ContainsKey(LanguageCodes.Arabic));
        });
    }

    [Fact]
    public void AcademicYears_StartBeforeTheyEnd()
    {
        Assert.All(CatalogSeedData.AcademicYears, year => Assert.True(year.StartDate < year.EndDate));
    }

    [Fact]
    public void Countries_UseValidIanaTimeZones()
    {
        Assert.All(CatalogSeedData.Countries, country =>
            Assert.True(TimeZoneInfo.TryFindSystemTimeZoneById(country.DefaultTimeZoneId, out _), country.DefaultTimeZoneId));
    }

    private static void AssertUnique<T>(IEnumerable<T> values)
    {
        var list = values.ToList();
        Assert.Equal(list.Count, list.Distinct().Count());
    }
}
