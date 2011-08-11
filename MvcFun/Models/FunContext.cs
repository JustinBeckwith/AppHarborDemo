using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;


namespace MvcFun.Models
{
	public class FunContext :  DbContext
	{
		public DbSet<Person> People { get; set; }
	}
}