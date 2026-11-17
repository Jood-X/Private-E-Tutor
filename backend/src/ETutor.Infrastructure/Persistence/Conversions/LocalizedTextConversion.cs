using System.Text.Json;
using ETutor.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ETutor.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores <see cref="LocalizedText"/> as a JSON object, e.g. {"en":"Physics","ar":"الفيزياء"}.
/// </summary>
internal static class LocalizedTextConversion
{
    /// <summary>Upper bound for short catalog names in all languages combined.</summary>
    public const int MaxJsonLength = 500;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Keep Arabic readable in the database instead of \u-escaped.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly ValueConverter<LocalizedText, string> Converter = new(
        text => JsonSerializer.Serialize(text.Values, JsonOptions),
        json => new LocalizedText(JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)!));

    private static readonly ValueComparer<LocalizedText> Comparer = new(
        (left, right) => Equals(left, right),
        text => text.GetHashCode(),
        text => new LocalizedText(new Dictionary<string, string>(text.Values)));

    public static PropertyBuilder<LocalizedText> IsLocalizedText(this PropertyBuilder<LocalizedText> property) =>
        property
            .HasConversion(Converter, Comparer)
            .HasMaxLength(MaxJsonLength)
            .IsUnicode()
            .IsRequired();
}
