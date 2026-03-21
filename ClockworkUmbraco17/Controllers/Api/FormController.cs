using ClockworkUmbraco.Extensions;
using ClockworkUmbraco.Helpers;
using ClockworkUmbraco.Models.Dtos;
using ClockworkUmbraco.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common;

namespace ClockworkUmbraco.Controllers
{
    [ApiController]
    [Route("/umbraco/api/form")]
    public class FormController : Controller
    {
        private readonly IPublishedContentQuery _publishedContentQuery;
        private readonly UmbracoHelper _umbracoHelper;
        private readonly IContentTypeService _contentType;
        private readonly MailHandler _mailHandler;
        public FormController(
            IPublishedContentQuery publishedContentQuery,
            IOptions<GlobalSettings> globalSettings,
            UmbracoHelper umbracoHelper,
            IContentService contentService,
            IContentTypeService contentType,
            IMediaService mediaService,
            MailHandler mailHandler)
        {
            _publishedContentQuery = publishedContentQuery;
            var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json");
            _umbracoHelper = umbracoHelper;
            _contentType = contentType;
            _mailHandler = mailHandler;
        }

        [HttpPost("SaveContactForm")]
        [ReCaptchaValidation("grecaptcharesponse")]

        // NOT => PII Kullanıldı ama duruma göre db oluşturulup veriler oraya da kaydedilebilir.
        public bool SaveContactForm([FromForm] ContactFormModel model, [FromForm] string grecaptcharesponse)
        {
            if (!ModelState.IsValid)
            {
                Response.StatusCode = 400;
                return false;
            }

            try
            {
                FormIdModel contactIds = _umbracoHelper.GetContactFormId(_contentType);
                PIIHelper pIIHelper = new PIIHelper();

                var emailModel = new
                {
                    NameSurname = model.NameSurname,
                    Email = model.Email,
                    Subject = model.Subject,
                    Message = model.Message,
                    Agreement = model.Subject,
                };

                var modelStr = JsonConvert.SerializeObject(emailModel);

                var result = pIIHelper.SendData(modelStr, model.Subject, contactIds.formTemplateId);

                var receipentMail = contactIds.receipentMail; // ilgili içerik bölümünden alabilirsiniz ya da appsettings.json'dan ayarlayabilirsiniz

                _ = _mailHandler.SendWithSmtp(receipentMail, model.Subject, "~/Views/Shared/Form.cshtml", JObject.Parse(modelStr));

                return true;
            }
            catch (Exception ex)
            {
                _ = typeof(ContactFormModel).LogToFile(ex.Message);
                Response.StatusCode = 500;
                return false;
            }
        }

    }
}