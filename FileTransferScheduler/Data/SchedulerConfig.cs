using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public class SchedulerConfig
    {
        public string server { get; set; }
        public string xfileTime { get; set; }

        public string initTime { get; set; }
        public string workstationId { get; set; }

        public string uploadScript { get; set; }
        public int retry { get; set; }

        public int maxXFileGenTime { get; set; }

        public int maxInitTime { get; set; }
        public int maxExchangeTime { get; set; }
    }

    public enum AlertType
    {
        GenerateFile,
        SendFile
    }
}
