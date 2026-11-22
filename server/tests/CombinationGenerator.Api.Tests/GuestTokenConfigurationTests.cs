using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using CombinationGenerator.Api.Security;
using Microsoft.AspNetCore.Hosting;

namespace CombinationGenerator.Api.Tests;

public sealed class GuestTokenConfigurationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void Outside_development_the_api_refuses_to_start_without_a_shared_signing_key()
    {
        using var production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Auth:AllowGuestTokensOutsideDevelopment", "true");
        });

        var error = Assert.ThrowsAny<Exception>(() => production.CreateClient());

        Assert.Contains("GuestSigningKey", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task With_a_shared_key_a_token_from_one_instance_is_accepted_by_another()
    {
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        using var instanceA = factory.WithWebHostBuilder(builder => builder.UseSetting("Auth:GuestSigningKey", key));
        using var instanceB = factory.WithWebHostBuilder(builder => builder.UseSetting("Auth:GuestSigningKey", key));

        var tokenResponse = await instanceA.CreateClient().PostAsync("/api/auth/guest-token", content: null, Ct);
        var token = await tokenResponse.Content.ReadFromJsonAsync<GuestToken>(Ct);

        var clientB = instanceB.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var response = await clientB.PostAsJsonAsync("/api/combinations/start", new { n = 3 }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
