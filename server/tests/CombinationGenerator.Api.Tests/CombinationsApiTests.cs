using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CombinationGenerator.Api.Tests;

public sealed class CombinationsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Start_returns_the_total_as_a_string_so_20_factorial_survives_JavaScript()
    {
        var client = await factory.CreateGuestClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/combinations/start", new { n = 20 }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.String, body.GetProperty("totalCount").ValueKind);
        Assert.Equal("2432902008176640000", body.GetProperty("totalCount").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task Start_rejects_n_outside_the_allowed_range(int n)
    {
        var client = await factory.CreateGuestClientAsync(Ct);

        var response = await client.PostAsJsonAsync("/api/combinations/start", new { n }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Full_flow_next_then_all_then_back_to_next()
    {
        var client = await factory.CreateGuestClientAsync(Ct);
        var sessionId = await StartAsync(client, n: 3);

        var first = await ReadJsonAsync(await client.PostAsync($"/api/combinations/{sessionId}/next", null, Ct));
        Assert.Equal("1", first.GetProperty("combination").GetProperty("ordinal").GetString());
        Assert.Equal("[1,2,3]", first.GetProperty("combination").GetProperty("values").GetRawText());

        var page = await ReadJsonAsync(await client.PostAsJsonAsync(
            $"/api/combinations/{sessionId}/all", new { pageSize = 2, pageNumber = 1 }, Ct));
        Assert.Equal("1", page.GetProperty("anchor").GetString());
        Assert.Equal("3", page.GetProperty("totalPages").GetString());
        Assert.Equal("2", page.GetProperty("items")[0].GetProperty("ordinal").GetString());

        var next = await ReadJsonAsync(await client.PostAsync($"/api/combinations/{sessionId}/next", null, Ct));
        Assert.Equal("4", next.GetProperty("combination").GetProperty("ordinal").GetString());
    }

    [Fact]
    public async Task Next_returns_a_message_when_there_are_no_more_combinations()
    {
        var client = await factory.CreateGuestClientAsync(Ct);
        var sessionId = await StartAsync(client, n: 1);
        await client.PostAsync($"/api/combinations/{sessionId}/next", null, Ct);

        var response = await client.PostAsync($"/api/combinations/{sessionId}/next", null, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("combination").ValueKind);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Requests_without_a_token_are_rejected()
    {
        var anonymous = factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/api/combinations/start", new { n = 3 }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A_user_cannot_access_another_users_session()
    {
        var alice = await factory.CreateGuestClientAsync(Ct);
        var mallory = await factory.CreateGuestClientAsync(Ct);
        var aliceSession = await StartAsync(alice, n: 4);

        var response = await mallory.PostAsync($"/api/combinations/{aliceSession}/next", null, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Page_beyond_the_last_page_returns_problem_details()
    {
        var client = await factory.CreateGuestClientAsync(Ct);
        var sessionId = await StartAsync(client, n: 3);

        var response = await client.PostAsJsonAsync(
            $"/api/combinations/{sessionId}/all", new { pageSize = 5, pageNumber = 3 }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.True(body.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Huge_page_numbers_can_be_sent_as_strings()
    {
        var client = await factory.CreateGuestClientAsync(Ct);
        var sessionId = await StartAsync(client, n: 20);

        var response = await client.PostAsJsonAsync(
            $"/api/combinations/{sessionId}/all",
            new { pageSize = 1, pageNumber = "2432902008176640000", anchor = "0" },
            Ct);

        var body = await ReadJsonAsync(response);
        Assert.Equal("2432902008176640000", body.GetProperty("items")[0].GetProperty("ordinal").GetString());
    }

    [Fact]
    public async Task Responses_carry_security_headers()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
    }

    private static async Task<string> StartAsync(HttpClient client, int n)
    {
        var response = await client.PostAsJsonAsync("/api/combinations/start", new { n }, Ct);
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("sessionId").GetString()!;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
}
