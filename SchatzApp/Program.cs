using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace SchatzApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Sampler.Init("../TestLog", "data/sample-01.txt");
            Sampler.Instance.GetPermutatedSample();
            var host = new WebHostBuilder()
               .UseUrls("http://127.0.0.1:5001")
               .UseKestrel()
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseStartup<Startup>()
               .Build();
            host.Run();
        }
    }
}
