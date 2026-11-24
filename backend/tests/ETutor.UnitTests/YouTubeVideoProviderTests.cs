using System.Net;
using System.Text;
using System.Text.Json;
using ETutor.Application.Abstractions;
using ETutor.Infrastructure.Videos;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ETutor.UnitTests;

public sealed class YouTubeVideoProviderTests
{
    private const string Id = "dQw4w9WgXcQ";

    [Theory]
    [InlineData(Id)]
    [InlineData("https://www.youtube.com/watch?v=" + Id)]
    [InlineData("https://youtube.com/watch?feature=share&v=" + Id + "&t=42s")]
    [InlineData("https://m.youtube.com/watch?v=" + Id)]
    [InlineData("youtu.be/" + Id)]
    [InlineData("https://youtu.be/" + Id + "?si=AbC123")]
    [InlineData("https://www.youtube.com/shorts/" + Id)]
    [InlineData("https://www.youtube.com/embed/" + Id)]
    [InlineData("https://www.youtube-nocookie.com/embed/" + Id + "?rel=0")]
    [InlineData("https://www.youtube.com/live/" + Id)]
    [InlineData("  https://www.youtube.com/watch?v=" + Id + "  ")]
    public void Parses_CommonLinkShapes(string input)
    {
        Assert.True(YouTubeVideoProvider.TryParse(input, out var assetId));
        Assert.Equal(Id, assetId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("dQw4w9WgXc")] // 10 characters
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXc!")]
    [InlineData("https://evil.example/watch?v=" + Id)]
    [InlineData("https://youtube.com.evil.example/watch?v=" + Id)]
    [InlineData("https://www.youtube.com/channel/UC1234567890")]
    [InlineData("https://www.youtube.com/playlist?list=PL123")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://youtu.be/" + Id)]
    public void Refuses_AnythingElse(string input) => Assert.False(YouTubeVideoProvider.TryParse(input, out _));

    [Theory]
    [InlineData("PT1H2M3S", 3723)]
    [InlineData("PT15M", 900)]
    [InlineData("PT45S", 45)]
    [InlineData("P0D", null)] // live stream
    [InlineData("nonsense", null)]
    [InlineData(null, null)]
    public void ParsesIsoDurations(string? iso, int? seconds) => Assert.Equal(seconds, YouTubeVideoProvider.ParseDuration(iso));

    [Fact]
    public void DataApi_FoundVideo_IsMapped()
    {
        var lookup = YouTubeVideoProvider.ReadDataApiResponse(Json("""
            {"items":[{"snippet":{"title":"Newton's laws","thumbnails":{"default":{"url":"d.jpg"},"medium":{"url":"m.jpg"}}},
              "contentDetails":{"duration":"PT12M5S"},"status":{"privacyStatus":"unlisted","embeddable":true}}]}
            """));

        Assert.Equal(VideoLookupStatus.Found, lookup.Status);
        Assert.Equal(new VideoMetadata("Newton's laws", 725, "m.jpg"), lookup.Metadata);
    }

    [Theory]
    [InlineData("""{"items":[]}""", VideoLookupStatus.NotFound)]
    [InlineData("""{"items":[{"snippet":{"title":"x"},"status":{"privacyStatus":"private","embeddable":true}}]}""", VideoLookupStatus.NotFound)]
    [InlineData("""{"items":[{"snippet":{"title":"x"},"status":{"privacyStatus":"public","embeddable":false}}]}""", VideoLookupStatus.NotEmbeddable)]
    public void DataApi_RefusedVideos_AreMapped(string json, VideoLookupStatus status) =>
        Assert.Equal(status, YouTubeVideoProvider.ReadDataApiResponse(Json(json)).Status);

    [Fact]
    public async Task DataApi_SendsTheKeyInAHeader_NotTheUrl()
    {
        var handler = new StubHandler(HttpStatusCode.OK, """{"items":[]}""");
        var provider = Provider(handler, apiKey: "secret-key");

        await provider.LookupAsync(Id, CancellationToken.None);

        Assert.Equal("secret-key", handler.LastRequest!.Headers.GetValues("X-Goog-Api-Key").Single());
        Assert.DoesNotContain("secret", handler.LastRequest.RequestUri!.ToString(), StringComparison.Ordinal);
        Assert.Contains("id=" + Id, handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, VideoLookupStatus.Found)]
    [InlineData(HttpStatusCode.Unauthorized, VideoLookupStatus.NotEmbeddable)]
    [InlineData(HttpStatusCode.NotFound, VideoLookupStatus.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError, VideoLookupStatus.ProviderUnavailable)]
    public async Task WithoutAKey_OEmbedIsUsed(HttpStatusCode status, VideoLookupStatus expected)
    {
        var handler = new StubHandler(status, """{"title":"Waves","thumbnail_url":"https://i.ytimg.com/vi/x/hq.jpg"}""");

        var lookup = await Provider(handler, apiKey: null).LookupAsync(Id, CancellationToken.None);

        Assert.Equal(expected, lookup.Status);
        Assert.Equal("www.youtube.com", handler.LastRequest!.RequestUri!.Host);
        if (expected == VideoLookupStatus.Found)
        {
            Assert.Equal(new VideoMetadata("Waves", null, "https://i.ytimg.com/vi/x/hq.jpg"), lookup.Metadata);
        }
    }

    [Fact]
    public async Task NetworkFailures_MeanProviderUnavailable()
    {
        var lookup = await Provider(new StubHandler(new HttpRequestException("offline")), apiKey: null).LookupAsync(Id, CancellationToken.None);

        Assert.Equal(VideoLookupStatus.ProviderUnavailable, lookup.Status);
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static YouTubeVideoProvider Provider(StubHandler handler, string? apiKey) => new(
        new HttpClient(handler),
        Options.Create(new YouTubeOptions { ApiKey = apiKey }),
        NullLogger<YouTubeVideoProvider>.Instance);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode status;
        private readonly string body = string.Empty;
        private readonly Exception? failure;

        public StubHandler(HttpStatusCode status, string body) => (this.status, this.body) = (status, body);

        public StubHandler(Exception failure) => this.failure = failure;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return failure is not null
                ? Task.FromException<HttpResponseMessage>(failure)
                : Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
