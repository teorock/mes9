using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace intranet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

       public static IHostBuilder CreateHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup<Startup>();
               });


            //public static IHostBuilder CreateHostBuilder(string[] args) =>
            //    Host.CreateDefaultBuilder(args)
            //        .ConfigureWebHostDefaults(webBuilder =>
            //        {
            //            webBuilder.UseKestrel((hostingContext, options) =>
            //            {
            //                System.Net.IPAddress ip = new IPAddress()
            //                options.Listen(new System.Net.IPAddress., 8443, listenOptions =>
            //                {
            //                    listenOptions.UseHttps("certificato/server.pfx", "Grupp0Gb22@!");
            //                });
            //            });
            //            webBuilder.UseStartup<Startup>();
            //        });
        //
            //}
    }
}
