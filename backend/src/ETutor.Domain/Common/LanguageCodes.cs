namespace ETutor.Domain.Common;

/// <summary>ISO 639-1 codes of the languages the platform supports.</summary>
public static class LanguageCodes
{
    public const string English = "en";
    public const string Arabic = "ar";

    /// <summary>The language used when a requested translation is missing.</summary>
    public const string Default = English;

    public static IReadOnlyList<string> Supported { get; } = [English, Arabic];

    public static bool IsSupported(string? code) =>
        code is not null && Supported.Contains(code, StringComparer.OrdinalIgnoreCase);
}
