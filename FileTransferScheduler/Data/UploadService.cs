using Microsoft.Extensions.Logging;
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
        public UploadService(ILoggerFactory loggerFactory)
        {
            this.log = loggerFactory.CreateLogger<UploadService>();
        }

        public async Task<bool> genFile(string workstationId)
        {
            var http = new HttpRequest();
            (var status,var result) =await http.getAsync("http://car-park-api-sit.xanvi.com/api/OctopusOps/xfile/" + workstationId);
            var json = JsonConvert.DeserializeObject<XFileReponse>(result);
            log.LogInformation("status:{0} result:{1}",status,result);
            if (status == "OK" && json.message == "XFile generated")
                return true;
            else
                return false;
        }

        public bool uploadFile(int sec)
        {
            var timeout = sec * 1000;
            Process cmd = new Process();
            cmd.StartInfo.FileName = "c:\\Users\\vincent.chan\\.ssh\\exchange.bat";
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
