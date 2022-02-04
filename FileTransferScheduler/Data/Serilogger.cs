using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public class Serilogger
    {
        private static string path = AppDomain.CurrentDomain.BaseDirectory;
        public static Logger logger = new LoggerConfiguration()
                    .WriteTo
                    .Console()
                    
                    .WriteTo
                    .RollingFile($"{path}\\Logs\\AppLog.txt",
                    outputTemplate: "<{Timestamp:yyyy-MM-dd HH:mm:ss}> <{Message:lj}>{NewLine}{Exception}")
                    .CreateLogger();
        public Serilogger()
        {
            
            
        }

        public void LogInformation(string msg, string val1="", string val2="")
        {
            logger.Information(msg, val1, val2);
        }

        public void LogError(string msg, string val1="", string val2="")
        {
            logger.Error(msg, val1, val2);
        }
    }
}
