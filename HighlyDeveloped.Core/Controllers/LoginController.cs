using HighlyDeveloped.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    /// <summary>
    /// Login process
    /// </summary>
    public class LoginController : SurfaceController
    {
        public const string PARTIAL_VIEW_FOLDER="~/Views/Partials/Login/";
        public ActionResult RenderLogin()
        {
            var vm = new LoginViewModel();
            vm.RedirectUrl = HttpContext.Request.Url.AbsolutePath;
            return PartialView(PARTIAL_VIEW_FOLDER + "Login.cshtml", vm);
        }
    }
}
