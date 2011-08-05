using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Enyim.Caching;

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
	}
}