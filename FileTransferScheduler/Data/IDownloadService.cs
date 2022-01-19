using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public interface IDownloadService
    {
        void Dispose();
        bool downloadFile(int sec);
        Task<bool> sendAlert(string workstationId, AlertType alertType);
        Task<bool> sendInit(string workstationId);
    }
}