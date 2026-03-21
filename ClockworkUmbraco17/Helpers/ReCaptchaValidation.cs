using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClockworkUmbraco.Helpers;

public class ReCaptchaValidation : ActionFilterAttribute
{
    private readonly string _key;

    public ReCaptchaValidation(string key)
    {
        _key = key;
    }

    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        var recaptcha = new ReCaptcha();
        var response = filterContext.ActionArguments[_key]?.ToString();
        if (string.IsNullOrEmpty(response) || !recaptcha.ValidateReCaptcha(response))
        {
            filterContext.Result = new BadRequestObjectResult("ReCaptcha validation failed");
            return;
        }
        base.OnActionExecuting(filterContext);
    }
}

