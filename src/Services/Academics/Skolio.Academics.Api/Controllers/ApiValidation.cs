using Microsoft.AspNetCore.Mvc;

namespace Skolio.Academics.Api.Controllers;

internal static class ApiValidation
{
    public static ActionResult ValidationField(this ControllerBase controller, string field, string message)
        => controller.BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
        {
            Title = "Validation failed.",
            Status = StatusCodes.Status400BadRequest
        });

    public static ActionResult ValidationForm(this ControllerBase controller, string message)
        => controller.ValidationField("$form", message);
}
