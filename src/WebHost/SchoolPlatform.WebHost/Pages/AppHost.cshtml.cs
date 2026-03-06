using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace SchoolPlatform.WebHost.Pages;

public sealed class AppHostModel : PageModel
{
    private readonly IConfiguration _configuration;

    public AppHostModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnGet()
    {
        ViewData["BootstrapJson"] = JsonSerializer.Serialize(new
        {
            identityAuthority = _configuration["Frontend:IdentityAuthority"],
            clientId = _configuration["Frontend:ClientId"],
            frontendBaseUrl = _configuration["Frontend:FrontendBaseUrl"]
        });
    }
}
