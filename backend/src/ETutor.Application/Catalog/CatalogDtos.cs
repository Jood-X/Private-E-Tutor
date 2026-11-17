namespace ETutor.Application.Catalog;

public sealed record CountryDto(string IsoCode, string Name, string DefaultTimeZoneId);

public sealed record CurriculumDto(int Id, string Code, string Name, bool HasStreams);

public sealed record CurriculumStreamDto(int Id, string Code, string Name);

public sealed record GradeLevelDto(int Id, string Code, string Name, int Ordinal);

public sealed record SubjectDto(int Id, string Code, string Name);

public sealed record AcademicYearDto(int Id, string Label, DateOnly StartDate, DateOnly EndDate);
