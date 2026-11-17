using System.Net;
using System.Net.Http.Headers;
using ETutor.Application.Catalog;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Catalog;

[Collection(ApiTestGroup.Name)]
public sealed class CatalogEndpointsTests(ApiFactory factory)
{
    private const string Base = "/api/v1/catalog";
    private const int TawjihiId = 1;
    private const int CambridgeYear12Id = 812;
    private const string IslamicEducation = "islamic-education";

    [Fact]
    public async Task GetCountries_ReturnsSeededCountries_InEnglishByDefault()
    {
        var response = await factory.CreateClient().GetAsync($"{Base}/countries");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.ReadEnvelopeAsync<List<CountryDto>>();
        Assert.True(envelope.Success);
        Assert.Null(envelope.Errors);
        Assert.Equal(7, envelope.Data!.Count);
        var jordan = envelope.Data[0];
        Assert.Equal("JO", jordan.IsoCode);
        Assert.Equal("Jordan", jordan.Name);
        Assert.Equal("Asia/Amman", jordan.DefaultTimeZoneId);
    }

    [Fact]
    public async Task GetCountries_ReturnsArabicNames_WhenRequested()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ar"));

        var envelope = await (await client.GetAsync($"{Base}/countries")).ReadEnvelopeAsync<List<CountryDto>>();

        Assert.Equal("الأردن", envelope.Data![0].Name);
    }

    [Fact]
    public async Task GetCountries_FallsBackToEnglish_ForUnsupportedLanguage()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("fr"));

        var envelope = await (await client.GetAsync($"{Base}/countries")).ReadEnvelopeAsync<List<CountryDto>>();

        Assert.Equal("Jordan", envelope.Data![0].Name);
    }

    [Theory]
    [InlineData("JO")]
    [InlineData("jo")]
    public async Task GetCurricula_ForJordan_ReturnsNationalAndInternationalCurricula(string code)
    {
        var envelope = await (await factory.CreateClient().GetAsync($"{Base}/countries/{code}/curricula"))
            .ReadEnvelopeAsync<List<CurriculumDto>>();

        var codes = envelope.Data!.Select(curriculum => curriculum.Code).ToList();
        Assert.Equal(["jo-tawjihi", "cambridge", "ib-dp", "american"], codes);
        Assert.True(envelope.Data![0].HasStreams);
        Assert.False(envelope.Data![1].HasStreams);
    }

    [Fact]
    public async Task GetCurricula_UnknownCountry_Returns404WithCodeAndMatchingTraceId()
    {
        var response = await factory.CreateClient().GetAsync($"{Base}/countries/ZZ/curricula");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.False(envelope.Success);
        Assert.Null(envelope.Data);
        Assert.Equal("catalog.country_not_found", Assert.Single(envelope.Errors!).Code);
        Assert.False(string.IsNullOrWhiteSpace(envelope.TraceId));
        Assert.Equal(envelope.TraceId, response.Headers.GetValues("X-Trace-Id").Single());
    }

    [Theory]
    [InlineData("JOR")]
    [InlineData("J1")]
    public async Task GetCurricula_InvalidCountryCode_Returns400ForField(string code)
    {
        var response = await factory.CreateClient().GetAsync($"{Base}/countries/{code}/curricula");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.ReadEnvelopeAsync<object>()).Errors!);
        Assert.Equal("catalog.invalid_country_code", error.Code);
        Assert.Equal("isoCode", error.Field);
    }

    [Fact]
    public async Task GetStreams_ForTawjihi_ReturnsOrderedStreams()
    {
        var envelope = await (await factory.CreateClient().GetAsync($"{Base}/curricula/{TawjihiId}/streams"))
            .ReadEnvelopeAsync<List<CurriculumStreamDto>>();

        Assert.Equal(["scientific", "literary"], envelope.Data!.Select(stream => stream.Code));
    }

    [Fact]
    public async Task GetGradeLevels_ReturnsGradesInOrder()
    {
        var envelope = await (await factory.CreateClient().GetAsync($"{Base}/curricula/{TawjihiId}/grade-levels"))
            .ReadEnvelopeAsync<List<GradeLevelDto>>();

        Assert.Equal([10, 11, 12], envelope.Data!.Select(grade => grade.Ordinal));
    }

    [Fact]
    public async Task GetGradeLevels_UnknownCurriculum_Returns404()
    {
        var response = await factory.CreateClient().GetAsync($"{Base}/curricula/9999/grade-levels");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("catalog.curriculum_not_found", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task GetAcademicYears_ReturnsCurrentYear()
    {
        var envelope = await (await factory.CreateClient().GetAsync($"{Base}/curricula/{TawjihiId}/academic-years"))
            .ReadEnvelopeAsync<List<AcademicYearDto>>();

        var year = Assert.Single(envelope.Data!);
        Assert.Equal("2026/2027", year.Label);
        Assert.True(year.StartDate < year.EndDate);
    }

    [Fact]
    public async Task GetSubjects_ForInternationalGrade_ExcludesIslamicEducation()
    {
        var international = await (await factory.CreateClient().GetAsync($"{Base}/grade-levels/{CambridgeYear12Id}/subjects"))
            .ReadEnvelopeAsync<List<SubjectDto>>();
        var tawjihi = await (await factory.CreateClient().GetAsync($"{Base}/grade-levels/112/subjects"))
            .ReadEnvelopeAsync<List<SubjectDto>>();

        Assert.DoesNotContain(international.Data!, subject => subject.Code == IslamicEducation);
        Assert.Contains(tawjihi.Data!, subject => subject.Code == IslamicEducation);
    }

    [Fact]
    public async Task GetSubjects_UnknownGrade_Returns404()
    {
        var response = await factory.CreateClient().GetAsync($"{Base}/grade-levels/1/subjects");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("catalog.grade_level_not_found", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }
}
