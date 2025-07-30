using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO.Compression;
using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Enums;

namespace CorpFileHub.Infrastructure.Services
{
    public interface IBackupService
    {
        Task<BackupResult> CreateFullBackupAsync(string backupPath);
        Task<BackupResult> CreateDatabaseBackupAsync(string backupPath);
        Task<BackupResult> CreateFilesBackupAsync(string backupPath);
        Task<RestoreResult> RestoreFromBackupAsync(string backupPath, BackupType backupType);
        Task<bool> VerifyBackupIntegrityAsync(string backupPath);
        Task<List<BackupInfo>> GetAvailableBackupsAsync(string backupDirectory);
        Task<bool> CleanupOldBackupsAsync(string backupDirectory, int retentionDays = 30);
        Task<bool> ScheduleAutomaticBackupAsync(TimeSpan interval);
    }

    public class BackupService : IBackupService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;
        private readonly ILogger<BackupService> _logger;
        private readonly string _archivePath;
        private readonly string _connectionString;

        public BackupService(
            IConfiguration configuration,
            IAuditService auditService,
            ILogger<BackupService> logger)
        {
            _configuration = configuration;
            _auditService = auditService;
            _logger = logger;
            _archivePath = configuration["FileStorage:ArchivePath"] ?? "./Archive";
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task<BackupResult> CreateFullBackupAsync(string backupPath)
        {
            var result = new BackupResult { BackupType = BackupType.Full };

            try
            {
                _logger.LogInformation("Начало создания полного резервного копирования в {BackupPath}", backupPath);

                // Создаем временную папку для бэкапа
                var tempBackupDir = Path.Combine(Path.GetTempPath(), $"corpfilehub_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(tempBackupDir);

                try
                {
                    // 1. Резервное копирование базы данных
                    var dbBackupResult = await CreateDatabaseBackupAsync(Path.Combine(tempBackupDir, "database.sql"));
                    if (!dbBackupResult.Success)
                    {
                        result.ErrorMessage = "Ошибка резервного копирования базы данных: " + dbBackupResult.ErrorMessage;
                        return result;
                    }

                    // 2. Резервное копирование файлов
                    var archiveBackupPath = Path.Combine(tempBackupDir, "files");
                    Directory.CreateDirectory(archiveBackupPath);
                    await CopyDirectoryAsync(_archivePath, archiveBackupPath);

                    // 3. Создание метаданных бэкапа
                    var metadata = new BackupMetadata
                    {
                        CreatedAt = DateTime.UtcNow,
                        BackupType = BackupType.Full,
                        DatabaseSize = new FileInfo(Path.Combine(tempBackupDir, "database.sql")).Length,
                        FilesCount = Directory.GetFiles(archiveBackupPath, "*", SearchOption.AllDirectories).Length,
                        TotalSize = GetDirectorySize(tempBackupDir)
                    };

                    await File.WriteAllTextAsync(Path.Combine(tempBackupDir, "metadata.json"),
                        System.Text.Json.JsonSerializer.Serialize(metadata));

                    // 4. Создание архива
                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    ZipFile.CreateFromDirectory(tempBackupDir, backupPath, CompressionLevel.Optimal, false);

                    result.Success = true;
                    result.BackupPath = backupPath;
                    result.BackupSize = new FileInfo(backupPath).Length;
                    result.CreatedAt = DateTime.UtcNow;

                    await _auditService.LogSystemActionAsync(AuditAction.SystemBackup,
                        $"Создано полное резервное копирование: {Path.GetFileName(backupPath)}");

                    _logger.LogInformation("Полное резервное копирование успешно создано: {BackupPath}", backupPath);
                }
                finally
                {
                    // Очищаем временную папку
                    if (Directory.Exists(tempBackupDir))
                        Directory.Delete(tempBackupDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания полного резервного копирования");
                result.ErrorMessage = ex.Message;

                await _auditService.LogSystemActionAsync(AuditAction.SystemError,
                    "Ошибка создания резервного копирования", ex.Message);
            }

            return result;
        }

        public async Task<BackupResult> CreateDatabaseBackupAsync(string backupPath)
        {
            var result = new BackupResult { BackupType = BackupType.Database };

            try
            {
                _logger.LogInformation("Создание резервной копии базы данных в {BackupPath}", backupPath);

                // Парсим строку подключения для получения параметров
                var connectionParams = ParseConnectionString(_connectionString);

                var pgDumpPath = FindPgDumpPath();
                if (string.IsNullOrEmpty(pgDumpPath))
                {
                    result.ErrorMessage = "pg_dump не найден. Убедитесь, что PostgreSQL установлен и доступен в PATH";
                    return result;
                }

                // Команда для pg_dump
                var arguments = $"-h {connectionParams.Host} -p {connectionParams.Port} -U {connectionParams.Username} -d {connectionParams.Database} -f \"{backupPath}\" --verbose";

                var processInfo = new ProcessStartInfo
                {
                    FileName = pgDumpPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Устанавливаем пароль через переменную окружения
                processInfo.EnvironmentVariables["PGPASSWORD"] = connectionParams.Password;

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    result.ErrorMessage = "Не удалось запустить процесс pg_dump";
                    return result;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && File.Exists(backupPath))
                {
                    result.Success = true;
                    result.BackupPath = backupPath;
                    result.BackupSize = new FileInfo(backupPath).Length;
                    result.CreatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Резервная копия базы данных успешно создана: {BackupPath}", backupPath);
                }
                else
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    result.ErrorMessage = $"pg_dump завершился с кодом {process.ExitCode}: {error}";
                    _logger.LogError("Ошибка pg_dump: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания резервной копии базы данных");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<BackupResult> CreateFilesBackupAsync(string backupPath)
        {
            var result = new BackupResult { BackupType = BackupType.Files };

            try
            {
                _logger.LogInformation("Создание резервной копии файлов в {BackupPath}", backupPath);

                if (!Directory.Exists(_archivePath))
                {
                    result.ErrorMessage = $"Папка архива не существует: {_archivePath}";
                    return result;
                }

                var tempDir = Path.Combine(Path.GetTempPath(), $"files_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    await CopyDirectoryAsync(_archivePath, tempDir);

                    if (File.Exists(backupPath))
                        File.Delete(backupPath);

                    ZipFile.CreateFromDirectory(tempDir, backupPath, CompressionLevel.Optimal, false);

                    result.Success = true;
                    result.BackupPath = backupPath;
                    result.BackupSize = new FileInfo(backupPath).Length;
                    result.CreatedAt = DateTime.UtcNow;

                    _logger.LogInformation("Резервная копия файлов успешно создана: {BackupPath}", backupPath);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания резервной копии файлов");
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<RestoreResult> RestoreFromBackupAsync(string backupPath, BackupType backupType)
        {
            var result = new RestoreResult { BackupType = backupType };

            try
            {
                _logger.LogInformation("Начало восстановления из резервной копии {BackupPath}", backupPath);

                if (!File.Exists(backupPath))
                {
                    result.ErrorMessage = "Файл резервной копии не найден";
                    return result;
                }

                var tempDir = Path.Combine(Path.GetTempPath(), $"restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(tempDir);

                try
                {
                    // Извлекаем архив
                    ZipFile.ExtractToDirectory(backupPath, tempDir);

                    // Читаем метаданные если есть
                    var metadataPath = Path.Combine(tempDir, "metadata.json");
                    if (File.Exists(metadataPath))
                    {
                        var metadataJson = await File.ReadAllTextAsync(metadataPath);
                        var metadata = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(metadataJson);
                        result.BackupMetadata = metadata;
                    }

                    // Восстанавливаем базу данных
                    if (backupType == BackupType.Full || backupType == BackupType.Database)
                    {
                        var dbBackupFile = Path.Combine(tempDir, "database.sql");
                        if (File.Exists(dbBackupFile))
                        {
                            var dbRestoreResult = await RestoreDatabaseAsync(dbBackupFile);
                            if (!dbRestoreResult)
                            {
                                result.ErrorMessage = "Ошибка восстановления базы данных";
                                return result;
                            }
                        }
                    }

                    // Восстанавливаем файлы
                    if (backupType == BackupType.Full || backupType == BackupType.Files)
                    {
                        var filesBackupDir = Path.Combine(tempDir, "files");
                        if (Directory.Exists(filesBackupDir))
                        {
                            // Создаем резервную копию текущих файлов
                            var currentBackupDir = $"{_archivePath}_backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                            if (Directory.Exists(_archivePath))
                            {
                                Directory.Move(_archivePath, currentBackupDir);
                            }

                            // Восстанавливаем файлы
                            await CopyDirectoryAsync(filesBackupDir, _archivePath);
                        }
                    }

                    result.Success = true;
                    result.RestoredAt = DateTime.UtcNow;

                    await _auditService.LogSystemActionAsync(AuditAction.SystemRestore,
                        $"Восстановление из резервной копии: {Path.GetFileName(backupPath)}");

                    _logger.LogInformation("Восстановление успешно завершено из {BackupPath}", backupPath);
                }
                finally
                {
                    if (Directory.Exists(tempDir))
                        Directory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка восстановления из резервной копии");
                result.ErrorMessage = ex.Message;

                await _auditService.LogSystemActionAsync(AuditAction.SystemError,
                    "Ошибка восстановления из резервной копии", ex.Message);
            }

            return result;
        }

        public async Task<bool> VerifyBackupIntegrityAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                    return false;

                // Проверяем, что архив не поврежден
                using var archive = ZipFile.OpenRead(backupPath);
                foreach (var entry in archive.Entries)
                {
                    using var stream = entry.Open();
                    // Читаем первые байты для проверки
                    var buffer = new byte[1024];
                    await stream.ReadAsync(buffer, 0, buffer.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки целостности резервной копии {BackupPath}", backupPath);
                return false;
            }
        }

        public async Task<List<BackupInfo>> GetAvailableBackupsAsync(string backupDirectory)
        {
            var backups = new List<BackupInfo>();

            try
            {
                if (!Directory.Exists(backupDirectory))
                    return backups;

                var backupFiles = Directory.GetFiles(backupDirectory, "*.zip");

                foreach (var backupFile in backupFiles)
                {
                    var fileInfo = new FileInfo(backupFile);
                    var backupInfo = new BackupInfo
                    {
                        FileName = fileInfo.Name,
                        FilePath = fileInfo.FullName,
                        Size = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTime,
                        IsValid = await VerifyBackupIntegrityAsync(backupFile)
                    };

                    backups.Add(backupInfo);
                }

                return backups.OrderByDescending(b => b.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения списка резервных копий");
                return backups;
            }
        }

        public async Task<bool> CleanupOldBackupsAsync(string backupDirectory, int retentionDays = 30)
        {
            try
            {
                if (!Directory.Exists(backupDirectory))
                    return true;

                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var backupFiles = Directory.GetFiles(backupDirectory, "*.zip");
                var deletedCount = 0;

                foreach (var backupFile in backupFiles)
                {
                    var fileInfo = new FileInfo(backupFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(backupFile);
                        deletedCount++;
                        _logger.LogInformation("Удалена старая резервная копия: {BackupFile}", backupFile);
                    }
                }

                if (deletedCount > 0)
                {
                    await _auditService.LogSystemActionAsync(AuditAction.SystemBackup,
                        $"Очистка старых резервных копий: удалено {deletedCount} файлов");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка очистки старых резервных копий");
                return false;
            }
        }

        public async Task<bool> ScheduleAutomaticBackupAsync(TimeSpan interval)
        {
            // TODO: Реализовать планировщик автоматических резервных копий
            // Можно использовать Hangfire или создать собственный сервис
            await Task.CompletedTask;
            return true;
        }

        #region Вспомогательные методы

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var targetSubDir = Path.Combine(targetDir, Path.GetFileName(subDir));
                await CopyDirectoryAsync(subDir, targetSubDir);
            }
        }

        private long GetDirectorySize(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return 0;

            return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                           .Sum(file => new FileInfo(file).Length);
        }

        private string FindPgDumpPath()
        {
            // Проверяем стандартные пути
            var possiblePaths = new[]
            {
                "pg_dump", // В PATH
                @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
                @"C:\Program Files\PostgreSQL\13\bin\pg_dump.exe",
                "/usr/bin/pg_dump", // Linux
                "/usr/local/bin/pg_dump" // macOS
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(processInfo);
                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode == 0)
                            return path;
                    }
                }
                catch
                {
                    // Продолжаем поиск
                }
            }

            return string.Empty;
        }

        private (string Host, string Port, string Database, string Username, string Password) ParseConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            var host = "localhost";
            var port = "5432";
            var database = "";
            var username = "";
            var password = "";

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLower();
                    var value = keyValue[1].Trim();

                    switch (key)
                    {
                        case "host":
                        case "server":
                            host = value;
                            break;
                        case "port":
                            port = value;
                            break;
                        case "database":
                            database = value;
                            break;
                        case "username":
                        case "user id":
                        case "uid":
                            username = value;
                            break;
                        case "password":
                        case "pwd":
                            password = value;
                            break;
                    }
                }
            }

            return (host, port, database, username, password);
        }

        private async Task<bool> RestoreDatabaseAsync(string sqlFilePath)
        {
            try
            {
                var connectionParams = ParseConnectionString(_connectionString);
                var psqlPath = FindPsqlPath();

                if (string.IsNullOrEmpty(psqlPath))
                {
                    _logger.LogError("psql не найден для восстановления базы данных");
                    return false;
                }

                var arguments = $"-h {connectionParams.Host} -p {connectionParams.Port} -U {connectionParams.Username} -d {connectionParams.Database} -f \"{sqlFilePath}\"";

                var processInfo = new ProcessStartInfo
                {
                    FileName = psqlPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                processInfo.EnvironmentVariables["PGPASSWORD"] = connectionParams.Password;

                using var process = Process.Start(processInfo);
                if (process == null)
                    return false;

                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка восстановления базы данных");
                return false;
            }
        }

        private string FindPsqlPath()
        {
            return FindPgDumpPath().Replace("pg_dump", "psql");
        }

        #endregion
    }

    // Вспомогательные классы
    public class BackupResult
    {
        public bool Success { get; set; }
        public BackupType BackupType { get; set; }
        public string BackupPath { get; set; } = "";
        public long BackupSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ErrorMessage { get; set; } = "";
    }

    public class RestoreResult
    {
        public bool Success { get; set; }
        public BackupType BackupType { get; set; }
        public DateTime RestoredAt { get; set; }
        public string ErrorMessage { get; set; } = "";
        public BackupMetadata? BackupMetadata { get; set; }
    }

    public class BackupInfo
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsValid { get; set; }
    }

    public class BackupMetadata
    {
        public DateTime CreatedAt { get; set; }
        public BackupType BackupType { get; set; }
        public long DatabaseSize { get; set; }
        public int FilesCount { get; set; }
        public long TotalSize { get; set; }
    }

    public enum BackupType
    {
        Full,
        Database,
        Files
    }
}