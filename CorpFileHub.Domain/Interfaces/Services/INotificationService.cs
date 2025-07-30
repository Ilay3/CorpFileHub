namespace CorpFileHub.Domain.Interfaces.Services
{
    public interface INotificationService
    {
        Task SendFileUploadNotificationAsync(string userEmail, string fileName, string folderPath);
        Task SendFileEditNotificationAsync(string userEmail, string fileName, string editorName);
        Task SendAccessChangedNotificationAsync(string userEmail, string entityName, string newAccess);
        Task SendErrorNotificationAsync(string adminEmail, string errorMessage, Exception exception);
        Task SendBulkNotificationAsync(IEnumerable<string> emails, string subject, string message);
        Task SendPasswordResetAsync(string userEmail, string resetLink);
    }
}