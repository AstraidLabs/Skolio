var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSignalR();

var app = builder.Build();

app.MapHealthChecks("/health");

app.Run();
