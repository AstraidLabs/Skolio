var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapRazorPages();
app.MapFallbackToPage("/AppHost");

app.Run();
