using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Mvc;

namespace HighlyDeveloped.Core.Controllers
{
    public class TwitterController :SurfaceController
    {
        public ActionResult GetTweets(string twitterHandle)
        {
            string PARTIAL_VIEW_FOLDER = "~/Views/Partials";

            return PartialView(PARTIAL_VIEW_FOLDER + "/Latest Tweets.cshtml", twitterHandle);
        }
    }
}
