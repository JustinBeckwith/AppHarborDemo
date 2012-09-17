using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Configuration;
using ServiceStack.Redis;

namespace Worker
{
    class Program
    {
        static void Main(string[] args)
        {
            var redisUri = new Uri(ConfigurationManager.AppSettings.Get("REDISTOGO_URL"));
            var redisClient = new RedisClient(redisUri.Host, redisUri.Port);
            redisClient.Password = "553eee0ecf0a87501f5c67cb4302fc55";

            var reader = new StreamReader("data.txt");            

            while (true)
            {
                if (reader.EndOfStream)
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);

                var line = reader.ReadLine();                
                redisClient.PublishMessage("CHANNEL", line);
                Thread.Sleep(1000);
            }
        }
    }
}
