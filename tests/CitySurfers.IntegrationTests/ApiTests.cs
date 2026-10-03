using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CitySurfers.Application.Authentication;

namespace CitySurfers.IntegrationTests;

public sealed class ApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Liveness_returns_200()
    {
        var response = await factory.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.ServiceUnavailable)]
    public async Task Readiness_reflects_database_but_liveness_remains_healthy(bool available, HttpStatusCode expected)
    {
        await using var app = new ApiFactory { DatabaseAvailable = available };
        using var client = app.CreateClient();
        Assert.Equal(expected, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }

    [Fact]
    public async Task Valid_login_returns_store_data_without_credentials_or_token()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { username = "demo", password = "1234" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new LoginResult("persisted-user", "demo", "Persisted Test Runner"),
            await response.Content.ReadFromJsonAsync<LoginResult>());
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(3, json.RootElement.EnumerateObject().Count());
        Assert.False(json.RootElement.TryGetProperty("demoPassword", out _));
        Assert.False(json.RootElement.TryGetProperty("token", out _));
    }

    [Theory]
    [InlineData("demo", "wrong")]
    [InlineData("unknown", "1234")]
    public async Task Invalid_login_returns_generic_401_problem(string username, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid username or password.", json.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"username\":\"demo\"}")]
    [InlineData("{\"username\":\"\",\"password\":\"1234\"}")]
    [InlineData("{\"username\":\"demo\",\"password\":null}")]
    [InlineData("null")]
    [InlineData("{broken")]
    public async Task Invalid_payload_returns_validation_problem(string body)
    {
        var response = await factory.CreateClient().PostAsync("/api/auth/login",
            new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("text/html")]
    [InlineData("text/plain")]
    public async Task Store_failure_returns_sanitized_500(string accept)
    {
        await using var app = new ApiFactory { StoreThrows = true };
        using var client = app.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd(accept);
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username = "demo", password = "1234" });
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-database-secret", body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain("stack", body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://frontend.example", true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Cors_only_allows_configured_origin_without_credentials(string origin, bool allowed)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        var response = await factory.CreateClient().SendAsync(request);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        if (allowed)
            Assert.Equal(origin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Development_openapi_contains_all_required_endpoints()
    {
        await using var app = new ApiFactory { EnvironmentName = "Development" };
        var response = await app.CreateClient().GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/health", out _));
        Assert.True(paths.TryGetProperty("/health/ready", out _));
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
    }

    [Fact]
    public async Task Production_openapi_is_not_exposed()
    {
        Assert.Equal(HttpStatusCode.NotFound,
            (await factory.CreateClient().GetAsync("/openapi/v1.json")).StatusCode);
    }
}
