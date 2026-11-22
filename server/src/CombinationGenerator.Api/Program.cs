using CombinationGenerator.Api.Infrastructure;
using CombinationGenerator.Application;
using CombinationGenerator.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration, builder.Environment);

var app = builder.Build();
app.UseApi();
app.Run();

// Application entry point.
public partial class Program;
