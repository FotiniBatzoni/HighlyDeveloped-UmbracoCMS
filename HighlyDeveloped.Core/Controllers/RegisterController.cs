using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    public class RegisterController :SurfaceController
    {
        private const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/";
        public ActionResult RenderRegister()
        {
            var vm = new RegisterViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "Register.cshtml", vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleRegister(RegisterViewModel vm)
        {
            //If form is not valid -return
            if(!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //Check if there is already a member with that email address
            var existingMember = Services.MemberService.GetByEmail(vm.EmailAddress);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with this email address");
                return CurrentUmbracoPage();
            }

            //Check if the username is already in use
            existingMember = Services.MemberService.GetByUsername(vm.Username);

            if (existingMember != null)
            {
                ModelState.AddModelError("Account Error", "There is already a user with this username");
                return CurrentUmbracoPage();
            }

            //Create a 'member' in umbraco with details
            var newMember = Services.MemberService
                .CreateMember(vm.Username, vm.EmailAddress, $"{vm.FirstName} {vm.LastName}", "Member");

            newMember.PasswordQuestion = "";
            newMember.RawPasswordAnswerValue = "";

            //Need to save the member before you can set the password
            Services.MemberService.Save(newMember);
            Services.MemberService.SavePassword(newMember, vm.Password);

            //Assign a role -  i.e Normal User
            Services.MemberService.AssignRole(newMember.Id, "Normal User");

            //Create email verification token


            //Sens email verification
            return CurrentUmbracoPage();
        }
    }
}
