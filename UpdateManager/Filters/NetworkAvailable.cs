using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UpdateManager.Filters;

public class NetworkAvailable : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!Utils.CheckForInternetConnection())
        {
            context.Result = new BadRequestObjectResult("No internet connection.");
            return;
        }

        await next();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {

    }
}