using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using MvcFun.Models;

namespace MvcFun.Controllers
{
    public class VideoController : Controller
    {
        //
        // GET: /Video/

        public ActionResult Index()
        {
			var db = new db3364Entities();
			return View(db.Videos.ToList());
        }

    }
}
