using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.ViewModel;
using System.Net.Mail;
using System;
using Umbraco.Web;
using System.Linq;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using System.Web;

namespace HighlyDeveloped.Core.Services
{
    /// <summary>
    /// The home to all outbound emails from my site
    /// </summary>
    public class EmailService : IEmailService
    {
        private UmbracoHelper _umbraco;

        private IContentService _contentService;

        private ILogger _logger;

        /// <summary>
        /// Sending of the email to an admin when a new contact form comes in
        /// </summary>
        /// <param name="umbraco"></param>
        public EmailService(UmbracoHelper umbraco, IContentService contentService, ILogger logger)
        {
            _umbraco = umbraco;
            _contentService = contentService;
            _logger = logger;
        }

        public void SendContactNotificationToAdmin(ContactFormViewModel vm)
        {
            //Get email template from Umbraco for "this notification is
            var emailTemplate = GetEmailTemplate("New Contact Form Notification");

            if(emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //Get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //Mail merge necessary fields

            //##name##
            MailMerge("name", vm.Name, ref htmlContent, ref textContent);

            //##email##
            MailMerge("email", vm.EmailAddress, ref htmlContent, ref textContent);

            //##comment##
            MailMerge("comment", vm.Comment, ref htmlContent, ref textContent);
 

            //Send email out to whoever

            //Get the site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            //Read email FROM and TO addresses
            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("There needs to be a from address in site settings");
            }

            SendEmail(toAddresses, subject, htmlContent, textContent);

        }

        /// <summary>
        /// A generic send mail that logs the email in umbraco and sends via smtp
        /// </summary>
        /// <param name="toAddresses"></param>
        /// <param name="subject"></param>
        /// <param name="htmlContent"></param>
        /// <param name="textContent"></param>
        /// <exception cref="Exception"></exception>
        private void SendEmail(string toAddresses, string subject, string htmlContent, string textContent)
        {
            //Get the site settings
            var siteSettings = _umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");
            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("There needs to be a from address in site settings");
            }

            //Debug Mode
            var debugMode = siteSettings.Value<bool>("testMode");
            // var testEmailAccounts = siteSettings.Value<string>("emailTestAccounts");

            var recipients = toAddresses;

            if (debugMode)
            {
                //Change the TO - testEmailAccounts
                //recipients = testEmailAccounts;

                //Alter subject line - to show in test mode
                subject += "(TEST MODE)" + toAddresses;
            }

            //Log the email in umbraco
            var emails = _umbraco.ContentAtRoot()
                .DescendantsOrSelfOfType("emails").FirstOrDefault();

            var newEmail = _contentService.Create(toAddresses, emails.Id, "Email");

            newEmail.SetValue("emailSubject", subject);
            newEmail.SetValue("emailToAddress", recipients);
            newEmail.SetValue("emailHtmlContent", htmlContent);
            newEmail.SetValue("emailTextContent", textContent);
            // newEmail.SetValue("emailSend", false);
            _contentService.SaveAndPublish(newEmail);

            //Send the email via smtp or whatever
            var mimeType = new System.Net.Mime.ContentType("text/html");
            var alternateView = AlternateView.CreateAlternateViewFromString(htmlContent, mimeType);

            var smtpMessage = new MailMessage();
            smtpMessage.AlternateViews.Add(alternateView);

            //To - collecction or one email
            foreach (var recipient in recipients.Split(','))
            {
                if (!string.IsNullOrEmpty(recipient))
                {
                    smtpMessage.To.Add(recipient);
                }
            }

            //From
            smtpMessage.From = new MailAddress(fromAddress);

            //Subject
            smtpMessage.Subject = subject;

            //Body
            smtpMessage.Body = textContent;

            //Sending
            using (var smtp = new SmtpClient())
            {
                try
                {
                    smtp.Send(smtpMessage);
                    newEmail.SetValue("emailSent", true);
                    newEmail.SetValue("emailSentDate", DateTime.Now);
                    _contentService.SaveAndPublish(newEmail);
                }
                catch (Exception ex)
                {
                    //Log the error
                    _logger.Error<EmailService>("Problem sending the email", ex);
                    throw ex;
                }
            }
        }


        /// <summary>
        /// Send the email verification link to the new member
        /// </summary>
        /// <param name="membersEmail"></param>
        /// <param name="verificationToken"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void SendVerifyEmailAddressNotification(string membersEmail, string verificationToken)
        {
            //Get Template - create a new template in umbraco
            var emailTemplate = GetEmailTemplate("Verify Email");

            if (emailTemplate == null)
            {
                throw new Exception("Template not found");
            }

            //Get the template data
            var subject = emailTemplate.Value<string>("emailTemplateSubjectLine");
            var htmlContent = emailTemplate.Value<string>("emailTemplateHtmlContent");
            var textContent = emailTemplate.Value<string>("emailTemplateTextContent");

            //Mail Merge
            var url = HttpContext.Current.Request.Url.AbsoluteUri
                    .Replace(HttpContext.Current.Request.Url.AbsolutePath, string.Empty);
            url += $"verify?token={verificationToken}";

            MailMerge("verify-url", url, ref htmlContent, ref textContent);

            //Log the email
            //Send the email
            SendEmail(membersEmail, subject, htmlContent, textContent);
        }

        private void MailMerge(string token, string value, ref string htmlContent, ref string textContent)
        {
            htmlContent = htmlContent.Replace($"##{token}##", value);
            textContent = textContent.Replace($"##v{token}##", value); ;
        }













        /// <summary>
        /// Returns the email template as IPublishedContent
        /// where the title matches the template name
        /// 
        /// </summary>
        /// <param name="templateName"></param>
        /// <returns></returns>
        private IPublishedContent GetEmailTemplate(string templateName)
        {
            var template = _umbraco.ContentAtRoot()
                .DescendantsOrSelfOfType("emailTemplate")
                .Where(x => x.Name == templateName)
                .FirstOrDefault();

            return template;
        }
    }
}
