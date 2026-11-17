namespace ETutor.Domain.Common;

/// <summary>
/// A short text (e.g. a catalog name) with one value per language.
/// Adding a language requires no schema change because values are stored as a map.
/// </summary>
public sealed class LocalizedText : IEquatable<LocalizedText>
{
    private readonly Dictionary<string, string> _values;

    public LocalizedText(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (!values.TryGetValue(LanguageCodes.Default, out var defaultValue) || string.IsNullOrWhiteSpace(defaultValue))
        {
            throw new ArgumentException($"A '{LanguageCodes.Default}' value is required.", nameof(values));
        }

        _values = values
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(pair => pair.Key.ToLowerInvariant(), pair => pair.Value.Trim(), StringComparer.Ordinal);
    }

    public IReadOnlyDictionary<string, string> Values => _values;

    public static LocalizedText Create(string english, string arabic) =>
        new(new Dictionary<string, string>
        {
            [LanguageCodes.English] = english,
            [LanguageCodes.Arabic] = arabic,
        });

    /// <summary>Returns the value for <paramref name="languageCode"/>, falling back to the default language.</summary>
    public string Resolve(string? languageCode)
    {
        if (languageCode is not null && _values.TryGetValue(languageCode.ToLowerInvariant(), out var value))
        {
            return value;
        }

        return _values[LanguageCodes.Default];
    }

    public bool Equals(LocalizedText? other) =>
        other is not null
        && _values.Count == other._values.Count
        && _values.All(pair => other._values.TryGetValue(pair.Key, out var value) && value == pair.Value);

    public override bool Equals(object? obj) => Equals(obj as LocalizedText);

    public override int GetHashCode() =>
        _values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value));

    public override string ToString() => Resolve(LanguageCodes.Default);
}
