using FileTransferScheduler.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace FileTransferScheduler.Data.HostedService
{
    public class TimeHostedService : BackgroundService
    {
        static Timer _timer;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<TimeHostedService> logger;
        private readonly UploadService uploadService;

        public TimeHostedService(ILoggerFactory loggerFactory, UploadService uploadService)
        {
            this.loggerFactory = loggerFactory;
            var path = AppDomain.CurrentDomain.BaseDirectory;
            loggerFactory.AddFile($"{path}\\Logs\\AppLog.txt");
            this.logger = loggerFactory.CreateLogger<TimeHostedService>();
            this.uploadService = uploadService;
            //this.hub = hub;
        }


        public void Dispose()
        {
            if (_timer != null) _timer.Dispose();
            _timer = null;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }

        public async void DoWork(object state)
        {
            logger.LogInformation("Start at" + DateTime.Now);
            //var task = new UploadService();
            var currentMin = DateTime.Now.ToString("mm");
            if (currentMin == "01")
            {
                var success = await uploadService.uploadFile();

                if (success)
                    logger.LogInformation("success");
                else
                    logger.LogInformation("fail");
            }

            //this.log.LogDebug("ServerTimeUpdateTriggered");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //Process p = new Process(_logger);
                //await p.resetCycle();
                //_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
                logger.LogInformation(DateTime.Now.ToString());
                var currentTime = DateTime.Now.ToString("HH:mm");
                if (currentTime == "15:24")
                {
                    var success = await uploadService.uploadFile();

                    if (success)
                        logger.LogInformation("success");
                    else
                        logger.LogInformation("fail");
                }
                await Task.Delay(60000, stoppingToken);
                
            }
        }
    }
}
