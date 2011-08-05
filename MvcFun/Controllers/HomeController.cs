using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.Collections.Specialized;

using MvcFun.Models;
using MvcFun.ServiceReference1;

using Twilio;


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
			string result = svc.GetData(23);

			ViewBag.data = result;							

			return View();
		}

		[HttpPost]
		public ActionResult DeployHook(MvcFun.Models.Notification notify)
		{

			//{
			//  "application": {
			//    "name": "Foo"
			//  }, 
			//  "build": {
			//    "commit": {
			//      "id": "77d991fe61187d205f329ddf9387d118a09fadcd", 
			//      "message": "Implement foo"
			//    }, 
			//    "status": "succeeded"
			//  }
			//}

			this.SendMessage(string.Format("A new build of '{0}' has been deployed!  The status was: '{1}'", notify.application.name, notify.build.status));

			return View(notify);
		}


		protected void SendMessage(string message)
		{
			string accountSid = "AC4535a3259069b70dbc641954a9b7ae0f";
			string authToken = "f5c3c5f80a251cdf571812483d09ba3c";
			string baseURI = "https://api.twilio.com/2010-04-01";
			WebRequest req = System.Net.WebRequest.Create(string.Format("{0}/Accounts/{1}/SMS/Messages", baseURI, accountSid));
			NetworkCredential nc = new NetworkCredential(accountSid, authToken);
			req.Credentials = nc;
			req.ContentType = "application/x-www-form-urlencoded";
			req.Method = "POST";
			string parameters = "From=+4155992671&To=+7247771008&Body=helloworld";
			byte[] bytes = System.Text.Encoding.ASCII.GetBytes(parameters);
			req.ContentLength = bytes.Length;
			System.IO.Stream os = req.GetRequestStream();
			os.Write(bytes, 0, bytes.Length); //Push it out there
			os.Close();
			System.Net.WebResponse resp = req.GetResponse();
			if (resp == null) return;
			System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
			string response = sr.ReadToEnd().Trim();
		}
	}
}
