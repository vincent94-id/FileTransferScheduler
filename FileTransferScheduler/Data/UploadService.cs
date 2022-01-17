using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

        public async Task<bool> uploadFile()
        {
            var http = new HttpRequest();
            (var status,var result) =await http.getAsync("http://www.google.com");
            log.LogInformation("status:{0} result:{1}",status,result);
            if (status == "OK")
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
