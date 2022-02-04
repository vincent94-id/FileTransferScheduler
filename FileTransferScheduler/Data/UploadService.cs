using Microsoft.Extensions.DependencyInjection;
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
    public class UploadService : IDisposable, IUploadService
    {
        //private readonly ILogger log;
        private readonly Serilogger log;
        private readonly IOptions<SchedulerConfig> options;
        //private readonly IHttpRequest http;
        private string server;
        private string uploadScript;
        private readonly IServiceProvider serviceProvider;
        private SessionOptions sessionOptions;
        //private readonly Serilogger mylogger;
        public UploadService(ILoggerFactory loggerFactory, IOptions<SchedulerConfig> options,  Serilogger logger, IServiceProvider serviceProvider)
        {
            //this.log = loggerFactory.CreateLogger<UploadService>();
            this.log = logger;
            this.options = options;
            server = options.Value.server;
            uploadScript = options.Value.uploadScript;
           
            this.serviceProvider = serviceProvider;
            //this.mylogger = mylogger;
            //mylogger.LogInformation("program started");

            sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Sftp,
                HostName = options.Value.sftpHost,
                UserName = options.Value.sftpUsername,
                SshHostKeyFingerprint = options.Value.sftpHostFingerprint,
                SshPrivateKeyPath = options.Value.sftpPrivateKey,
            };
        }

        public async Task<bool> genFile(string workstationId)
        {

            //var http = new HttpRequest();
            var url = $"http://{server}/api/OctopusOps/xfile/" + workstationId;
            //log.LogInformation("Requesting {0}", url);

            var scope = serviceProvider.CreateScope();
            IHttpRequest request = scope.ServiceProvider.GetRequiredService<IHttpRequest>();

            (var status, var result) = await request.getAsync(url, options.Value.maxXFileGenTime);
            
            //log.LogInformation("status:{0} result:{1}", status, result);
            var json = JsonConvert.DeserializeObject<OctopusReponse>(result);
            
            if (status == "OK" && json.message == "XFile generated")
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
            var scope = serviceProvider.CreateScope();
            var httprequest = scope.ServiceProvider.GetRequiredService<IHttpRequest>();
            (var status, var result) = await httprequest.postAsync(url, JsonConvert.SerializeObject(request));
            log.LogInformation("response:{0},{1}", status, result);
            if (status == "OK")
                return true;
            else
                return false;
        }

        public bool uploadFile(int sec)
        {
            var timeout = sec * 1000;
            Process cmd = new Process();
            cmd.StartInfo.FileName = uploadScript;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            //cmd.StandardInput.WriteLine("start upload");
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

        public bool sftpUpload()
        {
            bool success = true;
            log.LogInformation("SFTP transfer start");
            using (Session session = new Session())
            {
                session.Open(sessionOptions);

                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;

                TransferOperationResult transferResult;
                //var info = session.ListDirectory(options.Value.sftpRemoteDownloadPath);
                transferResult =
                    session.PutFiles(options.Value.sftpLocalUploadPath+"*", options.Value.sftpRemoteUploadPath, false, transferOptions);
                
                // Throw on any error
                try
                {
                    transferResult.Check();
                    // Print results
                    foreach (TransferEventArgs transfer in transferResult.Transfers)
                    {
                        log.LogInformation("Upload of {0} succeeded", transfer.FileName);

                        backupFile(transfer.FileName);
                    }
                }
                catch(Exception e)
                {
                    success = false;
                    log.LogError(e.Message);
                }

                
            }
            return success;
        }

        private void backupFile(string filename)
        {
            //var desname = "";
            var name = Path.GetFileName(filename);
            var path = Path.GetDirectoryName(filename);
            var folder = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fullPath = path + "\\" + folder;
            if(!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
            var desname = fullPath + "\\" + name;
            try
            {
                File.Move(filename, desname);
                log.LogInformation("Back up file {0} successfully to {1}", filename, desname);
            }catch(Exception e)
            {
                log.LogError(e.Message);
            }
        }
        public void Dispose()
        {
            this.Dispose();
        }


    }

   



}
