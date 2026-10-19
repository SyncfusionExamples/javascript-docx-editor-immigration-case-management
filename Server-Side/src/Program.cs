using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Syncfusion.Licensing;

namespace EJ2APIServices
{
    public class Program
    {
        public static void Main(string[] args)
        {
         
            SyncfusionLicenseProvider.RegisterLicense("Enter your Syncfusion License Key");

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
