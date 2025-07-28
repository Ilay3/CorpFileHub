using CorpFileHub.Domain.Interfaces.Services;

namespace CorpFileHub.Infrastructure.Services
{
    public class FileStorageService : IFileStorageService
    {
        private readonly string _archivePath;

        public FileStorageService(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _archivePath = configuration["FileStorage:ArchivePath"] ?? "./Archive";

            // Создаем директорию если её нет
            if (!Directory.Exists(_archivePath))
            {
                Directory.CreateDirectory(_archivePath);
            }
        }

        public async Task<string> SaveFileVersionAsync(Stream fileStream, int fileId, int version, string fileName)
        {
            var fileDir = Path.Combine(_archivePath, fileId.ToString());
            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var versionFileName = $"v{version}_{timestamp}_{fileName}";
            var fullPath = Path.Combine(fileDir, versionFileName);

            using var fileStreamOut = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(fileStreamOut);

            return fullPath;
        }

        public async Task<Stream> GetFileVersionAsync(int fileId, int version)
        {
            var versionPath = await GetVersionPathAsync(fileId, version);
            if (string.IsNullOrEmpty(versionPath) || !File.Exists(versionPath))
            {
                throw new FileNotFoundException($"Version {version} of file {fileId} not found");
            }

            return new FileStream(versionPath, FileMode.Open, FileAccess.Read);
        }

        public async Task<bool> DeleteFileVersionAsync(int fileId, int version)
        {
            var versionPath = await GetVersionPathAsync(fileId, version);
            if (string.IsNullOrEmpty(versionPath) || !File.Exists(versionPath))
            {
                return false;
            }

            File.Delete(versionPath);
            return true;
        }

        public async Task<string> GetVersionPathAsync(int fileId, int version)
        {
            var fileDir = Path.Combine(_archivePath, fileId.ToString());
            if (!Directory.Exists(fileDir))
            {
                return string.Empty;
            }

            var files = Directory.GetFiles(fileDir, $"v{version}_*");
            return files.FirstOrDefault() ?? string.Empty;
        }

        public async Task<bool> VersionExistsAsync(int fileId, int version)
        {
            var versionPath = await GetVersionPathAsync(fileId, version);
            return !string.IsNullOrEmpty(versionPath) && File.Exists(versionPath);
        }
    }
}