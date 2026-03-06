using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Skolio.WebHost.Configuration;

public class AppHostModel : PageModel
{
    private readonly FrontendHostOptions _frontend;

    public AppHostModel(IOptions<FrontendHostOptions> frontend)
    {
        _frontend = frontend.Value;
    }

    public string BootstrapEndpoint { get; private set; } = "/bootstrap-config";

    public string ViteDevServer { get; private set; } = string.Empty;

    public string SpaAssetsPath { get; private set; } = string.Empty;

    public bool IsDevelopment { get; private set; }

    public void OnGet()
    {
        IsDevelopment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        ViteDevServer = _frontend.ViteDevServer.TrimEnd('/');
        SpaAssetsPath = _frontend.SpaAssetsPath.Trim('/');
    }
}
