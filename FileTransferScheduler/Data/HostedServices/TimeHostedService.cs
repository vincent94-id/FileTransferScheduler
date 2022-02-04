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
        //private readonly ILogger<TimeHostedService> logger;
        private readonly Serilogger logger;
        private readonly IUploadService uploadService;
        private readonly IDownloadService downloadService;
        private readonly IOptions<SchedulerConfig> options;

        public TimeHostedService(ILoggerFactory loggerFactory, IUploadService uploadService, IDownloadService downloadService, IOptions<SchedulerConfig> options, Serilogger logger)
        {
            //this.loggerFactory = loggerFactory;
            var path = AppDomain.CurrentDomain.BaseDirectory;
            //loggerFactory.AddFile($"{path}\\Logs\\AppLog.txt");
            //this.logger = loggerFactory.CreateLogger<TimeHostedService>();
            this.logger = logger;
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
            logger.LogInformation("Program started");
            while (!stoppingToken.IsCancellationRequested)
            {
                //Process p = new Process(_logger);
                //await p.resetCycle();
                //_timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
                //logger.LogInformation("CurrentTime: {0}, time to check:{1}",DateTime.Now.ToString(),options.Value.xfileTime);
                var currentTime = DateTime.Now.ToString("HH:mm");
                
                // gen xfile and upload
                if (checkTimeFrame(currentTime,options.Value.xfileTime))
                {
                    logger.LogInformation("Download time triggered(download schedule:{0})", options.Value.xfileTime);
                    var genFileSuccess = false;
                    try
                    {
                        genFileSuccess = await uploadService.genFile(options.Value.workstationId);

                    }catch(Exception e)
                    {
                        logger.LogError(e.Message);
                    }
                    if (genFileSuccess)
                    {
                        var uploadSuccess = false;
                        logger.LogInformation("Generate Xfile Success");
                        for (var i = 0; i < options.Value.retry; i++)
                        {
                            //uploadSuccess = uploadService.uploadFile(options.Value.maxExchangeTime);
                            uploadSuccess = uploadService.sftpUpload();
                            if (uploadSuccess)
                            {
                                uploadSuccess = true;
                                break;
                            }else
                            {
                                logger.LogInformation("Upload file failed, retry upload ({0})",(i+1).ToString());
                            }
                        }
                        if(uploadSuccess)
                        {
                            logger.LogInformation("Upload XFile Completed.");
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
                //if(true)
                {
                    logger.LogInformation("Upload time triggered(upload schedule:{0})", options.Value.initTime);
                    var downloadSuccess = false;
                    for (var i = 0; i < options.Value.retry; i++)
                    {

                        downloadSuccess = downloadService.sftpDownload();
                        if (downloadSuccess)
                        {
                            downloadSuccess = true;
                            break;
                        }
                        else
                        {
                            logger.LogInformation("Download meta files failed, retry download ({0})", (i + 1).ToString());
                        }
                    }
                    if (downloadSuccess)
                    {
                        logger.LogInformation("Download meta files successfully");
                        var sendInitSuccess = false;
                        try
                        {
                            sendInitSuccess = await downloadService.sendInit(options.Value.workstationId);
                        }catch(Exception e)
                        {
                            logger.LogError(e.Message);
                        }
                        if(sendInitSuccess)
                        {
                            logger.LogInformation("Octopus device update meta files successfully.");
                        }else
                        {
                            logger.LogInformation("Octopus device update meta files failed.");
                        }
                    }
                    else
                    {
                        logger.LogInformation("Download meta files failed, sending system alert");
                        await downloadService.sendAlert(options.Value.workstationId, AlertType.SendFile);
                    }
                }
                await Task.Delay(60000, stoppingToken);
                
            }
            logger.LogInformation("Program shutdown");
        }

        private bool checkTimeFrame(string currentTime, string uploadTime)
        {
            
            //logger.LogInformation("compare time {0} with {1}",currentTime,uploadTime);
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
