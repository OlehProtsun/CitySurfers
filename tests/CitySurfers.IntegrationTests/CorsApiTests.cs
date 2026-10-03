using System.Net;

namespace CitySurfers.IntegrationTests;

public sealed class CorsApiTests
{
    [Theory]
    [InlineData("https://frontend.example", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task OnlyConfiguredOriginReceivesAllowOrigin(string origin, bool allowed)
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/home");
        request.Headers.Add("Origin", origin);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        if (allowed)
            Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        else
            Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task ProgressPreflightAllowsConfiguredOriginMethodAndContentTypeWithoutCredentials()
    {
        await using var app = new ApiFactory();
        using var client = app.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/runs/run-id/progress");
        request.Headers.Add("Origin", "https://frontend.example");
        request.Headers.Add("Access-Control-Request-Method", "PATCH");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://frontend.example", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("PATCH", response.Headers.GetValues("Access-Control-Allow-Methods"));
        Assert.Contains("content-type", response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }
}
