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
using Enyim.Caching;
using Enyim.Caching.Memcached;


namespace MvcFun.Controllers
{
	/// <summary>
	/// Home Controller for appharbor demo app
	/// </summary>
	public class HomeController : Controller
	{
		//--------------------------------------------------------------------------
		//
		//	Controller Actions
		//
		//--------------------------------------------------------------------------

		#region Index
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ActionResult Index()
		{
			ViewBag.Message = "Welcome to my AppHarbor demo application!";						

			return View();
		}
		#endregion

		#region WCF
		/// <summary>
		/// invoke the WCF service running at wcffun.apphb.com/service1.svc
		/// </summary>
		/// <returns></returns>
		public ActionResult WCF()
		{
			//throw new Exception("Waving my arms about wildly!");
			ViewBag.where = ConfigurationManager.AppSettings["where"];
			
			IService1 svc = new Service1Client();						
			string result = svc.GetData(23);

			ViewBag.data = result;			

			return View();
		}
		#endregion

		#region Cache
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ActionResult Cache()
		{
			object cacheVideos;

			var startTime = DateTime.Now;

			// attempt to load the videos out of the cache
			if (!Globals.Cache.TryGet("Videos", out cacheVideos))
			{
				// videos aren't in the cache - load them from the db
				ViewBag.CacheHit = false;
				var db = new db3364Entities();
				cacheVideos = db.Videos.ToList();
				Globals.Cache.Store(StoreMode.Set, "Videos", cacheVideos);
			}
			else
			{
				// video
				ViewBag.CacheHit = true;
			}
			
			ViewBag.TimeToLoad = DateTime.Now.Subtract(startTime).Milliseconds;			
			return View(cacheVideos);
		}

		[HttpPost]
		public ActionResult Cache(FormCollection collection)
		{
			Globals.Cache.FlushAll();
			return Cache();
		}
		#endregion

		#region DeployHook
		/// <summary>
		/// when a build on appharbor completes, this lets us pipe a notification via twilio to my cell phone
		/// </summary>
		/// <param name="notify"></param>
		/// <returns></returns>
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
		#endregion

		#region SQLServer
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ActionResult SQLServer()
		{
			var db = new db3364Entities();
			return View(db.Videos.ToList());
		}
		#endregion

		//--------------------------------------------------------------------------
		//
		//	Internal Methods
		//
		//--------------------------------------------------------------------------

		#region SendMessage
		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
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
			string parameters = "From=+4155992671&To=+7247771008&Body=" + HttpUtility.UrlEncode(message);
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
		#endregion
	}
}
