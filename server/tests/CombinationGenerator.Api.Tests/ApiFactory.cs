using System.Net.Http.Headers;
using System.Net.Http.Json;
using CombinationGenerator.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CombinationGenerator.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("RateLimiting:GuestTokensPerMinute", "1000");
        builder.UseSetting("RateLimiting:RequestsPerSecond", "1000");
        builder.UseSetting("RateLimiting:BurstLimit", "1000");
    }

    // A client authenticated as a brand-new guest user.
    public async Task<HttpClient> CreateGuestClientAsync(CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var response = await client.PostAsync("/api/auth/guest-token", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<GuestToken>(cancellationToken);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        return client;
    }
}
