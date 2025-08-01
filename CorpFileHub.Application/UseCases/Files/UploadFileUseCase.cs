﻿using CorpFileHub.Domain.Entities;
using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Linq;

namespace CorpFileHub.Application.UseCases.Files
{
    public class UploadFileUseCase
    {
        private readonly IFileRepository _fileRepository;
        private readonly IFolderRepository _folderRepository;
        private readonly IYandexDiskService _yandexDiskService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly INotificationService _notificationService;
        private readonly long _maxFileSize;
        private readonly HashSet<string> _allowedExtensions;

        public UploadFileUseCase(
            IFileRepository fileRepository,
            IFolderRepository folderRepository,
            IYandexDiskService yandexDiskService,
            IFileStorageService fileStorageService,
            IAuditLogRepository auditLogRepository,
            INotificationService notificationService,
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _yandexDiskService = yandexDiskService;
            _fileStorageService = fileStorageService;
            _auditLogRepository = auditLogRepository;
            _notificationService = notificationService;

            _maxFileSize = configuration.GetValue<long>("FileStorage:MaxFileSize", 104857600);
            _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>()?.Select(e => e.ToLower()).ToHashSet() ?? new HashSet<string>();
        }

        public async Task<FileItem> ExecuteAsync(Stream fileStream, string fileName, int folderId, int userId, string comment = "")
        {
            // 1. Проверяем права доступа к папке
            var folder = await _folderRepository.GetByIdAsync(folderId);
            if (folder == null)
                throw new ArgumentException("Папка не найдена");

            if (fileStream.Length > _maxFileSize)
                throw new InvalidOperationException("Размер файла превышает допустимый предел");

            var extension = Path.GetExtension(fileName).ToLower();
            if (_allowedExtensions.Any() && !_allowedExtensions.Contains(extension))
                throw new InvalidOperationException($"Формат '{extension}' не поддерживается");

            // 2. Проверяем уникальность имени файла
            if (await _fileRepository.FileExistsAsync(fileName, folderId))
            {
                fileName = await GenerateUniqueFileNameAsync(fileName, folderId);
            }

            // 3. Определяем путь и расширение
            extension = Path.GetExtension(fileName);
            var contentType = GetContentType(extension);
            var yandexPath = $"{folder.YandexDiskPath}/{fileName}";

            // 4. Загружаем файл на Яндекс.Диск
            var yandexDiskPath = await _yandexDiskService.UploadFileAsync(fileStream, fileName, folder.YandexDiskPath);

            // 5. Создаем запись о файле
            var fileItem = new FileItem
            {
                Name = fileName,
                Path = $"{folder.Path}/{fileName}",
                YandexDiskPath = yandexDiskPath,
                Size = fileStream.Length,
                ContentType = contentType,
                Extension = extension,
                Status = FileStatus.Active,
                OwnerId = userId,
                FolderId = folderId,
                Description = comment
            };

            var createdFile = await _fileRepository.CreateAsync(fileItem);

            // 6. Сохраняем первую версию локально
            fileStream.Position = 0; // Сброс позиции потока
            var localPath = await _fileStorageService.SaveFileVersionAsync(fileStream, createdFile.Id, 1, fileName);

            var fileVersion = new FileVersion
            {
                FileId = createdFile.Id,
                LocalPath = localPath,
                YandexDiskPath = yandexDiskPath,
                Version = 1,
                Size = fileStream.Length,
                CreatedById = userId,
                Comment = comment,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // Добавляем версию к файлу
            createdFile.Versions.Add(fileVersion);
            await _fileRepository.UpdateAsync(createdFile);


            // 7. Создаем аудит лог
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = userId,
                Action = AuditAction.FileUpload,
                EntityType = "File",
                EntityId = createdFile.Id,
                EntityName = fileName,
                Description = $"Файл '{fileName}' загружен в папку '{folder.Name}'"
            });

            // 8. Отправляем уведомление (если настроено)
            try
            {
                // Можно добавить логику уведомления других пользователей
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не прерываем операцию
            }

            return createdFile;
        }

        private async Task<string> GenerateUniqueFileNameAsync(string originalName, int folderId)
        {
            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalName);
            var extension = Path.GetExtension(originalName);
            var counter = 1;

            string newName;
            do
            {
                newName = $"{nameWithoutExt}_{counter}{extension}";
                counter++;
            } while (await _fileRepository.FileExistsAsync(newName, folderId));

            return newName;
        }

        private string GetContentType(string extension)
        {
            return extension.ToLower() switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}