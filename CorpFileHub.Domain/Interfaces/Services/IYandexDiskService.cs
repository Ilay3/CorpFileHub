namespace CorpFileHub.Domain.Interfaces.Services
{
    public interface IYandexDiskService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath);
        Task<Stream> DownloadFileAsync(string filePath);
        Task<bool> DeleteFileAsync(string filePath);
        Task<string> GetEditLinkAsync(string filePath);
        Task<string> GetDownloadLinkAsync(string filePath);
        Task<bool> FileExistsAsync(string filePath);
        Task<DateTime> GetLastModifiedAsync(string filePath);
    }
}