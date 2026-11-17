using ETutor.Domain.Catalog;
using ETutor.Domain.Common;

namespace ETutor.Infrastructure.Persistence.Seed;

/// <summary>
/// Reference catalog shipped with every environment through migrations (HasData).
/// IDs are fixed so later migrations stay stable. Content (streams, calendar dates) must be
/// reviewed by the product owner; see docs/database-design.md §12 and docs/known-limitations.md.
/// </summary>
internal static class CatalogSeedData
{
    private const int CambridgeId = 8;
    private const int InternationalBaccalaureateId = 9;
    private const int AmericanId = 10;
    private const int IslamicEducationSubjectId = 7;

    public static IReadOnlyList<Country> Countries { get; } =
    [
        Country(1, "JO", "Jordan", "الأردن", "Asia/Amman"),
        Country(2, "SA", "Saudi Arabia", "السعودية", "Asia/Riyadh"),
        Country(3, "AE", "United Arab Emirates", "الإمارات العربية المتحدة", "Asia/Dubai"),
        Country(4, "KW", "Kuwait", "الكويت", "Asia/Kuwait"),
        Country(5, "QA", "Qatar", "قطر", "Asia/Qatar"),
        Country(6, "BH", "Bahrain", "البحرين", "Asia/Bahrain"),
        Country(7, "OM", "Oman", "عُمان", "Asia/Muscat"),
    ];

    public static IReadOnlyList<Curriculum> Curricula { get; } =
    [
        Curriculum(1, "jo-tawjihi", "Jordanian Tawjihi", "التوجيهي الأردني"),
        Curriculum(2, "sa-national", "Saudi National Curriculum", "المنهج الوطني السعودي"),
        Curriculum(3, "ae-moe", "UAE Ministry of Education", "منهاج وزارة التربية والتعليم الإماراتية"),
        Curriculum(4, "kw-national", "Kuwaiti National Curriculum", "المنهج الوطني الكويتي"),
        Curriculum(5, "qa-national", "Qatari National Curriculum", "المنهج الوطني القطري"),
        Curriculum(6, "bh-national", "Bahraini National Curriculum", "المنهج الوطني البحريني"),
        Curriculum(7, "om-national", "Omani National Curriculum", "المنهج الوطني العُماني"),
        Curriculum(CambridgeId, "cambridge", "Cambridge IGCSE / A Level", "كامبريدج IGCSE / A Level"),
        Curriculum(InternationalBaccalaureateId, "ib-dp", "International Baccalaureate (IB DP)", "البكالوريا الدولية (IB)"),
        Curriculum(AmericanId, "american", "American High School", "المنهج الأمريكي"),
    ];

    /// <summary>National curricula are offered in their own country; international ones everywhere.</summary>
    public static IReadOnlyList<CurriculumAvailability> CurriculumAvailabilities { get; } =
        Countries
            .Select(country => new CurriculumAvailability { CountryId = country.Id, CurriculumId = country.Id })
            .Concat(
                from country in Countries
                from curriculumId in new[] { CambridgeId, InternationalBaccalaureateId, AmericanId }
                select new CurriculumAvailability { CountryId = country.Id, CurriculumId = curriculumId })
            .ToList();

    public static IReadOnlyList<CurriculumStream> Streams { get; } =
    [
        new() { Id = 1, CurriculumId = 1, Code = "scientific", Name = LocalizedText.Create("Scientific", "العلمي"), SortOrder = 1 },
        new() { Id = 2, CurriculumId = 1, Code = "literary", Name = LocalizedText.Create("Literary", "الأدبي"), SortOrder = 2 },
    ];

    public static IReadOnlyList<GradeLevel> GradeLevels { get; } = BuildGradeLevels();

