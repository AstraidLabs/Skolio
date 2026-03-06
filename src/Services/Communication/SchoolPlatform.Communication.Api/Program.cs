var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SchoolPlatform.Communication.Api" }));
app.MapControllers();

app.Run();
