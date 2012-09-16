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


using Twilio;

using Enyim.Caching;
using Enyim.Caching.Memcached;

using ServiceStack.Redis;
using ServiceStack.Redis.Generic;

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
			ViewBag.Message = "Welcome to my AppHarbor demo application! Change!";						

			return View();
		}
		#endregion

		#region Database
		/// <summary>
		/// Shows example of querying the same data from three databases
		/// </summary>
		/// <returns></returns>
		public ActionResult Database()
		{
			var clearAll = false;

			#region Mongo

			Stopwatch sw = new Stopwatch();
			sw.Start();

			try
			{

				// connect to MONGO HQ out in the cloud				
				var database = Globals.CreateMongoClient();
				var collection = database.GetCollection<Person>("People");

				// debug point to clear data
				if (clearAll)
					collection.RemoveAll();

				// if this is the first run populate the table
				if (collection.Count() == 0)
					collection.InsertBatch(this.GetPeople());

				// query all people from the table
				var people = collection.FindAll().ToList();

				sw.Stop();
				ViewBag.MongoTime = sw.ElapsedMilliseconds;
				ViewBag.MongoData = people;
			}
			catch (Exception ex)
			{
				ViewBag.MongoError = "There was an error contacting MongoHQ: " + ex.ToString();
			}


			#endregion

			#region Redis

			sw = new Stopwatch();
			sw.Start();

			try
			{
				using (var redis = Globals.RedisClient.GetTypedClient<Person>())
				{
					// get a reference to the current-people structure
					var currentPeople = redis.Lists["urn:people:current"];

					// debug point to clear data
					if (clearAll)
						currentPeople.RemoveAll();

					// if this is a the first run populate the table
					if (currentPeople.Count == 0)
						currentPeople.AddRange(this.GetPeople());

					// query all people from the table
					var cp = currentPeople.ToList();
					sw.Stop();
					ViewBag.RedisTime = sw.ElapsedMilliseconds;
					ViewBag.RedisData = cp;
				}
			}
			catch (Exception ex)
			{
				ViewBag.RedisError = "There was an error contacting Redis To Go: " + ex.ToString();
			}

			#endregion

			#region SQL Server

			sw = new Stopwatch();
			sw.Start();

			try
			{
				using (var db = new FunContext())
				{
					// we don't have a remove-all option
					if (clearAll)
					{						
						foreach (MvcFun.Models.Person p in db.People)
							db.People.Remove(p);
						db.SaveChanges();
					}

					// if this is the first run populate the table
					if (db.People.Count() == 0)
					{						
						foreach (Person p in this.GetPeople())
							db.People.Add(p);
						db.SaveChanges();
					}

					// query all people from the database
					var dbp = db.People.ToList();
					sw.Stop();
					ViewBag.SQLTime = sw.ElapsedMilliseconds;
					ViewBag.SQLData = dbp;
				}
			}
			catch (Exception ex)
			{
				ViewBag.SQLError = "There was an error contacting SQL Server: " + ex.ToString();
			}

			#endregion

			return View();
		}
		#endregion		

		#region Cache
		/// <summary>
		/// load and store the data using memcahced
		/// </summary>
		/// <returns></returns>
		public ActionResult Cache()
		{
			object cacheData = null;
			
			try
			{
				Stopwatch sw = new Stopwatch();
				sw.Start();

				if (!Globals.Cache.TryGet("People", out cacheData))
				{
					// videos aren't in the cache - load them from the db
					ViewBag.CacheHit = false;

					MongoDatabase db = Globals.CreateMongoClient();
					cacheData = db.GetCollection<Person>("People").FindAll().ToList();
										
					if (!Globals.Cache.Store(StoreMode.Add, "People", cacheData))
						throw new Exception("Cache write failed!");
				}
				else
				{
					// video
					ViewBag.CacheHit = true;
				}

				sw.Stop();
				ViewBag.TimeToLoad = sw.ElapsedMilliseconds;
			}
			catch (Exception ex)
			{
				ViewBag.Message = "There was an error contacting the cache: " + ex.ToString();
				cacheData = null;
			}			

			return View(cacheData);
		}

		/// <summary>
		/// flush the memcached data and return the get view
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Cache(FormCollection collection)
		{
			Globals.Cache.FlushAll();
			return Cache();
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

            //IService1 svc = new Service1Client();
            //string result = svc.GetData(23);

            ViewBag.data = "";

			return View();
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

		/// <summary>
		/// publish the posted message to a redis channel
		/// </summary>
		/// <param name="collection"></param>
		/// <returns></returns>
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

		//--------------------------------------------------------------------------
		//
		//	Internal Methods
		//
		//--------------------------------------------------------------------------

		#region SendMessage
		/// <summary>
		/// send a text message using the twilio api
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
							CreatedAt = DateTime.UtcNow,
							UpdatedAt = DateTime.UtcNow
						}
					);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Vignan Pattamatta",
					Email = "vignanp@microsoft.com",
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}
			);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Adam Abdelhamed",
					Email = "adam.abdelhamed@microsoft.com",
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}
			);

			people.Add(
				new Person()
				{
					Id = Guid.NewGuid(),
					Name = "Vikram Desai",
					Email = "vikdesai@microsoft.com",
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				}
			);

			return people;
		}

		#endregion

	}
}
