using FileTransferScheduler.Data;
using FileTransferScheduler.Data.HostedService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace FileTransferScheduler
{
    public class Program
    {
        public static void Main(string[] args)
        {
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                CreateHostBuilder(args).Build().Run();

        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            
            .ConfigureLogging(
                options =>
                {
                    options.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Information);
                    
                }


                )
               // .ConfigureWebHostDefaults(webBuilder =>
                //{
                //    webBuilder.UseStartup<Startup>();
                //})
                //.UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    // load in the json file as configuration
                    .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)

                    .Build();

                    services.Configure<SchedulerConfig>(configuration.GetSection("SchedulerConfig"));
                    services.AddHostedService<TimeHostedService>()
                    .Configure<EventLogSettings>(config =>
                    {
                        config.LogName = "File Transfer Log";
                        config.SourceName = "File Transfer Source";


                    });
                    services.AddScoped<IUploadService, UploadService>();
                    services.AddScoped<IDownloadService, DownloadService>();
                    services.AddScoped<IHttpRequest, HttpRequest>();
                    services.AddScoped<Serilogger>();

                })
                
                .UseWindowsService();
    }
}
