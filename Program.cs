using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace DrakesBasketballCourtServer
{
    public class Program
    {
        public static void Main()
        {
            CreateHostBuilder().Build().Run();
        }

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(opts => opts.Listen(IPAddress.Parse("0.0.0.0"), 5000));
                });
    }
}