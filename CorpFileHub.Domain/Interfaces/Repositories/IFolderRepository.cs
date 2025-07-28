using CorpFileHub.Domain.Entities;

namespace CorpFileHub.Domain.Interfaces.Repositories
{
    public interface IFolderRepository
    {
        Task<Folder?> GetByIdAsync(int id);
        Task<Folder?> GetByPathAsync(string path);
        Task<IEnumerable<Folder>> GetByParentIdAsync(int? parentId);
        Task<IEnumerable<Folder>> GetByOwnerIdAsync(int ownerId);
        Task<Folder> CreateAsync(Folder folder);
        Task<Folder> UpdateAsync(Folder folder);
        Task<bool> DeleteAsync(int id);
        Task<bool> FolderExistsAsync(string name, int? parentId);
    }
}