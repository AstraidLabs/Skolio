using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

public class AppHostModel : PageModel
{
    public string BootstrapJson { get; private set; } = "{}";

    public void OnGet()
    {
        var bootstrap = new
        {
            identityAuthority = "http://localhost:8081",
            organizationApi = "http://localhost:8082",
            academicsApi = "http://localhost:8083",
            communicationApi = "http://localhost:8084",
            administrationApi = "http://localhost:8085"
        };

        BootstrapJson = JsonSerializer.Serialize(bootstrap);
    }
}
