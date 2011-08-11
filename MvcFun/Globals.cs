using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

using Enyim.Caching;
using ServiceStack.Redis;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MvcFun
{
	public class Globals
	{
		protected static MemcachedClient _cache;
		public static MemcachedClient Cache
		{
			get
			{
				if (_cache == null)
					_cache = new MemcachedClient();
				return _cache;
			}
		}		


		protected static RedisClient _redisClient;
		public static RedisClient RedisClient
		{
			get
			{
				if (_redisClient == null)
					_redisClient = Globals.CreateRedisClient();
				return _redisClient;
			}
		}		

		public static RedisClient CreateRedisClient()
		{
			var redisUri = new Uri(ConfigurationManager.AppSettings.Get("REDISTOGO_URL"));
			var redisClient = new RedisClient(redisUri.Host, redisUri.Port);
			redisClient.Password = "553eee0ecf0a87501f5c67cb4302fc55";
			return redisClient;
		}
		
		public static MongoDatabase CreateMongoClient() 
		{
			var connectionString = ConfigurationManager.AppSettings["MONGOHQ_URL"];
			return MongoDatabase.Create(connectionString);							
		}

	}
}