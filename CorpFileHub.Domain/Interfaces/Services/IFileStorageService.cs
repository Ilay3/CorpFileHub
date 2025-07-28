namespace CorpFileHub.Domain.Interfaces.Services
{
    public interface IFileStorageService
    {
        Task<string> SaveFileVersionAsync(Stream fileStream, int fileId, int version, string fileName);
        Task<Stream> GetFileVersionAsync(int fileId, int version);
        Task<bool> DeleteFileVersionAsync(int fileId, int version);
        Task<string> GetVersionPathAsync(int fileId, int version);
        Task<bool> VersionExistsAsync(int fileId, int version);
    }
}