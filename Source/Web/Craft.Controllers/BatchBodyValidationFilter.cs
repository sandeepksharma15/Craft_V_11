using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Craft.Controllers;

internal sealed class BatchBodyValidationFilter : IActionFilter
{
    public BatchBodyValidationFilter() { }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        foreach (var value in context.ActionArguments.Values)
        {
            if (value is not IEnumerable sequence || value is string) continue;
            foreach (var item in sequence)
                if (item is null)
                {
                    context.Result = new BadRequestObjectResult(new ValidationProblemDetails(
                        new Dictionary<string, string[]> { ["body"] = ["Batch items cannot be null."] }) { Status = 400 });
                    return;
                }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
