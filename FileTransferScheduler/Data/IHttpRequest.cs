using System;
using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public interface IHttpRequest :IDisposable
    {
        void Dispose();
        Task<(string, string)> getAsync(string url, int timeout);
        string post(string url, string data);
        Task<(string, string)> postAsync(string url, string data);
    }
}