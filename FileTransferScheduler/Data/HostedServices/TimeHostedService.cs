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
        private readonly IUploadService uploadService;
        private readonly IDownloadService downloadService;
        private readonly IOptions<SchedulerConfig> options;

        public TimeHostedService(ILoggerFactory loggerFactory, IUploadService uploadService, IDownloadService downloadService, IOptions<SchedulerConfig> options)
        {
            this.loggerFactory = loggerFactory;
            var path = AppDomain.CurrentDomain.BaseDirectory;
            loggerFactory.AddFile($"{path}\\Logs\\AppLog.txt");
            this.logger = loggerFactory.CreateLogger<TimeHostedService>();
            this.uploadService = uploadService;
            this.downloadService = downloadService;
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
                
                // gen xfile and upload
                if (checkTimeFrame(currentTime,options.Value.xfileTime))
                {
                    var genFileSuccess = await uploadService.genFile(options.Value.workstationId);

                    if (genFileSuccess)
                    {
                        var uploadSuccess = false;
                        logger.LogInformation("Generate Xfile Success");
                        for (var i = 0; i < options.Value.retry; i++)
                        {
                            uploadSuccess = uploadService.uploadFile(options.Value.maxExchangeTime);
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
                            await uploadService.sendAlert(options.Value.workstationId, AlertType.SendFile);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Generate Xfile Fail");
                        await uploadService.sendAlert(options.Value.workstationId, AlertType.GenerateFile);
                    }

                }
                // download meta data and init
                if (checkTimeFrame(currentTime, options.Value.initTime))
                {
                    var downloadSuccess = false;
                    for (var i = 0; i < options.Value.retry; i++)
                    {
                        downloadSuccess = downloadService.downloadFile(options.Value.maxExchangeTime);
                        if (downloadSuccess)
                        {
                            downloadSuccess = true;
                            break;
                        }
                        else
                        {
                            logger.LogInformation("Download meta file failed, retry download ({0})", i + 1);
                        }
                    }
                    if (downloadSuccess)
                    {
                        logger.LogInformation("Download meta file successfully");
                        var sendInitSuccess = await downloadService.sendInit(options.Value.workstationId);
                        if(sendInitSuccess)
                        {
                            logger.LogInformation("Octopus update meta data successfully.");
                        }
                    }
                    else
                    {
                        logger.LogInformation("Download file failed, sending system alert");
                        await downloadService.sendAlert(options.Value.workstationId, AlertType.SendFile);
                    }
                }
                await Task.Delay(60000, stoppingToken);
                
            }
        }

        private bool checkTimeFrame(string currentTime, string uploadTime)
        {
            logger.LogInformation("compare time {0} with {1}",currentTime,uploadTime);
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
