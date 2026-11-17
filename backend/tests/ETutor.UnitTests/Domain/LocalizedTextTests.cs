using ETutor.Domain.Common;

namespace ETutor.UnitTests.Domain;

public sealed class LocalizedTextTests
{
    [Fact]
    public void Resolve_ReturnsRequestedLanguage_WhenAvailable()
    {
        var text = LocalizedText.Create("Physics", "الفيزياء");

        Assert.Equal("الفيزياء", text.Resolve("ar"));
        Assert.Equal("الفيزياء", text.Resolve("AR"));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData(null)]
    public void Resolve_FallsBackToEnglish_WhenLanguageMissing(string? language)
    {
        var text = LocalizedText.Create("Physics", "الفيزياء");

        Assert.Equal("Physics", text.Resolve(language));
    }

    [Fact]
    public void Constructor_Throws_WhenEnglishIsMissing()
    {
        var values = new Dictionary<string, string> { ["ar"] = "الفيزياء" };

        Assert.Throws<ArgumentException>(() => new LocalizedText(values));
    }

    [Fact]
    public void Constructor_DropsBlankTranslations_AndTrimsValues()
    {
        var text = new LocalizedText(new Dictionary<string, string> { ["en"] = " Physics ", ["ar"] = " " });

        Assert.Equal("Physics", text.Resolve("en"));
        Assert.False(text.Values.ContainsKey("ar"));
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = LocalizedText.Create("Physics", "الفيزياء");
        var second = LocalizedText.Create("Physics", "الفيزياء");
        var different = LocalizedText.Create("Physics", "فيزياء");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, different);
    }
}
