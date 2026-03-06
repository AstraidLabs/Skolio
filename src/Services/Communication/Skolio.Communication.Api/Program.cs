using Skolio.Communication.Api.Configuration;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application;
using Skolio.Communication.Infrastructure;
using Skolio.Communication.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<CommunicationServiceOptions>().Bind(builder.Configuration.GetSection(CommunicationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddCommunicationApplication();builder.Services.AddCommunicationInfrastructure(builder.Configuration);
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddHealthChecks();builder.Services.AddSignalR();builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyCommunicationMigrationsAsync(); app.MapOpenApi(); }
app.UseCors("SkolioDevelopment");app.MapControllers();app.MapHealthChecks("/health");app.MapHub<CommunicationHub>("/hubs/communication");
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<CommunicationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-4-ready", publicBaseUrl = options.Value.PublicBaseUrl, signalRHub = "/hubs/communication" }));
app.Run();
