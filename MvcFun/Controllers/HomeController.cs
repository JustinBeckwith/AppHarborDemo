using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Net;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MvcFun.Models;
using MvcFun.ServiceReference1;

using Twilio;

using Enyim.Caching;
using Enyim.Caching.Memcached;

using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

using BookSleeve;

using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

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

			/*
			 *  load and store the data using memcahced
			 */
			Stopwatch sw = new Stopwatch();
			sw.Start();

			if (!Globals.Cache.TryGet("Videos", out cacheVideos))
			{
				// videos aren't in the cache - load them from the db
				ViewBag.CacheHit = false;
				using (var db = new db3364Entities())
				{
					cacheVideos = db.Videos.ToList();
				}
				Globals.Cache.Store(StoreMode.Set, "Videos", cacheVideos);
			}
			else
			{
				// video
				ViewBag.CacheHit = true;
			}

			sw.Stop();

			ViewBag.TimeToLoad = sw.ElapsedTicks;
		

			return View(cacheVideos);
		}

		[HttpPost]
		public ActionResult Cache(FormCollection collection)
		{
			Globals.Cache.FlushAll();
			return View();
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
			Stopwatch sw = new Stopwatch();
			sw.Start();

			IList<Video> videos;
			using (var db = new db3364Entities())
			{
				videos = db.Videos.ToList();
			}

			sw.Stop();

			ViewBag.TimeToLoad = sw.ElapsedTicks;

			return View(videos);
		}
		#endregion

		#region PubSub
		/// <summary>
		/// this sample shows how to send a message to a redis pub/sub channel
		/// </summary>
		/// <returns></returns>
		public ActionResult PubSub()
		{
			
			return View();
		}

		[HttpPost]
		public ActionResult PubSub(FormCollection collection)
		{
			// the channel name needs to be the same on both ends
			const string ChannelName = "CHANNEL";
			const string ClientId = "AppHarbor MVC";
			
			// create a new redis client
			using (var redisPublisher = Globals.CreateRedisClient())
			{
				// publish the message to the "CHANNEL" channel
				var message = string.Format("{0}: {1}", ClientId, collection["message"]);					
				redisPublisher.PublishMessage(ChannelName, message);				
			}
			ViewBag.Message = "Message sent to queue";

			return View();
		}

		#endregion

		#region NoSQL
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public ActionResult NoSQL()
		{			
			#region Mongo

			Stopwatch sw = new Stopwatch();
			sw.Start();

			// connect to MONGO HQ out in the cloud
			var connectionString = ConfigurationManager.AppSettings["MONGOHQ_URL"];
			var database = MongoDatabase.Create(connectionString);
			var collection = database.GetCollection<Person>("People");

			// debug point to clear data
			if (false) 
				collection.RemoveAll();

			// if this is the first run populate the table
			if (collection.Count() == 0)			
				collection.InsertBatch(this.GetPeople());
			
			// query all people from the table
			var people = collection.FindAll().ToList();

			sw.Stop();
			ViewBag.MongoTime = sw.ElapsedTicks;
			ViewBag.MongoData = people;

			#endregion


			#region Redis

			sw = new Stopwatch();
			sw.Start();

			using (var redis = Globals.RedisClient.GetTypedClient<Person>())
			{	
				// get a reference to the current-people structure
				var currentPeople = redis.Lists["urn:people:current"];

				// debug point to clear data
				if (false) 
					currentPeople.RemoveAll();

				// if this is a the first run populate the table
				if (currentPeople.Count == 0)
					currentPeople.AddRange(this.GetPeople());

				// query all people from the table
				var cp = currentPeople.ToList();
				sw.Stop();
				ViewBag.RedisTime = sw.ElapsedTicks;
				ViewBag.RedisData = cp;
			}
			#endregion

			return View();
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

		#region GetPeople
		/// <summary>
		/// gets a list of people to seed a database
		/// </summary>
		/// <returns></returns>
		protected IList<Person> GetPeople()
		{
			var people = new List<Person>();

			people.Add(
						new Person()
						{
							Id = Guid.NewGuid(),
							Name = "Justin Beckwith",
							Email = "justbe@microsoft.com",
							CreatedAt = DateTime.UtcNow
						}
					);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Vignan Pattamatta",
					Email = "vignanp@microsoft.com",
					CreatedAt = DateTime.UtcNow
				}
			);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Adam Abdelhamed",
					Email = "adam.abdelhamed@microsoft.com",
					CreatedAt = DateTime.UtcNow
				}
			);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Vikram Desai",
					Email = "vikdesai@microsoft.com",
					CreatedAt = DateTime.UtcNow
				}
			);

			return people;
		}

		#endregion

	}
}
