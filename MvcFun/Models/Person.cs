using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MvcFun.Models
{
	public class Person
	{		
		public Guid Id { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}