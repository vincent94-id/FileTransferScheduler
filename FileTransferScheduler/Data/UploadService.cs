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
    public class UploadService: IDisposable
    {
        private readonly ILogger log;
        private readonly IOptions<SchedulerConfig> options;
        private string server;
        private string uploadScript;
        
        public UploadService(ILoggerFactory loggerFactory, IOptions<SchedulerConfig> options)
        {
            this.log = loggerFactory.CreateLogger<UploadService>();
            this.options = options;
            server = options.Value.server;
            uploadScript = options.Value.uploadScript;
        }

        public async Task<bool> genFile(string workstationId)
        {
            
            var http = new HttpRequest();
            var url = $"http://{server}/api/OctopusOps/xfile/" + workstationId;
            (var status,var result) =await http.getAsync(url);
            log.LogInformation("Requesting {0}",url);
            var json = JsonConvert.DeserializeObject<XFileReponse>(result);
            log.LogInformation("status:{0} result:{1}",status,result);
            if (status == "OK" && json.message == "XFile generated")
                return true;
            else
                return false;
        }

        public async Task<bool> sendAlert()
        {
            var http = new HttpRequest();
            var request = new
            {

                alertCode = 200002,
                messageCN = "Upload exchange file failed",
                messageEN = "Upload exchange file failed",
                status = 0,
                id = "0000-0000-0000-0000"
            };
            var url = $"http://{server}/api/SystemAlert";
            log.LogInformation("Requesting {0}", url);
            (var status, var result) = await http.postAsync(url,JsonConvert.SerializeObject(request));
            log.LogInformation("response:{0},{1}", status,result);
            if (status == "OK" )
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

            cmd.StandardInput.WriteLine("start upload");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit(timeout);
            
            var result = cmd.StandardOutput.ReadToEnd();
            result = result.TrimEnd('\r','\n');
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
