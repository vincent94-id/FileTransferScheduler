using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinSCP;

namespace FileTransferScheduler.Data
{
    public class DownloadService : IDisposable, IDownloadService
    {
        private readonly Serilogger log;
        //private readonly ILogger log;
        private readonly IOptions<SchedulerConfig> options;
        private readonly IHttpRequest http;
        private string server;
        private string uploadScript;
        private SessionOptions sessionOptions;

        public DownloadService(ILoggerFactory loggerFactory, IOptions<SchedulerConfig> options, IHttpRequest http, Serilogger logger)
        {
            //this.log = loggerFactory.CreateLogger<UploadService>();
            this.log = logger;
            this.options = options;
            server = options.Value.server;
            uploadScript = options.Value.uploadScript;
            this.http = http;

            sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = options.Value.sftpHost,
                UserName = options.Value.sftpUsername,
                SshHostKeyFingerprint = options.Value.sftpHostFingerprint,
                SshPrivateKeyPath = options.Value.sftpPrivateKey,
                
            };
        }

        public async Task<bool> sendInit(string workstationId)
        {

            //var http = new HttpRequest();
            var url = $"http://{server}/api/OctopusOps/connect/" + workstationId;
            //log.LogInformation("Requesting {0}", url);
            (var status, var result) = await http.getAsync(url, options.Value.maxInitTime);
            //log.LogInformation("status:{0} result:{1}", status, result);
            var json = JsonConvert.DeserializeObject<OctopusReponse>(result);
            
            if (status == "OK" && json.message == "Connect Success")
                return true;
            else
                return false;
        }

        public async Task<bool> sendAlert(string workstationId, AlertType alertType)
        {
            Object request = null;
            switch (alertType)
            {
                case AlertType.GenerateFile:
                    request = new
                    {

                        alertCode = 200002,
                        messageCN = $"生成八逹通交易檔失敗,電腦編號 {workstationId}",
                        messageEN = $"Generate octopus file failed in workstation {workstationId}",
                        status = 0,
                        id = "0000-0000-0000-0000"
                    };
                    break;
                case AlertType.SendFile:
                    request = new
                    {

                        alertCode = 200003,
                        messageCN = $"上傳失敗,電腦編號 {workstationId}",
                        messageEN = $"Upload exchange file failed in workstation {workstationId}",
                        status = 0,
                        id = "0000-0000-0000-0000"
                    };
                    break;
            }

            var url = $"http://{server}/api/SystemAlert";
            log.LogInformation("Requesting {0}", url);
            (var status, var result) = await http.postAsync(url, JsonConvert.SerializeObject(request));
            log.LogInformation("response:{0},{1}", status, result);
            if (status == "OK")
                return true;
            else
                return false;
        }

        public bool downloadFile(int sec)
        {
            var timeout = sec * 1000;
            Process cmd = new Process();
            cmd.StartInfo.FileName = uploadScript;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine("start upload");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit(timeout);

            var result = cmd.StandardOutput.ReadToEnd();
            result = result.TrimEnd('\r', '\n');
            //log.LogInformation(result);
            if (result == "Success Upload")
                return true;
            else
                return false;
        }

        public bool sftpDownload()
        {
            bool success = true;
            log.LogInformation("SFTP transfer start");
            using (Session session = new Session())
            {
                // Throw on any error
                try
                {
                    session.Open(sessionOptions);

                    TransferOptions transferOptions = new TransferOptions();
                    transferOptions.TransferMode = TransferMode.Binary;
                    

                    TransferOperationResult transferResult;
                    transferResult = session.GetFiles(options.Value.sftpRemoteDownloadPath +"*" , options.Value.sftpLocalDownloadPath, false, transferOptions);
                    //transferResult = session.GetFilesToDirectory(options.Value.sftpLocalDownloadPath, options.Value.sftpRemoteDownloadPath);
                    //var info = session.ListDirectory(options.Value.sftpRemoteDownloadPath);
                    //transferResult =
                    //session.PutFiles(options.Value.sftpLocalUploadPath + "*", options.Value.sftpRemoteUploadPath, false, transferOptions);

                    
                    transferResult.Check();
                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        log.LogInformation("Download of {0} succeeded", transfer.FileName);

                        // move to backup folder
                        var filename = new DirectoryInfo(transfer.FileName).Name;
                        var folder = new DirectoryInfo(transfer.FileName).Parent.Parent;
                        var backupFolder = folder.ToString().Replace("\\","/") + "/old_download/" + DateTime.Now.ToString("yyyy-MM-dd") + "/";
                        //if(!Directory.Exists(backupFolder))
                            //Directory.CreateDirectory(backupFolder);
                        var backupfile = backupFolder + filename;
                        if(!session.FileExists(backupFolder))
                            session.CreateDirectory(backupFolder);
                        session.MoveFile(transfer.FileName, backupfile);
                    }
                    

                }
                catch (Exception e)
                {
                    success = false;
                    log.LogError(e.Message);
                }


            }
            return success;
        }
        public void Dispose()
        {
            this.Dispose();
        }


    }





}
