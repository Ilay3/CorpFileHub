using CorpFileHub.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CorpFileHub.Infrastructure.Services
{
    public class YandexDiskService : IYandexDiskService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessToken;
        private readonly string _apiBaseUrl;
        private readonly ILogger<YandexDiskService> _logger;

        public YandexDiskService(IConfiguration configuration, ILogger<YandexDiskService> logger)
        {
            _httpClient = new HttpClient();
            _accessToken = configuration["YandexDisk:AccessToken"]!;
            _apiBaseUrl = configuration["YandexDisk:ApiBaseUrl"]!;
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {_accessToken}");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderPath)
        {
            try
            {
                // 1. Создаем папку если не существует
                await EnsureFolderExistsAsync(folderPath);

                // 2. Получаем URL для загрузки
                var uploadPath = $"{folderPath.TrimEnd('/')}/{fileName}";
                var uploadUrlResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/resources/upload?path={Uri.EscapeDataString(uploadPath)}&overwrite=true");

                if (!uploadUrlResponse.IsSuccessStatusCode)
                {
                    var error = await uploadUrlResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка получения URL для загрузки: {error}");
                }

                var uploadUrlData = JsonConvert.DeserializeObject<dynamic>(await uploadUrlResponse.Content.ReadAsStringAsync());
                string uploadUrl = uploadUrlData.href;

                // 3. Загружаем файл
                using var content = new StreamContent(fileStream);
                var uploadResponse = await _httpClient.PutAsync(uploadUrl, content);

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    var error = await uploadResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка загрузки файла: {error}");
                }

                _logger.LogInformation($"Файл {fileName} успешно загружен в {uploadPath}");
                return uploadPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка загрузки файла {fileName}");
                throw;
            }
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            try
            {
                // 1. Получаем URL для скачивания
                var downloadUrlResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/resources/download?path={Uri.EscapeDataString(filePath)}");

                if (!downloadUrlResponse.IsSuccessStatusCode)
                {
                    var error = await downloadUrlResponse.Content.ReadAsStringAsync();
                    throw new Exception($"Ошибка получения URL для скачивания: {error}");
                }

                var downloadUrlData = JsonConvert.DeserializeObject<dynamic>(await downloadUrlResponse.Content.ReadAsStringAsync());
                string downloadUrl = downloadUrlData.href;

                // 2. Скачиваем файл
                var response = await _httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Ошибка скачивания файла: {response.StatusCode}");
                }

                _logger.LogInformation($"Файл {filePath} успешно скачан");
                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка скачивания файла {filePath}");
                throw;
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Файл {filePath} успешно удален");
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Не удалось удалить файл {filePath}: {error}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка удаления файла {filePath}");
                return false;
            }
        }

        public async Task<string> GetEditLinkAsync(string filePath)
        {
            try
            {
                // Для файлов Office генерируем ссылку на редактирование
                var fileInfo = await GetFileInfoAsync(filePath);
                if (fileInfo != null)
                {
                    // Получаем публичную ссылку для редактирования
                    var publicResponse = await _httpClient.PutAsync($"{_apiBaseUrl}/resources/publish?path={Uri.EscapeDataString(filePath)}", null);

                    if (publicResponse.IsSuccessStatusCode)
                    {
                        var publicData = JsonConvert.DeserializeObject<dynamic>(await publicResponse.Content.ReadAsStringAsync());
                        string publicUrl = publicData.href;

                        // Формируем ссылку для редактирования в Яндекс.Документах
                        return $"https://docs.yandex.ru/docs/view?url={Uri.EscapeDataString(publicUrl)}";
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения ссылки для редактирования {filePath}");
                return string.Empty;
            }
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DateTime> GetLastModifiedAsync(string filePath)
        {
            try
            {
                var fileInfo = await GetFileInfoAsync(filePath);
                if (fileInfo?.modified != null)
                {
                    return DateTime.Parse(fileInfo.modified.ToString());
                }
                return DateTime.MinValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения даты модификации {filePath}");
                return DateTime.MinValue;
            }
        }

        private async Task<dynamic?> GetFileInfoAsync(string filePath)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/resources?path={Uri.EscapeDataString(filePath)}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<dynamic>(content);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async Task EnsureFolderExistsAsync(string folderPath)
        {
            try
            {
                // Проверяем существование папки
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}");

                if (!response.IsSuccessStatusCode)
                {
                    // Создаем папку
                    var createResponse = await _httpClient.PutAsync($"{_apiBaseUrl}/resources?path={Uri.EscapeDataString(folderPath)}", null);

                    if (createResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"Папка {folderPath} создана");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Не удалось создать папку {folderPath}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}