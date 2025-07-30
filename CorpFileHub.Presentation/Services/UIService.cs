using Microsoft.JSInterop;

namespace CorpFileHub.Presentation.Services
{
    public interface IUIService
    {
        Task ShowNotificationAsync(string message, string type = "info", int duration = 5000);
        Task ShowConfirmationAsync(string message, string title = "Подтверждение");
        Task<bool> ConfirmActionAsync(string message, string title = "Подтверждение");
        Task ShowModalAsync(string modalId);
        Task HideModalAsync(string modalId);
        Task ScrollToElementAsync(string elementId);
        Task FocusElementAsync(string elementId);
        Task CopyToClipboardAsync(string text);
        Task DownloadFileAsync(string url, string fileName);
        Task RefreshPageAsync();
        Task RedirectAsync(string url);
        Task SetLocalStorageAsync(string key, string value);
        Task<string?> GetLocalStorageAsync(string key);
        Task RemoveLocalStorageAsync(string key);
        Task SetThemeAsync(string theme);
        Task ToggleFullscreenAsync();
        Task PrintPageAsync();
    }

    public class UIService : IUIService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<UIService> _logger;

        public UIService(IJSRuntime jsRuntime, ILogger<UIService> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        public async Task ShowNotificationAsync(string message, string type = "info", int duration = 5000)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("showNotification", message, type, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка показа уведомления");
            }
        }

        public async Task ShowConfirmationAsync(string message, string title = "Подтверждение")
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"{title}\n\n{message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка показа подтверждения");
            }
        }

        public async Task<bool> ConfirmActionAsync(string message, string title = "Подтверждение")
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("confirm", $"{title}\n\n{message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения действия");
                return false;
            }
        }

        public async Task ShowModalAsync(string modalId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("showModal", modalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка показа модального окна {ModalId}", modalId);
            }
        }

        public async Task HideModalAsync(string modalId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("hideModal", modalId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скрытия модального окна {ModalId}", modalId);
            }
        }

        public async Task ScrollToElementAsync(string elementId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("scrollToElement", elementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка прокрутки к элементу {ElementId}", elementId);
            }
        }

        public async Task FocusElementAsync(string elementId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("focusElement", elementId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка фокуса на элементе {ElementId}", elementId);
            }
        }

        public async Task CopyToClipboardAsync(string text)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("copyToClipboard", text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка копирования в буфер обмена");
            }
        }

        public async Task DownloadFileAsync(string url, string fileName)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("downloadFile", url, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка скачивания файла {FileName}", fileName);
            }
        }

        public async Task RefreshPageAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("location.reload");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления страницы");
            }
        }

        public async Task RedirectAsync(string url)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("window.location.assign", url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка перенаправления на {Url}", url);
            }
        }

        public async Task SetLocalStorageAsync(string key, string value)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка записи в localStorage");
            }
        }

        public async Task<string?> GetLocalStorageAsync(string key)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка чтения из localStorage");
                return null;
            }
        }

        public async Task RemoveLocalStorageAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления из localStorage");
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("setTheme", theme);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка установки темы {Theme}", theme);
            }
        }

        public async Task ToggleFullscreenAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("toggleFullscreen");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка переключения полноэкранного режима");
            }
        }

        public async Task PrintPageAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("window.print");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка печати страницы");
            }
        }
    }
}