using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IFileRepository
    {
        Task<FileItem?> GetByIdAsync(int id);
        Task<FileItem?> GetByPathAsync(string path);
        Task<IEnumerable<FileItem>> GetByFolderIdAsync(int folderId);
        Task<IEnumerable<FileItem>> GetByOwnerIdAsync(int ownerId);
        Task<IEnumerable<FileItem>> SearchAsync(string searchTerm, int? folderId = null);
        Task<FileItem> CreateAsync(FileItem file);
        Task<FileItem> UpdateAsync(FileItem file);
        Task<bool> DeleteAsync(int id);
        Task<bool> FileExistsAsync(string name, int folderId);
    }
}