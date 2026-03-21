using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Umbraco.Cms.Core.Configuration.Models;

namespace ClockworkUmbraco.Helpers;
public class MailHandler
{
    private readonly ILogger<MailHandler> _logger;
    private readonly IOptions<GlobalSettings> _globalSettings;
    private readonly RenderPartialViewHandler _renderPartialViewHandler;

    public MailHandler(ILogger<MailHandler> logger, IOptions<GlobalSettings> globalSettings,
        RenderPartialViewHandler renderPartialViewHandler)
    {
        _logger = logger;
        _globalSettings = globalSettings;
        _renderPartialViewHandler = renderPartialViewHandler;
    }

    public Task SendWithSmtp(string to, string subject, string mailView, object model)
    {
        try
        {
            var smtpSection = _globalSettings.Value.Smtp;
            var smtpClient = new SmtpClient(smtpSection.Host, smtpSection.Port);
            smtpClient.Credentials = new NetworkCredential(smtpSection.Username, smtpSection.Password);
            var mailBody = _renderPartialViewHandler.RenderToStringAsync(mailView, model);
            var mailMessage = new MailMessage(smtpSection.From, to, subject, mailBody.Result);

            mailMessage.IsBodyHtml = true;
            smtpClient.Send(mailMessage);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Task.FromException(e);
        }
    }

    public async Task SendWithSmtpAsync(string to, string subject, string mailView, object model)
    {
        try
        {
            var smtpSection = _globalSettings.Value.Smtp;

            using (var smtpClient = new SmtpClient(smtpSection.Host, smtpSection.Port))
            {
                smtpClient.EnableSsl = true;

                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpSection.Username, smtpSection.Password);
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                string mailBody = await _renderPartialViewHandler.RenderToStringAsync(mailView, model);

                using (var mailMessage = new MailMessage(smtpSection.From, to, subject, mailBody))
                {
                    mailMessage.IsBodyHtml = true;

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }

    public Task SendWithSmtpAndAttachment(string to, string subject, string mailView, object model, byte[] attachmentContent, string attachmentFileName, string attachmentContentType)
    {
        try
        {
            var smtpSection = _globalSettings.Value.Smtp;
            var smtpClient = new SmtpClient(smtpSection.Host, smtpSection.Port);
            smtpClient.Credentials = new NetworkCredential(smtpSection.Username, smtpSection.Password);
            smtpClient.EnableSsl = true;
            var mailBody = _renderPartialViewHandler.RenderToStringAsync(mailView, model);
            var mailMessage = new MailMessage(smtpSection.From, to, subject, mailBody.Result);

            mailMessage.IsBodyHtml = true;

            if (attachmentContent != null && !string.IsNullOrEmpty(attachmentFileName))
            {
                var attachment = new Attachment(new MemoryStream(attachmentContent), attachmentFileName, attachmentContentType);
                mailMessage.Attachments.Add(attachment);
            }

            smtpClient.Send(mailMessage);
            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return Task.FromException(e);
        }
    }
}

