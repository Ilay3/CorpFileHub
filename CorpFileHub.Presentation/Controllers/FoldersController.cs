using Microsoft.AspNetCore.Mvc;
using CorpFileHub.Application.UseCases.Folders;
using CorpFileHub.Application.Services;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace CorpFileHub.Presentation.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly CreateFolderUseCase _createFolderUseCase;
        private readonly DeleteFolderUseCase _deleteFolderUseCase;
        private readonly MoveFolderUseCase _moveFolderUseCase;
        private readonly IFolderRepository _folderRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IAccessControlService _accessControlService;
        private readonly IAuditService _auditService;
        private readonly ILogger<FoldersController> _logger;
        private readonly IUserContextService _userContext;

        public FoldersController(
            CreateFolderUseCase createFolderUseCase,
            DeleteFolderUseCase deleteFolderUseCase,
            MoveFolderUseCase moveFolderUseCase,
            IFolderRepository folderRepository,
            IFileRepository fileRepository,
            IAccessControlService accessControlService,
            IAuditService auditService,
            ILogger<FoldersController> logger,
            IUserContextService userContext)
        {
            _createFolderUseCase = createFolderUseCase;
            _deleteFolderUseCase = deleteFolderUseCase;
            _moveFolderUseCase = moveFolderUseCase;
            _folderRepository = folderRepository;
            _fileRepository = fileRepository;
            _accessControlService = accessControlService;
            _auditService = auditService;
            _logger = logger;
            _userContext = userContext;
        }

        /// <summary>
        /// Создание новой папки
        /// POST: api/folders
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] FolderCreateDto folderDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderDto.Name))
                    return BadRequest(new { error = "Название папки не может быть пустым" });

                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var createdFolder = await _createFolderUseCase.ExecuteAsync(
                    folderDto.Name,
                    folderDto.ParentFolderId,
                    userId,
                    folderDto.Description ?? "");

                var folderResponse = new FolderDto
                {
                    Id = createdFolder.Id,
                    Name = createdFolder.Name,
                    Path = createdFolder.Path,
                    CreatedAt = createdFolder.CreatedAt,
                    UpdatedAt = createdFolder.UpdatedAt,
                    Description = createdFolder.Description,
                    ParentFolderId = createdFolder.ParentFolderId,
                    OwnerName = createdFolder.Owner?.FullName ?? ""
                };

                return CreatedAtAction(nameof(GetFolder), new { id = createdFolder.Id }, folderResponse);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания папки {FolderName}", folderDto.Name);
                return StatusCode(500, new { error = "Ошибка при создании папки" });
            }
        }

        /// <summary>
        /// Получение информации о папке
        /// GET: api/folders/{id}
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFolder(int id)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var canRead = await _accessControlService.CanReadFolderAsync(id, userId);
                if (!canRead)
                    return Forbid("Недостаточно прав для просмотра папки");

                var folder = await _folderRepository.GetByIdAsync(id);
                if (folder == null)
                    return NotFound(new { error = "Папка не найдена" });

                // Получаем содержимое папки
                var subFolders = await _folderRepository.GetByParentIdAsync(id);
                var files = await _fileRepository.GetByFolderIdAsync(id);

                var folderDto = new FolderDto
                {
                    Id = folder.Id,
                    Name = folder.Name,
                    Path = folder.Path,
                    CreatedAt = folder.CreatedAt,
                    UpdatedAt = folder.UpdatedAt,
                    Description = folder.Description,
                    Tags = folder.Tags,
                    ParentFolderId = folder.ParentFolderId,
                    ParentFolderName = folder.ParentFolder?.Name,
                    SubFoldersCount = subFolders.Count(),
                    FilesCount = files.Count(),
                    TotalSize = files.Sum(f => f.Size),
                    OwnerName = folder.Owner?.FullName ?? "",
                    CanRead = true,
                    CanEdit = await _accessControlService.CanEditFolderAsync(id, userId),
                    CanDelete = await _accessControlService.CanDeleteFolderAsync(id, userId),
                    CanCreateFiles = await _accessControlService.CanCreateInFolderAsync(id, userId),
                    CanCreateFolders = await _accessControlService.CanCreateInFolderAsync(id, userId)
                };

                return Ok(folderDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения информации о папке {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при получении информации о папке" });
            }
        }

        /// <summary>
        /// Получение дерева папок
        /// GET: api/folders/tree
        /// </summary>
        [HttpGet("tree")]
        public async Task<IActionResult> GetFolderTree([FromQuery] int? parentId = null)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var folders = await _folderRepository.GetByParentIdAsync(parentId);
                var accessibleFolders = new List<FolderTreeDto>();

                foreach (var folder in folders)
                {
                    var canRead = await _accessControlService.CanReadFolderAsync(folder.Id, userId);
                    if (canRead)
                    {
                        var subFolders = await _folderRepository.GetByParentIdAsync(folder.Id);
                        var files = await _fileRepository.GetByFolderIdAsync(folder.Id);

                        accessibleFolders.Add(new FolderTreeDto
                        {
                            Id = folder.Id,
                            Name = folder.Name,
                            Path = folder.Path,
                            ParentFolderId = folder.ParentFolderId,
                            HasFiles = files.Any(),
                            FilesCount = files.Count(),
                            SubFoldersCount = subFolders.Count(),
                            CanAccess = true
                        });
                    }
                }

                return Ok(accessibleFolders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения дерева папок");
                return StatusCode(500, new { error = "Ошибка при получении дерева папок" });
            }
        }

        /// <summary>
        /// Получение содержимого папки
        /// GET: api/folders/{id}/content
        /// </summary>
        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetFolderContent(int id)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var canRead = await _accessControlService.CanReadFolderAsync(id, userId);
                if (!canRead)
                    return Forbid("Недостаточно прав для просмотра содержимого папки");

                var subFolders = await _folderRepository.GetByParentIdAsync(id);
                var files = await _fileRepository.GetByFolderIdAsync(id);

                var folderDtos = new List<FolderDto>();
                foreach (var folder in subFolders)
                {
                    if (await _accessControlService.CanReadFolderAsync(folder.Id, userId))
                    {
                        folderDtos.Add(new FolderDto
                        {
                            Id = folder.Id,
                            Name = folder.Name,
                            Path = folder.Path,
                            CreatedAt = folder.CreatedAt,
                            UpdatedAt = folder.UpdatedAt,
                            Description = folder.Description,
                            ParentFolderId = folder.ParentFolderId
                        });
                    }
                }

                var fileDtos = new List<FileDto>();
                foreach (var file in files)
                {
                    if (await _accessControlService.CanReadFileAsync(file.Id, userId))
                    {
                        fileDtos.Add(new FileDto
                        {
                            Id = file.Id,
                            Name = file.Name,
                            Size = file.Size,
                            Extension = file.Extension,
                            CreatedAt = file.CreatedAt,
                            UpdatedAt = file.UpdatedAt,
                            Status = file.Status
                        });
                    }
                }

                return Ok(new
                {
                    folders = folderDtos,
                    files = fileDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения содержимого папки {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при получении содержимого папки" });
            }
        }

        /// <summary>
        /// Удаление папки
        /// DELETE: api/folders/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id, [FromQuery] bool force = false)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var success = await _deleteFolderUseCase.ExecuteAsync(id, userId, force);

                if (success)
                {
                    return Ok(new { success = true, message = "Папка успешно удалена" });
                }
                else
                {
                    return NotFound(new { error = "Папка не найдена" });
                }
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
                _logger.LogError(ex, "Ошибка удаления папки {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при удалении папки" });
            }
        }

        /// <summary>
        /// Перемещение папки
        /// PUT: api/folders/{id}/move
        /// </summary>
        [HttpPut("{id}/move")]
        public async Task<IActionResult> MoveFolder(int id, [FromBody] FolderMoveDto moveDto)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var success = await _moveFolderUseCase.ExecuteAsync(id, moveDto.NewParentFolderId, userId);

                if (success)
                {
                    return Ok(new { success = true, message = "Папка успешно перемещена" });
                }
                else
                {
                    return BadRequest(new { error = "Не удалось переместить папку" });
                }
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перемещения папки {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при перемещении папки" });
            }
        }

        /// <summary>
        /// Переименование папки
        /// PUT: api/folders/{id}/rename
        /// </summary>
        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameFolder(int id, [FromBody] RenameFolderRequest request)
        {
            try
            {
                var userId = _userContext.GetCurrentUserId() ?? 0;
                if (userId == 0)
                    return Unauthorized();

                var canEdit = await _accessControlService.CanEditFolderAsync(id, userId);
                if (!canEdit)
                    return Forbid("Недостаточно прав для переименования папки");

                var folder = await _folderRepository.GetByIdAsync(id);
                if (folder == null)
                    return NotFound(new { error = "Папка не найдена" });

                // Проверяем уникальность имени
                if (await _folderRepository.FolderExistsAsync(request.NewName, folder.ParentFolderId))
                    return Conflict(new { error = "Папка с таким именем уже существует" });

                var oldName = folder.Name;
                folder.Name = request.NewName;
                folder.UpdatedAt = DateTime.UtcNow;

                await _folderRepository.UpdateAsync(folder);

                await _auditService.LogSuccessAsync(userId, Domain.Enums.AuditAction.FolderRename, "Folder", id,
                    request.NewName, $"Папка переименована с '{oldName}' на '{request.NewName}'");

                return Ok(new { success = true, message = "Папка успешно переименована" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка переименования папки {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при переименовании папки" });
            }
        }

        /// <summary>
        /// Получение хлебных крошек для папки
        /// GET: api/folders/{id}/breadcrumbs
        /// </summary>
        [HttpGet("{id}/breadcrumbs")]
        public async Task<IActionResult> GetBreadcrumbs(int id)
        {
            try
            {
                var breadcrumbs = new List<BreadcrumbDto>();
                var currentFolder = await _folderRepository.GetByIdAsync(id);

                while (currentFolder != null)
                {
                    breadcrumbs.Insert(0, new BreadcrumbDto
                    {
                        Id = currentFolder.Id,
                        Name = currentFolder.Name,
                        Path = currentFolder.Path
                    });

                    if (currentFolder.ParentFolderId.HasValue)
                    {
                        currentFolder = await _folderRepository.GetByIdAsync(currentFolder.ParentFolderId.Value);
                    }
                    else
                    {
                        break;
                    }
                }

                // Добавляем корневую папку
                breadcrumbs.Insert(0, new BreadcrumbDto
                {
                    Id = 0,
                    Name = "Корень",
                    Path = "/"
                });

                return Ok(breadcrumbs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения хлебных крошек для папки {FolderId}", id);
                return StatusCode(500, new { error = "Ошибка при получении навигации" });
            }
        }
    }

    // Вспомогательные классы для запросов
    public class RenameFolderRequest
    {
        public string NewName { get; set; } = "";
    }
}