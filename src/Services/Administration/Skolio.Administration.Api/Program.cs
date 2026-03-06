using Hangfire;
using Skolio.Administration.Api.Configuration;
using Skolio.Administration.Application;
using Skolio.Administration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AdministrationServiceOptions>()
    .Bind(builder.Configuration.GetSection(AdministrationServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAdministrationApplication();
builder.Services.AddAdministrationInfrastructure(builder.Configuration);

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
builder.Services.AddHangfireServer();

var app = builder.Build();

app.UseCors("SkolioDevelopment");
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHangfireDashboard("/hangfire");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AdministrationServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "bootstrap-ready",
        publicBaseUrl = options.Value.PublicBaseUrl,
        hangfireDashboard = "/hangfire"
    });
});

app.Run();
