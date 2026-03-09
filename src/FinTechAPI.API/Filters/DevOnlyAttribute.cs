using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinTechAPI.API.Filters
{
    /// <summary>
    /// Restricts the controller or action to the Development environment only.
    /// Returns 403 in all other environments.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class DevOnlyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment())
            {
                context.Result = new ObjectResult(new { message = "This endpoint is only available in Development." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
