using HighlyDeveloped.Core.ViewModel;
using System.Web.Mvc;
using System.Web.Security;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    public class AccountController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER = "~/Views/Partials/MyAccount/";
        public ActionResult RenderMyAccount()
        {
            var vm = new AccountViewModel();

            //Are we logged in
            if (!Umbraco.MemberIsLoggedOn())
            {
                ModelState.AddModelError("Error", "You are not currently logged in");
                return CurrentUmbracoPage();
            }

            //Get member details
            var member = Services.MemberService.GetByUsername(Membership.GetUser().UserName);
            if(member == null)
            {
                ModelState.AddModelError("Error", "We cannot find you in the system");
                return CurrentUmbracoPage();
            }

            //Populate the view model
            vm.Name = member.Name;
            vm.Email = member.Email;
            vm.Username = member.Username;


            return PartialView(PARTIAL_VIEW_FOLDER + "MyAccount.cshtml", vm);
        }
    }
}
