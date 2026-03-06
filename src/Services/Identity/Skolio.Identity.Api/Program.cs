using Skolio.Identity.Api.Configuration;
using Skolio.Identity.Application;
using Skolio.Identity.Infrastructure;
using Skolio.Identity.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<IdentityServiceOptions>().Bind(builder.Configuration.GetSection(IdentityServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddHealthChecks();builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyIdentityMigrationsAsync(); app.MapOpenApi(); }
app.UseCors("SkolioDevelopment");app.MapControllers();app.MapHealthChecks("/health");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<IdentityServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-4-ready", publicBaseUrl = options.Value.PublicBaseUrl, authWiring = "placeholder" }));
app.MapGet("/.well-known/jwks.json", () => Results.Ok(new { keys = Array.Empty<object>() }));
app.Run();
