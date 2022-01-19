using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public class DownloadService : IDisposable, IDownloadService
    {
        private readonly ILogger log;
        private readonly IOptions<SchedulerConfig> options;
        private readonly IHttpRequest http;
        private string server;
        private string uploadScript;

        public DownloadService(ILoggerFactory loggerFactory, IOptions<SchedulerConfig> options, IHttpRequest http)
        {
            this.log = loggerFactory.CreateLogger<UploadService>();
            this.options = options;
            server = options.Value.server;
            uploadScript = options.Value.uploadScript;
            this.http = http;
        }

        public async Task<bool> sendInit(string workstationId)
        {

            //var http = new HttpRequest();
            var url = $"http://{server}/api/OctopusOps/connect/" + workstationId;
            log.LogInformation("Requesting {0}", url);
            (var status, var result) = await http.getAsync(url, options.Value.maxInitTime);

            var json = JsonConvert.DeserializeObject<OctopusReponse>(result);
            log.LogInformation("status:{0} result:{1}", status, result);
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

        public void Dispose()
        {
            this.Dispose();
        }


    }





}
