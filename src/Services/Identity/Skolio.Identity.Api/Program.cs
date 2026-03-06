using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Application;
using Skolio.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<IdentityServiceOptions>()
    .Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddRouting();
builder.Services.AddHealthChecks();
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

app.UseCors("SkolioDevelopment");
app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<IdentityServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "bootstrap-ready",
        publicBaseUrl = options.Value.PublicBaseUrl,
        authWiring = "placeholder"
    });
});

app.MapGet("/.well-known/jwks.json", () => Results.Ok(new { keys = Array.Empty<object>() }));

app.Run();
