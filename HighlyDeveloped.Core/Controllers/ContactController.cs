using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using System.Net.Mail;

namespace HighlyDeveloped.Core.Controllers
{
    /// <summary>
    /// This is for all operations regarding the contact form
    /// </summary>
    public class ContactController : SurfaceController
    {
        public ActionResult RenderContactForm()
        {
            var vm = new ContactFormViewModel();
            return PartialView("~/Views/Partials/Contact Form.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleContactForm(ContactFormViewModel vm)
        {
            if(!ModelState.IsValid)
            {
                ModelState.AddModelError("Error", "Please check the form");

                //Set the recaptcha site key
                var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
                if(siteSettings != null)
                {
                    var siteKey = siteSettings.Value<string>("recaptchaSiteKey");
                    vm.RecaptchaSiteKey = siteKey;
                }
                return CurrentUmbracoPage();
            }

            try
            {
                //Create a new Contact Form in Umbraco
                //Get a handle to "Contact Forms"
                var contactForms = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("contactForms").FirstOrDefault();

                if (contactForms != null)
                {
                    var newContact = Services.ContentService.Create("Contact", contactForms.Id, "contactForm");
                    newContact.SetValue("contactName", vm.Name);
                    newContact.SetValue("contactEmail", vm.EmailAddress);
                    newContact.SetValue("contactSubject", vm.Subject);
                    newContact.SetValue("contactComment", vm.Comment);
                    Services.ContentService.SaveAndPublish(newContact);
                }


                //Send out an email to site admin
                SendContactFormReceivedEmail(vm);


                //Return confirmation message to user
                TempData["status"] = "OK";

                return RedirectToCurrentUmbracoPage();
            }
            catch (Exception ex)
            {
                Logger.Error<ContactController>("There was an error in the contact form submission", ex.Message);
                ModelState.AddModelError("Error", "Sorry there was a problem noting your details.Would ypou please try again later");
            }

            return CurrentUmbracoPage();
        }

        /// <summary>
        /// This woll send out an email to site admins saying a contact form has been submitted
        /// </summary>
        /// <param name="vm"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void SendContactFormReceivedEmail(ContactFormViewModel vm)
        {
            //Get the site settings
            var siteSettings = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("siteSettings").FirstOrDefault();
            if (siteSettings == null)
            {
                throw new Exception("There are no site settings");
            }

            //Read email FROM and TO addresses
            var fromAddress = siteSettings.Value<string>("emailSettingsFromAddress");
            var toAddresses = siteSettings.Value<string>("emailSettingsAdminAccounts");

            if (string.IsNullOrEmpty(fromAddress))
            {
                throw new Exception("There needs to be a from address in site settings");
            }

            if (string.IsNullOrEmpty(toAddresses))
            {
                throw new Exception("There needs to be a from address in site settings");
            }

            //Construct the actual email
            var emailSubject = "There has been a contact form submitted";
            var emailBody = $"A new contact form has been received from {vm.Name}. Their comments were: {vm.Comment}";
            var smptMessage = new MailMessage();
            smptMessage.Subject = emailSubject;
            smptMessage.Body = emailBody;
            smptMessage.From = new MailAddress(fromAddress);

            var toList = toAddresses.Split(',');
            foreach (var item in toList)
            {
                if (!String.IsNullOrEmpty(item))
                {
                    smptMessage.To.Add(item);
                }

            }

            //Send via whatever email service
            using(var smtp = new SmtpClient())
            {
                smtp.Send(smptMessage);
            }
        }
    }
}
