using System.Net;

namespace CitySurfers.IntegrationTests;

public sealed class ReadSideErrorTests
{
    [Theory]
    [InlineData("/api/runs/history")]
    [InlineData("/api/progress")]
    [InlineData("/api/leaderboards/today")]
    [InlineData("/api/leaderboards/month")]
    [InlineData("/api/rivals/current")]
    [InlineData("/api/goals/next")]
    public async Task PersistenceFailureIsSanitizedProblem(string path)
    {
        await using var app = new ApiFactory { HistoryThrows = true };
        using var client = app.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-history-database-secret", body);
        Assert.DoesNotContain("InvalidOperationException", body);
    }
}
