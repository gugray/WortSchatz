using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace SchatzApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
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
