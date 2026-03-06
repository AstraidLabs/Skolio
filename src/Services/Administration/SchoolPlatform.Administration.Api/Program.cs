var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SchoolPlatform.Administration.Api" }));
app.MapControllers();

app.Run();
