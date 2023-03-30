using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web;
using Umbraco.Web.Mvc;

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
                return CurrentUmbracoPage();
            }

            //Create a new Contact Form in Umbraco
            //Get a handle to "Contact Forms"
            var contactForms = Umbraco.ContentAtRoot().DescendantsOrSelfOfType("contactForms").FirstOrDefault();

            if(contactForms != null)
            {
                var newContact = Services.ContentService.Create("Contact", contactForms.Id, "contactForm");
                newContact.SetValue("contactName", vm.Name);
                newContact.SetValue("contactEmail", vm.EmailAddress);
                newContact.SetValue("contactSubject", vm.Subject);
                newContact.SetValue("contactComment", vm.Comment);
                Services.ContentService.SaveAndPublish(newContact);
            }


            //Send out an email to site admin

            //Return confirmation message to user
            TempData["status"] = "OK";

            return RedirectToCurrentUmbracoPage();
        }
    }
}
