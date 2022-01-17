using FileTransferScheduler.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data.HostedService
{
    public class TimeHostedService : IHostedService, IDisposable
    {
        static Timer _timer;
        private readonly ILogger logger;
        private readonly UploadService uploadService;

        public TimeHostedService(ILoggerFactory loggerFactory, UploadService uploadService)
        {
            
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
    }
}
