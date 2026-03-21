using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;
using System.Net.Mail;
using Umbraco.Cms.Core.Configuration.Models;

namespace ClockworkUmbraco.Helpers;

public static class FormsMailHandler
{
    // ReSharper disable once TooManyArguments
    public static Task SendForm<T>(this T formType, string json, Controller controllerContext, GlobalSettings configuration, string to, string subject = null) where T : Type
    {
        try
        {
            if (to == null)
            {
                Log.Logger.Warning("Email alıcıları boş olamaz");
                throw new NullReferenceException("Email alıcıları boş olamaz.");
            }

            if (configuration.Smtp != null)
            {
                var smtpClient = new SmtpClient(configuration.Smtp.Host, configuration.Smtp.Port);
                smtpClient.Credentials = new NetworkCredential(configuration.Smtp.Username, configuration.Smtp.Password);
                var mailSubject = formType.Name;
                if (!string.IsNullOrWhiteSpace(subject)) mailSubject = subject;
                var mailBody = RenderRazorViewToString(controllerContext, "~/Views/Shared/Form.cshtml", JObject.Parse(json));
                var mailMessage = new MailMessage(configuration.Smtp.From, to, mailSubject, mailBody);

                mailMessage.IsBodyHtml = true;
                smtpClient.Send(mailMessage);
            }
            else
            {
                throw new NullReferenceException("Couldn't find SMTP settings.");

            }
        }
        catch (Exception e)
        {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            Log.Logger.Error(e, e.Message);
        }

        return Task.CompletedTask;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static string RenderRazorViewToString(Controller controller, string viewName, object model = null)
    {
        controller.ViewData.Model = model;
        using var sw = new StringWriter();
        IViewEngine viewEngine =
            controller.HttpContext.RequestServices.GetService(typeof(ICompositeViewEngine)) as
                ICompositeViewEngine;
        if (viewEngine != null)
        {
            var viewResult = viewEngine.GetView("", viewName, false);

            if (viewResult.View != null)
            {
                var viewContext = new ViewContext(
                    controller.ControllerContext,
                    viewResult.View,
                    controller.ViewData,
                    controller.TempData,
                    sw,
                    new HtmlHelperOptions()
                );
                viewResult.View.RenderAsync(viewContext);
            }
            else
            {
                throw new NullReferenceException("Couldn't find view result ");
            }
        }
        else
        {
            throw new NullReferenceException("Couldn't find view engine ");
        }

        return sw.GetStringBuilder().ToString();
    }
}

