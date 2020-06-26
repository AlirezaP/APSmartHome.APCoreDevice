using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace APCoreDevice
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
.Enrich.FromLogContext()
.WriteTo.Console()
//.WriteTo.File(
// @"c:\\applogs\\mqlog.txt",
// fileSizeLimitBytes: 1_000_000,
// rollOnFileSizeLimit: true,
// shared: true,
// flushToDiskInterval: TimeSpan.FromSeconds(1))
.CreateLogger();

            try
            {
                Log.Information("Starting up");

                // Set up configuration sources.
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Path.Combine(AppContext.BaseDirectory))
                    .AddJsonFile("appsettings.json", optional: true);

                var Configuration = builder.Build();

                Settings.User = Configuration["Device:UserName"];
                Settings.Pass = Configuration["Device:Password"];

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
                    Host.CreateDefaultBuilder(args)
                   .UseSerilog()
               .UseWindowsService()
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddHostedService<Worker>();
               
                   //services.AddHostedService<BroadCasterWorker>();
               });
    }
}
