namespace CorpFileHub.Domain.Enums
{
    public enum AuditAction
    {
        // Аутентификация
        Login = 0,
        Logout = 1,

        // Действия с файлами
        FileUpload = 2,
        FileDownload = 3,
        FileEdit = 4,
        FileDelete = 5,
        FileRestore = 6,
        FileMove = 7,
        FileRename = 8,

        // Действия с папками
        FolderCreate = 10,
        FolderDelete = 11,
        FolderMove = 12,
        FolderRename = 13,

        // Версии файлов
        VersionRollback = 20,
        VersionCreate = 21,
        VersionDelete = 22,

        // Права доступа
        AccessRightsChange = 30,
        AccessRightsView = 31,

        // Пользователи и группы
        UserCreate = 40,
        UserUpdate = 41,
        UserDelete = 42,
        UserBlock = 43,
        UserUnblock = 44,

        GroupCreate = 50,
        GroupUpdate = 51,
        GroupDelete = 52,
        GroupAddUser = 53,
        GroupRemoveUser = 54,

        // Системные действия
        SystemBackup = 60,
        SystemRestore = 61,
        SystemError = 62,

        // Поиск и просмотр
        Search = 70,
        FileView = 71,
        FolderView = 72
    }
}