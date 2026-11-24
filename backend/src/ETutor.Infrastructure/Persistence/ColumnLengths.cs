namespace ETutor.Infrastructure.Persistence;

/// <summary>Shared column sizes so the same concept has the same width in every table.</summary>
internal static class ColumnLengths
{
    public const int IsoCountryCode = 2;
    public const int Code = 40;
    public const int TimeZoneId = 64;
    public const int AcademicYearLabel = 20;
    public const int DisplayName = 100;
    public const int LanguageCode = 10;
    public const int Headline = 150;
    public const int Bio = 2000;
    public const int Title = 200;
    public const int ShortDescription = 2000;
    public const int LongDescription = 4000;
    public const int Url = 500;
    public const int Note = 500;
}