    public static IReadOnlyList<Subject> Subjects { get; } =
    [
        Subject(1, "mathematics", "Mathematics", "الرياضيات"),
        Subject(2, "physics", "Physics", "الفيزياء"),
        Subject(3, "chemistry", "Chemistry", "الكيمياء"),
        Subject(4, "biology", "Biology", "الأحياء"),
        Subject(5, "arabic", "Arabic Language", "اللغة العربية"),
        Subject(6, "english", "English Language", "اللغة الإنجليزية"),
        Subject(IslamicEducationSubjectId, "islamic-education", "Islamic Education", "التربية الإسلامية"),
        Subject(8, "history", "History", "التاريخ"),
        Subject(9, "geography", "Geography", "الجغرافيا"),
        Subject(10, "computer-science", "Computer Science", "علوم الحاسوب"),
        Subject(11, "economics", "Economics", "الاقتصاد"),
        Subject(12, "business", "Business Studies", "إدارة الأعمال"),
    ];

    /// <summary>Every subject at every grade, except Islamic Education for international curricula.</summary>
    public static IReadOnlyList<GradeSubject> GradeSubjects { get; } =
        (from grade in GradeLevels
         from subject in Subjects
         where subject.Id != IslamicEducationSubjectId || grade.CurriculumId < CambridgeId
         select new GradeSubject { GradeLevelId = grade.Id, SubjectId = subject.Id })
        .ToList();

    /// <summary>One academic year per curriculum; the year's ID equals the curriculum's ID for 2026/2027.</summary>
    public static IReadOnlyList<AcademicYear> AcademicYears { get; } =
        Curricula
            .Select(curriculum => new AcademicYear
            {
                Id = curriculum.Id,
                CurriculumId = curriculum.Id,
                Label = "2026/2027",
                StartDate = curriculum.Id < CambridgeId ? new DateOnly(2026, 8, 23) : new DateOnly(2026, 9, 1),
                EndDate = new DateOnly(2027, 6, 30),
            })
            .ToList();

    private static List<GradeLevel> BuildGradeLevels()
    {
        var grades = new List<GradeLevel>();

        // National curricula and the American system: Grades 10-12.
        foreach (var curriculum in Curricula.Where(c => c.Id < CambridgeId || c.Id == AmericanId))
        {
            grades.Add(Grade(curriculum.Id, 10, "g10", "Grade 10", "الصف العاشر"));
            grades.Add(Grade(curriculum.Id, 11, "g11", "Grade 11", "الصف الحادي عشر"));
            grades.Add(Grade(curriculum.Id, 12, "g12", "Grade 12", "الصف الثاني عشر"));
        }

        grades.Add(Grade(CambridgeId, 10, "y10", "Year 10 (IGCSE)", "السنة العاشرة (IGCSE)"));
        grades.Add(Grade(CambridgeId, 11, "y11", "Year 11 (IGCSE)", "السنة الحادية عشرة (IGCSE)"));
        grades.Add(Grade(CambridgeId, 12, "y12", "Year 12 (AS Level)", "السنة الثانية عشرة (AS Level)"));
        grades.Add(Grade(CambridgeId, 13, "y13", "Year 13 (A Level)", "السنة الثالثة عشرة (A Level)"));

        grades.Add(Grade(InternationalBaccalaureateId, 11, "dp1", "DP Year 1", "السنة الأولى من الدبلوما"));
        grades.Add(Grade(InternationalBaccalaureateId, 12, "dp2", "DP Year 2", "السنة الثانية من الدبلوما"));

        return grades;
    }

    /// <summary>Deterministic ID: curriculum ID × 100 + ordinal (e.g. Tawjihi Grade 12 = 112).</summary>
    private static GradeLevel Grade(int curriculumId, byte ordinal, string code, string english, string arabic) => new()
    {
        Id = (curriculumId * 100) + ordinal,
        CurriculumId = curriculumId,
        Code = code,
        Name = LocalizedText.Create(english, arabic),
        Ordinal = ordinal,
    };

    private static Country Country(int id, string isoCode, string english, string arabic, string timeZoneId) => new()
    {
        Id = id,
        IsoCode = isoCode,
        Name = LocalizedText.Create(english, arabic),
        DefaultTimeZoneId = timeZoneId,
        SortOrder = id,
    };

    private static Curriculum Curriculum(int id, string code, string english, string arabic) => new()
    {
        Id = id,
        Code = code,
        Name = LocalizedText.Create(english, arabic),
        SortOrder = id,
    };

    private static Subject Subject(int id, string code, string english, string arabic) => new()
    {
        Id = id,
        Code = code,
        Name = LocalizedText.Create(english, arabic),
    };
}
