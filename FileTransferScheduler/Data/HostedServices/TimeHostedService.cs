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
using Microsoft.Extensions.Options;

namespace FileTransferScheduler.Data.HostedService
{
    public class TimeHostedService : BackgroundService
    {
        static Timer _timer;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger<TimeHostedService> logger;
        private readonly UploadService uploadService;
        private readonly IOptions<SchedulerConfig> options;

        public TimeHostedService(ILoggerFactory loggerFactory, UploadService uploadService, IOptions<SchedulerConfig> options)
        {
            this.loggerFactory = loggerFactory;
            var path = AppDomain.CurrentDomain.BaseDirectory;
            loggerFactory.AddFile($"{path}\\Logs\\AppLog.txt");
            this.logger = loggerFactory.CreateLogger<TimeHostedService>();
            this.uploadService = uploadService;
            this.options = options;
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
                var success = await uploadService.genFile("42db6d19-72c8-4de0-a81b-db60b7b72f39");

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
                logger.LogInformation("CurrentTime: {0}, time to check:{1}",DateTime.Now.ToString(),options.Value.xfileTime);
                var currentTime = DateTime.Now.ToString("HH:mm");
                if (checkTimeFrame(currentTime,options.Value.xfileTime))
                {
                    var genFileSuccess = await uploadService.genFile(options.Value.workstationId);

                    if (genFileSuccess)
                    {
                        var uploadSuccess = false;
                        logger.LogInformation("Generate Xfile Success");
                        for (var i = 0; i < options.Value.retry; i++)
                        {
                            uploadSuccess = uploadService.uploadFile(30);
                            if (uploadSuccess)
                            {
                                uploadSuccess = true;
                                break;
                            }else
                            {
                                logger.LogInformation("Upload file failed, retry upload ({0})",i+1);
                            }
                        }
                        if(uploadSuccess)
                        {
                            logger.LogInformation("Upload file successfully");
                        }else
                        {
                            logger.LogInformation("Upload file failed, sending system alert");
                            await uploadService.sendAlert();
                        }
                    }
                    else
                    {
                        logger.LogInformation("Generate Xfile Fail");
                    }

                }
                
                await Task.Delay(60000, stoppingToken);
                
            }
        }

        private bool checkTimeFrame(string currentTime, string uploadTime)
        {
            var times = uploadTime.Split(',');
            for (var i=0;i< times.Length;i++)
            {
                if (currentTime == times[i])
                    return true;
            }
            return false;
        }
    }
}
