using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IFileRepository
    {
        Task<FileItem?> GetByIdAsync(int id);
        Task<FileItem?> GetByPathAsync(string path);
        Task<IEnumerable<FileItem>> GetByFolderIdAsync(int folderId);
        Task<IEnumerable<FileItem>> GetByOwnerIdAsync(int ownerId);
        Task<IEnumerable<FileItem>> SearchAsync(string searchTerm, int? folderId = null);
        Task<IEnumerable<FileItem>> SearchAdvancedAsync(
            string? searchTerm,
            int? folderId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? extension = null,
            int? ownerId = null,
            string? tags = null,
            long? minSize = null,
            long? maxSize = null);
        Task<FileItem> CreateAsync(FileItem file);
        Task<FileItem> UpdateAsync(FileItem file);
        Task<bool> DeleteAsync(int id);
        Task<FileItem?> GetByIdIncludingDeletedAsync(int id);
        Task<bool> FileExistsAsync(string name, int folderId);
        Task<IEnumerable<FileItem>> GetByStatusAsync(FileStatus status);

        /// <summary>
        /// Получить все файлы вместе с версиями
        /// </summary>
        Task<IEnumerable<FileItem>> GetAllWithVersionsAsync();

        /// <summary>
        /// Удалить версию файла по идентификатору
        /// </summary>
        Task<bool> DeleteFileVersionAsync(int versionId);
    }
}
