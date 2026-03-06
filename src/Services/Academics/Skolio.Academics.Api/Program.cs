using Skolio.Academics.Api.Configuration;
using Skolio.Academics.Application;
using Skolio.Academics.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<AcademicsServiceOptions>()
    .Bind(builder.Configuration.GetSection(AcademicsServiceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAcademicsApplication();
builder.Services.AddAcademicsInfrastructure(builder.Configuration);

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
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AcademicsServiceOptions> options) =>
{
    return Results.Ok(new
    {
        service = options.Value.ServiceName,
        status = "bootstrap-ready",
        publicBaseUrl = options.Value.PublicBaseUrl
    });
});

app.Run();
