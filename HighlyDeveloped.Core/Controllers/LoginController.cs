using HighlyDeveloped.Core.Interfaces;
using HighlyDeveloped.Core.ViewModel;
using System;
using System.Web.Mvc;
using Umbraco.Web.Mvc;
using Umbraco.Core.Logging;
using System.Linq;

namespace HighlyDeveloped.Core.Controllers
{
    /// <summary>
    /// Login process
    /// </summary>
    public class LoginController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER="~/Views/Partials/Login/";
        private IEmailService _emailService;

        public LoginController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        #region Login
        public ActionResult RenderLogin()
        {
            var vm = new LoginViewModel();
            vm.RedirectUrl = HttpContext.Request.Url.AbsolutePath;
            return PartialView(PARTIAL_VIEW_FOLDER + "Login.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleLogin(LoginViewModel vm)
        {
            //Check if model is ok
            if (!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //Check if the member exists with that username
            var member = Services.MemberService.GetByUsername(vm.UserName);
            if(member == null)
            {
                ModelState.AddModelError("Login", "Cannot find that username in the system");
                return CurrentUmbracoPage();
            }

            //Check if the member is locked out
            if (member.IsLockedOut)
            {
                ModelState.AddModelError("Login", "The account is locked, please use forgotten password to reset");
                return CurrentUmbracoPage();
            }

            //Check if they have validated their email address
            var emailVerified = member.GetValue<bool>("emailVerified");
            if (!emailVerified)
            {
                ModelState.AddModelError("Login", "Please verify your email to login");
                return CurrentUmbracoPage();
            }

            //Check if credentials are ok
            //Log them in
            if(!Members.Login(vm.UserName, vm.Password))
            {
                ModelState.AddModelError("Login", "The username/password is not correct");
                return CurrentUmbracoPage();
            }

            if(!string.IsNullOrEmpty(vm.RedirectUrl))
            {
                return Redirect(vm.RedirectUrl);
            }

            return RedirectToCurrentUmbracoPage();
        }

        #endregion

        #region Forgotten Password
        public ActionResult RenderForgottenPassword()
        {
            var vm = new ForgottenPasswordViewModel();

            return PartialView(PARTIAL_VIEW_FOLDER + "ForgottenPassword.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HandleForgottenPassword(ForgottenPasswordViewModel vm)
        {
            //Is the Model ok?
            if(!ModelState.IsValid)
            {
                return CurrentUmbracoPage();
            }

            //Do we even have a member with this address
            //if not error
            var member = Services.MemberService.GetByEmail(vm.EmailAddress);
            if(member == null)
            {
                ModelState.AddModelError("Error", "Sorry we can't find that email in the system");
                return CurrentUmbracoPage();
            }

            //Create the reset token
            var resetToken = Guid.NewGuid().ToString();


            //Set the reset expiry date (now +12 hours)
            var expiryDate = DateTime.Now.AddHours(12);

            //Save the member
            member.SetValue("resetExpiryDate", expiryDate);
            member.SetValue("resetLinkToken", resetToken);
            Services.MemberService.Save(member);

            //Fire the email - reset password
            _emailService.SendResetPasswordNotification(vm.EmailAddress, resetToken);

            Logger.Info<LoginController>($"Sent a password reset to {vm.EmailAddress}");

            //Thanks
            TempData["status"] = "OK";

            return RedirectToCurrentUmbracoPage();
        }
        #endregion

        #region Reset Password

        public ActionResult RenderResetPassword()
        {
            var vm = new ResetPasswordViewModel();
            return PartialView(PARTIAL_VIEW_FOLDER + "ResetPassword.cshtml", vm);
        }

        public ActionResult HandleResetPassword(ResetPasswordViewModel vm)
        {
            //Get the reset token
            if(!ModelState.IsValid) 
            { 
                return CurrentUmbracoPage(); 
            }

            //Ensure that we have a token
            var token = Request.QueryString["token"];
            if (string.IsNullOrEmpty(token))
            {
                Logger.Warn<LoginController>("Reset Password - no token found");
                ModelState.AddModelError("Error", "Invalid Token");
                return CurrentUmbracoPage();
            }
            //Get the member for the token
            var member = Services.MemberService.GetMembersByPropertyValue("resetLinkToken", token).SingleOrDefault();
            if (member == null)
            {
                ModelState.AddModelError("Error", "That link is no longer valid");
                return CurrentUmbracoPage();
            }

            // Check the time window hasn't expire
            var membersTokenExpiryDate = member.GetValue<DateTime>("resetExpiryDate");
            var currentTime = DateTime.Now;
            if(currentTime.CompareTo(membersTokenExpiryDate) >= 0)
            {
                ModelState.AddModelError("Error", "Sorry the link has expired, please use the Forgotten Password page to generate a new one");
                return CurrentUmbracoPage();
            }


            //If ok, update the password for the member
            Services.MemberService.SavePassword(member, vm.Password);
            member.SetValue("resetLinkToken", string.Empty);
            member.SetValue("resetExpiryDate", null);
            member.IsLockedOut = false;
            Services.MemberService.Save(member);

            //Send out the email notification that the password changed
            _emailService.SendPasswordChangedNotification(member.Email);


            //Thanks
            TempData["status"] = "OK";
            Logger.Info<LoginController>($"User {member.Username} has changed their password");
            
            return RedirectToCurrentUmbracoPage();
        }

        #endregion
    }
}
