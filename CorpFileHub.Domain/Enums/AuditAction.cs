namespace CorpFileHub.Domain.Enums
{
    public enum AuditAction
    {
        Login = 0,
        Logout = 1,
        FileUpload = 2,
        FileDownload = 3,
        FileEdit = 4,
        FileDelete = 5,
        FileRestore = 6,
        FolderCreate = 7,
        FolderDelete = 8,
        AccessRightsChange = 9,
        VersionRollback = 10
    }
}