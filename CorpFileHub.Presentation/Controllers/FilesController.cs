using Microsoft.AspNetCore.Mvc;
using CorpFileHub.Application.UseCases.Files;
using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Application.DTOs;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly UploadFileUseCase _uploadFileUseCase;
        private readonly DownloadFileUseCase _downloadFileUseCase;
        private readonly OpenForEditingUseCase _openForEditingUseCase;
        private readonly DeleteFileUseCase _deleteFileUseCase;
        private readonly RollbackFileVersionUseCase _rollbackVersionUseCase;
        private readonly IFileManagementService _fileManagementService;
        private readonly IFileRepository _fileRepository;
        private readonly IAuditService _auditService;
        private readonly ILogger<FilesController> _logger;

        public FilesController(
            UploadFileUseCase uploadFileUseCase,
            DownloadFileUseCase downloadFileUseCase,
            OpenForEditingUseCase openForEditingUseCase,
            DeleteFileUseCase deleteFileUseCase,
            RollbackFileVersionUseCase rollbackVersionUseCase,
            IFileManagementService fileManagementService,
            IFileRepository fileRepository,
            IAuditService auditService,
            ILogger<FilesController> logger)
        {
            _uploadFileUseCase = uploadFileUseCase;
            _downloadFileUseCase = downloadFileUseCase;
            _openForEditingUseCase = openForEditingUseCase;
            _deleteFileUseCase = deleteFileUseCase;
            _rollbackVersionUseCase = rollbackVersionUseCase;
            _fileManagementService = fileManagementService;
            _fileRepository = fileRepository;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Загрузка файла
        /// POST: api/files/upload
        /// </summary>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file, [FromForm] int folderId = 0, [FromForm] string comment = "")
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Файл не выбран или пуст" });

                // TODO: Получить текущего пользователя из системы аутентификации
                var userId = 1; // Временно

                using var stream = file.OpenReadStream();
                var uploadedFile = await _uploadFileUseCase.ExecuteAsync(stream, file.FileName, folderId, userId, comment);

                return Ok(new
                {
                    success = true,
                    file = new
                    {
                        id = uploadedFile.Id,
                        name = uploadedFile.Name,
                        size = uploadedFile.Size,
                        path = uploadedFile.Path,
                        uploadedAt = uploadedFile.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки файла {FileName}", file?.FileName);
                return StatusCode(500, new { error = "Ошибка при загрузке файла", details = ex.Message });
            }
        }

        /// <summary>
        /// Скачивание файла
        /// GET: api/files/{id}/download
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var (fileStream, fileName, contentType) = await _downloadFileUseCase.ExecuteAsync(id, userId);

                return File(fileStream, contentType, fileName);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания файла {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при скачивании файла" });
            }
        }

        /// <summary>
        /// Открытие файла для редактирования
        /// POST: api/files/{id}/edit
        /// </summary>
        [HttpPost("{id}/edit")]
        public async Task<IActionResult> OpenForEditing(int id)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var editLink = await _openForEditingUseCase.ExecuteAsync(id, userId);

                return Ok(new
                {
                    success = true,
                    editLink = editLink,
                    message = "Файл открыт для редактирования"
                });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка открытия файла для редактирования {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при открытии файла" });
            }
        }

        /// <summary>
        /// Удаление файла
        /// DELETE: api/files/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var success = await _deleteFileUseCase.ExecuteAsync(id, userId);

                if (success)
                {
                    return Ok(new { success = true, message = "Файл успешно удален" });
                }
                else
                {
                    return NotFound(new { error = "Файл не найден" });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления файла {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при удалении файла" });
            }
        }

        /// <summary>
        /// Получение информации о файле
        /// GET: api/files/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var file = await _fileManagementService.GetFileWithAccessCheckAsync(id, userId);
                if (file == null)
                    return NotFound(new { error = "Файл не найден или нет доступа" });

                var fileDto = new FileDto
                {
                    Id = file.Id,
                    Name = file.Name,
                    Path = file.Path,
                    Size = file.Size,
                    ContentType = file.ContentType,
                    Extension = file.Extension,
                    CreatedAt = file.CreatedAt,
                    UpdatedAt = file.UpdatedAt,
                    Status = file.Status,
                    Tags = file.Tags,
                    Description = file.Description,
                    OwnerName = file.Owner?.FullName ?? "",
                    FolderName = file.Folder?.Name ?? "",
                    FolderId = file.FolderId,
                    VersionsCount = file.Versions.Count
                };

                return Ok(fileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения информации о файле {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при получении информации о файле" });
            }
        }

        /// <summary>
        /// Получение версий файла
        /// GET: api/files/{id}/versions
        /// </summary>
        [HttpGet("{id}/versions")]
        public async Task<IActionResult> GetFileVersions(int id)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var versions = await _fileManagementService.GetFileVersionsAsync(id, userId);

                var versionDtos = versions.Select(v => new FileVersionDto
                {
                    Id = v.Id,
                    FileId = v.FileId,
                    Version = v.Version,
                    Size = v.Size,
                    CreatedAt = v.CreatedAt,
                    CreatedByName = v.CreatedBy?.FullName ?? "",
                    Comment = v.Comment,
                    IsActive = v.IsActive
                }).ToList();

                return Ok(versionDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения версий файла {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при получении версий файла" });
            }
        }

        /// <summary>
        /// Откат к версии файла
        /// POST: api/files/{id}/rollback/{versionId}
        /// </summary>
        [HttpPost("{id}/rollback/{versionId}")]
        public async Task<IActionResult> RollbackToVersion(int id, int versionId, [FromBody] RollbackRequest request)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                // Найти номер версии по ID
                var file = await _fileRepository.GetByIdAsync(id);
                if (file == null)
                    return NotFound(new { error = "Файл не найден" });

                var version = file.Versions.FirstOrDefault(v => v.Id == versionId);
                if (version == null)
                    return NotFound(new { error = "Версия не найдена" });

                var success = await _rollbackVersionUseCase.ExecuteAsync(id, version.Version, userId, request.Comment ?? "");

                if (success)
                {
                    return Ok(new { success = true, message = $"Файл откачен к версии {version.Version}" });
                }
                else
                {
                    return BadRequest(new { error = "Не удалось откатить версию" });
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отката файла {FileId} к версии {VersionId}", id, versionId);
                return StatusCode(500, new { error = "Ошибка при откате версии" });
            }
        }

        /// <summary>
        /// Переименование файла
        /// PUT: api/files/{id}/rename
        /// </summary>
        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameFile(int id, [FromBody] RenameRequest request)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var success = await _fileManagementService.RenameFileAsync(id, request.NewName, userId);

                if (success)
                {
                    return Ok(new { success = true, message = "Файл успешно переименован" });
                }
                else
                {
                    return BadRequest(new { error = "Не удалось переименовать файл" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка переименования файла {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при переименовании файла" });
            }
        }

        /// <summary>
        /// Перемещение файла в другую папку
        /// PUT: api/files/{id}/move
        /// </summary>
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFile(int id, [FromBody] MoveRequest request)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var success = await _fileManagementService.MoveFileToFolderAsync(id, request.TargetFolderId, userId);

                if (success)
                {
                    return Ok(new { success = true, message = "Файл успешно перемещен" });
                }
                else
                {
                    return BadRequest(new { error = "Не удалось переместить файл" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перемещения файла {FileId}", id);
                return StatusCode(500, new { error = "Ошибка при перемещении файла" });
            }
        }

        /// <summary>
        /// Поиск файлов
        /// GET: api/files/search
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchFiles([FromQuery] string query, [FromQuery] int? folderId = null)
        {
            try
            {
                // TODO: Получить текущего пользователя
                var userId = 1; // Временно

                var files = await _fileManagementService.SearchUserFilesAsync(userId, query, new SearchFilters { FolderId = folderId });

                var fileDtos = files.Select(f => new FileDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Path = f.Path,
                    Size = f.Size,
                    ContentType = f.ContentType,
                    Extension = f.Extension,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    Status = f.Status,
                    FolderName = f.Folder?.Name ?? "",
                    FolderId = f.FolderId
                }).ToList();

                return Ok(fileDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка поиска файлов по запросу {Query}", query);
                return StatusCode(500, new { error = "Ошибка при поиске файлов" });
            }
        }
    }

    // Вспомогательные классы для запросов
    public class RollbackRequest
    {
        public string? Comment { get; set; }
    }

    public class RenameRequest
    {
        public string NewName { get; set; } = "";
    }

    public class MoveRequest
    {
        public int TargetFolderId { get; set; }
    }
}