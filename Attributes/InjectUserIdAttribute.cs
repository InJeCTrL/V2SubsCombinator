using Microsoft.AspNetCore.Mvc.Filters;
using V2SubsCombinator.DTOs;

namespace V2SubsCombinator.Attributes
{
    public class InjectUserIdAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userIdClaim = context.HttpContext.User.FindFirst("userId");

            if (userIdClaim != null)
            {
                var userId = userIdClaim.Value;

                var requestParam = context.ActionArguments.FirstOrDefault(arg => arg.Key == "request");
                if (requestParam.Value is SubscriptionRequestBase subRequestDTO)
                {
                    subRequestDTO.UserId = userId;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}