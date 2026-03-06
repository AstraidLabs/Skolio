var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "SchoolPlatform.WebHost" }));
app.MapGet("/bootstrap-config", () => Results.Ok(new
{
    identityAuthority = builder.Configuration["Frontend:IdentityAuthority"],
    webHostBaseUrl = builder.Configuration["Frontend:WebHostBaseUrl"]
}));

app.UseStaticFiles();
app.MapRazorPages();

app.MapFallbackToPage("/AppHost");

app.Run();
