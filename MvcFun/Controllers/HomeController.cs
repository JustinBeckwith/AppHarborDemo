using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;

using MvcFun.ServiceReference1;

namespace MvcFun.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Message = "Welcome to ASP.NET MVC!";

			return View();
		}

		public ActionResult About()
		{
			//throw new Exception("Waving my arms about wildly!");
			ViewBag.where = ConfigurationManager.AppSettings["where"];
			


			IService1 svc = new Service1Client();
			//int noWayThisWorks = svc.


			string result = svc.GetData(23);
			ViewBag.data = result;
			
			
						
			return View();
		}
	}
}
