namespace CombinationGenerator.Api.Infrastructure;

internal static class ApiPipelineExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseSecurityHeaders();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHsts();
        }

        if (!app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER") && !app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(ApiServiceCollectionExtensions.CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        return app;
    }
}
