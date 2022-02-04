using System.Threading.Tasks;

namespace FileTransferScheduler.Data
{
    public interface IUploadService
    {
        void Dispose();
        Task<bool> genFile(string workstationId);
        Task<bool> sendAlert(string workstationId, AlertType alertType);
        bool uploadFile(int sec);

        bool sftpUpload();
    }
}