using Microsoft.AspNetCore.Mvc;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Application.UseCases.Files;
using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Enums;
using System.Text;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IYandexDiskService _yandexDiskService;
        private readonly INotificationService _notificationService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly UploadFileUseCase _uploadFileUseCase;
        private readonly ILogger<TestController> _logger;

        public TestController(
            IYandexDiskService yandexDiskService,
            INotificationService notificationService,
            IFileStorageService fileStorageService,
            IUserRepository userRepository,
            IAuditLogRepository auditLogRepository,
            UploadFileUseCase uploadFileUseCase,
            ILogger<TestController> logger)
        {
            _yandexDiskService = yandexDiskService;
            _notificationService = notificationService;
            _fileStorageService = fileStorageService;
            _userRepository = userRepository;
            _auditLogRepository = auditLogRepository;
            _uploadFileUseCase = uploadFileUseCase;
            _logger = logger;
        }

        /// <summary>
        /// Тест подключения к Яндекс.Диску
        /// GET: api/test/yandex-disk
        /// </summary>
        [HttpGet("yandex-disk")]
        public async Task<IActionResult> TestYandexDisk()
        {
            try
            {
                // Создаем тестовый файл
                var testContent = "Это тестовый файл для проверки интеграции с Яндекс.Диском\n" +
                                 $"Создан: {DateTime.Now}\n" +
                                 "CorpFileHub System Test";

                var testBytes = Encoding.UTF8.GetBytes(testContent);
                using var testStream = new MemoryStream(testBytes);

                // Загружаем на Яндекс.Диск
                var uploadedPath = await _yandexDiskService.UploadFileAsync(
                    testStream,
                    "test_file.txt",
                    "CorpFileHub_Tests");

                // Проверяем существование
                var exists = await _yandexDiskService.FileExistsAsync(uploadedPath);

                // Скачиваем файл обратно
                using var downloadedStream = await _yandexDiskService.DownloadFileAsync(uploadedPath);
                using var reader = new StreamReader(downloadedStream);
                var downloadedContent = await reader.ReadToEndAsync();

                return Ok(new
                {
                    Status = "SUCCESS",
                    Message = "Яндекс.Диск работает корректно",
                    UploadedPath = uploadedPath,
                    FileExists = exists,
                    DownloadedContent = downloadedContent,
                    TestTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка тестирования Яндекс.Диска");
                return BadRequest(new
                {
                    Status = "ERROR",
                    Message = "Ошибка подключения к Яндекс.Диску",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Тест отправки email
        /// GET: api/test/email?to=test@example.com
        /// </summary>
        [HttpGet("email")]
        public async Task<IActionResult> TestEmail([FromQuery] string to = "test@example.com")
        {
            try
            {
                await _notificationService.SendFileUploadNotificationAsync(
                    to,
                    "test_document.pdf",
                    "Тестовые документы");

                return Ok(new
                {
                    Status = "SUCCESS",
                    Message = "Email отправлен успешно",
                    SentTo = to,
                    TestTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки тестового email");
                return BadRequest(new
                {
                    Status = "ERROR",
                    Message = "Ошибка отправки email",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Тест локального хранилища файлов
        /// GET: api/test/file-storage
        /// </summary>
        [HttpGet("file-storage")]
        public async Task<IActionResult> TestFileStorage()
        {
            try
            {
                // Создаем тестовый файл
                var testContent = "Тестовое содержимое для проверки локального хранилища\n" +
                                 $"Время создания: {DateTime.Now}";
                var testBytes = Encoding.UTF8.GetBytes(testContent);

                using var testStream = new MemoryStream(testBytes);

                // Сохраняем версию файла
                var savedPath = await _fileStorageService.SaveFileVersionAsync(
                    testStream,
                    999, // тестовый ID файла
                    1,   // версия 1
                    "test_file.txt");

                // Проверяем существование
                var exists = await _fileStorageService.VersionExistsAsync(999, 1);

                // Читаем файл обратно
                using var retrievedStream = await _fileStorageService.GetFileVersionAsync(999, 1);
                using var reader = new StreamReader(retrievedStream);
                var retrievedContent = await reader.ReadToEndAsync();

                return Ok(new
                {
                    Status = "SUCCESS",
                    Message = "Локальное хранилище работает корректно",
                    SavedPath = savedPath,
                    FileExists = exists,
                    RetrievedContent = retrievedContent,
                    TestTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка тестирования локального хранилища");
                return BadRequest(new
                {
                    Status = "ERROR",
                    Message = "Ошибка локального хранилища",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Тест базы данных
        /// GET: api/test/database
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                // Создаем тестового пользователя
                var testUser = new User
                {
                    Email = "test@corpfilehub.system",
                    FullName = "Тестовый Пользователь",
                    PasswordHash = "test_hash",
                    Department = "IT",
                    Position = "Тестировщик"
                };

                var createdUser = await _userRepository.CreateAsync(testUser);

                // Создаем тестовую запись аудита
                var auditLog = new AuditLog
                {
                    UserId = createdUser.Id,
                    Action = AuditAction.SystemBackup,
                    EntityType = "Test",
                    EntityName = "Database Test",
                    Description = "Тестирование подключения к базе данных",
                    IpAddress = "127.0.0.1"
                };

                await _auditLogRepository.CreateAsync(auditLog);

                // Получаем пользователя обратно
                var retrievedUser = await _userRepository.GetByEmailAsync(testUser.Email);

                return Ok(new
                {
                    Status = "SUCCESS",
                    Message = "База данных работает корректно",
                    CreatedUserId = createdUser.Id,
                    RetrievedUser = new
                    {
                        retrievedUser?.Id,
                        retrievedUser?.Email,
                        retrievedUser?.FullName
                    },
                    TestTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка тестирования базы данных");
                return BadRequest(new
                {
                    Status = "ERROR",
                    Message = "Ошибка подключения к базе данных",
                    Error = ex.Message
                });
            }
        }

        /// <summary>
        /// Полный тест всех компонентов
        /// GET: api/test/all
        /// </summary>
        [HttpGet("all")]
        public async Task<IActionResult> TestAll()
        {
            var results = new Dictionary<string, object>();

            // Тест БД
            try
            {
                var testUser = new User
                {
                    Email = $"fulltest@corpfilehub.system",
                    FullName = "Full Test User",
                    PasswordHash = "test_hash",
                    Department = "Testing"
                };
                await _userRepository.CreateAsync(testUser);
                results["Database"] = new { Status = "SUCCESS", Message = "БД работает" };
            }
            catch (Exception ex)
            {
                results["Database"] = new { Status = "ERROR", Message = ex.Message };
            }

            // Тест Яндекс.Диска
            try
            {
                var testBytes = Encoding.UTF8.GetBytes("Full test content");
                using var testStream = new MemoryStream(testBytes);
                await _yandexDiskService.UploadFileAsync(testStream, "fulltest.txt", "CorpFileHub_Tests");
                results["YandexDisk"] = new { Status = "SUCCESS", Message = "Яндекс.Диск работает" };
            }
            catch (Exception ex)
            {
                results["YandexDisk"] = new { Status = "ERROR", Message = ex.Message };
            }

            // Тест локального хранилища
            try
            {
                var testBytes = Encoding.UTF8.GetBytes("Full test local storage");
                using var testStream = new MemoryStream(testBytes);
                await _fileStorageService.SaveFileVersionAsync(testStream, 998, 1, "fulltest.txt");
                results["FileStorage"] = new { Status = "SUCCESS", Message = "Локальное хранилище работает" };
            }
            catch (Exception ex)
            {
                results["FileStorage"] = new { Status = "ERROR", Message = ex.Message };
            }

            // Тест Email (только если указан параметр)
            if (Request.Query.ContainsKey("testEmail") && !string.IsNullOrEmpty(Request.Query["testEmail"]))
            {
                try
                {
                    await _notificationService.SendFileUploadNotificationAsync(
                        Request.Query["testEmail"],
                        "fulltest.txt",
                        "Full System Test");
                    results["Email"] = new { Status = "SUCCESS", Message = "Email отправлен" };
                }
                catch (Exception ex)
                {
                    results["Email"] = new { Status = "ERROR", Message = ex.Message };
                }
            }
            else
            {
                results["Email"] = new { Status = "SKIPPED", Message = "Добавьте ?testEmail=your@email.com для тестирования" };
            }

            return Ok(new
            {
                OverallStatus = results.Values.Any(r => ((dynamic)r).Status == "ERROR") ? "PARTIAL" : "SUCCESS",
                TestResults = results,
                TestTime = DateTime.Now,
                Message = "Полное тестирование системы завершено"
            });
        }
    }
}