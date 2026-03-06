using Skolio.Organization.Api.Configuration;
using Skolio.Organization.Application;
using Skolio.Organization.Infrastructure;
using Skolio.Organization.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<OrganizationServiceOptions>()
    .Bind(builder.Configuration.GetSection(OrganizationServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOrganizationApplication();
builder.Services.AddOrganizationInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SkolioDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:8080", "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.ApplyOrganizationMigrationsAsync();
    app.MapOpenApi();
}

app.UseCors("SkolioDevelopment");
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<OrganizationServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "phase-4-ready",
        publicBaseUrl = options.Value.PublicBaseUrl
    });
});

app.Run();
