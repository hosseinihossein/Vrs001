using App.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace App.Filters;

//[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableControllerFilter : /*Attribute,*/ IActionFilter
{
    //readonly IConfiguration configuration;
    readonly List<string> enabledControllers;
    public DisableControllerFilter(IConfiguration configuration)
    {
        //this.configuration = configuration;
        var enabledServices = configuration.GetSection("ServiceActivity").Get<Dictionary<string, bool>>();
        enabledControllers = [];
        foreach (var kv in enabledServices ?? [])
        {
            if (kv.Value)
            {
                enabledControllers.Add($"{kv.Key}Controller");
            }
        }
    }
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!enabledControllers.Contains(context.Controller.GetType().Name))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden); // Disable with 403 Forbidden
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
