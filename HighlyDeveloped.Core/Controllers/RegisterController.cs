﻿using HighlyDeveloped.Core.Interfaces;
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
        private IEmailService _emailService;

        #region Register Form
        public RegisterController(IEmailService emailService)
        {
            _emailService = emailService;
        }

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
            var token = Guid.NewGuid().ToString();
            newMember.SetValue("emailVerifyToken", token);
            Services.MemberService.Save(newMember);


            //Send email verification
            try
            {
                _emailService.SendVerifyEmailAddressNotification(newMember.Email, token);
            }
            catch (Exception ex)
            {
                throw new Exception("Something's wrong. Please try again later");
            }
         

            //Thank the user
            TempData["status"] = "OK";

            return RedirectToCurrentUmbracoPage();
        }

        #endregion

        #region Verification

        public ActionResult RenderEmailVerification(string token)
        {
            //Get token (querystring)

            //Look for a member matching this token
            var member = Services.MemberService.GetMembersByPropertyValue("emailVerifyToken", token).SingleOrDefault();
            if(member != null)
            {
                //If find one, set them to verify
                var alreadyVerified = member.GetValue<bool>("emailVerified");
                if(alreadyVerified)
                {
                    ModelState.AddModelError("Verified", "You've already verified your email address.Thanks");
                    return CurrentUmbracoPage();
                }
                member.SetValue("emailVerified", true);
                member.SetValue("emailVerifiedDate", DateTime.Now);
                Services.MemberService.Save(member);

                //Thank the user
                TempData["status"] = "OK";
                return PartialView(PARTIAL_VIEW_FOLDER + "EmailVerification.cshtml");
            }

            //Otherwise...some problem
            ModelState.AddModelError("Error", "Apologies there has been a problem");

            return CurrentUmbracoPage();
        }


        #endregion
    }
}
