using Hangfire;
using Skolio.Administration.Api.Configuration;
using Skolio.Administration.Application;
using Skolio.Administration.Infrastructure;
using Skolio.Administration.Infrastructure.Extensions;
using Skolio.Administration.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<AdministrationServiceOptions>().Bind(builder.Configuration.GetSection(AdministrationServiceOptions.SectionName)).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddAdministrationApplication();builder.Services.AddAdministrationInfrastructure(builder.Configuration);
builder.Services.AddControllers();builder.Services.AddRouting();builder.Services.AddHealthChecks();builder.Services.AddOpenApi();builder.Services.AddCors(options => options.AddPolicy("SkolioDevelopment", policy => policy.WithOrigins("http://localhost:8080", "http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));builder.Services.AddHangfireServer();
var app = builder.Build();
if (app.Environment.IsDevelopment()) { await app.ApplyAdministrationMigrationsAsync(); app.MapOpenApi(); }
app.UseCors("SkolioDevelopment");app.MapControllers();app.MapHealthChecks("/health");app.MapHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<HousekeepingJob>("administration-housekeeping-boundary", job => job.ExecuteAsync(), Cron.HourInterval(6));
app.MapGet("/", (Microsoft.Extensions.Options.IOptions<AdministrationServiceOptions> options) => Results.Ok(new { service = options.Value.ServiceName, status = "phase-4-ready", publicBaseUrl = options.Value.PublicBaseUrl, hangfireDashboard = "/hangfire" }));
app.Run();
