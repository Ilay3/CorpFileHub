using CorpFileHub.Domain.Interfaces.Services;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CorpFileHub.Infrastructure.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly bool _emailEnabled;

        public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _emailEnabled = _configuration.GetValue<bool>("Email:Enabled", true);
        }

        public async Task SendFileUploadNotificationAsync(string userEmail, string fileName, string folderPath)
        {
            if (!_emailEnabled) return;

            var subject = "[CorpFileHub] Новый файл загружен";
            var message = $@"
                Новый файл загружен в систему CorpFileHub:

                📁 Файл: {fileName}
                📂 Папка: {folderPath}
                ⏰ Время: {DateTime.Now:dd.MM.yyyy HH:mm}

                Для доступа к файлу войдите в систему CorpFileHub.

                ---
                Это автоматическое уведомление системы CorpFileHub
                ";

            await SendEmailAsync(userEmail, subject, message);
        }

        public async Task SendFileEditNotificationAsync(string userEmail, string fileName, string editorName)
        {
            if (!_emailEnabled) return;

            var subject = "[CorpFileHub] Файл изменен";
            var message = $@"
                Файл был изменен в системе CorpFileHub:

                📄 Файл: {fileName}
                👤 Редактор: {editorName}
                ⏰ Время: {DateTime.Now:dd.MM.yyyy HH:mm}

                Новая версия файла доступна в системе.

                ---
                Это автоматическое уведомление системы CorpFileHub
                ";

            await SendEmailAsync(userEmail, subject, message);
        }

        public async Task SendAccessChangedNotificationAsync(string userEmail, string entityName, string newAccess)
        {
            if (!_emailEnabled) return;

            var subject = "[CorpFileHub] Изменены права доступа";
            var message = $@"
                Ваши права доступа были изменены:

                📄 Объект: {entityName}
                🔐 Новые права: {newAccess}
                ⏰ Время: {DateTime.Now:dd.MM.yyyy HH:mm}

                Обратитесь к администратору системы при возникновении вопросов.

                ---
                Это автоматическое уведомление системы CorpFileHub
                ";

            await SendEmailAsync(userEmail, subject, message);
        }

        public async Task SendPasswordResetAsync(string userEmail, string resetLink)
        {
            if (!_emailEnabled) return;

            var subject = "[CorpFileHub] Сброс пароля";
            var message = $@"
                Для сброса пароля перейдите по ссылке:

                {resetLink}

                Если вы не запрашивали сброс пароля, проигнорируйте это сообщение.

                ---
                Автоматическое уведомление системы CorpFileHub
                ";

            await SendEmailAsync(userEmail, subject, message);
        }

        public async Task SendErrorNotificationAsync(string adminEmail, string errorMessage, Exception exception)
        {
            if (!_emailEnabled) return;

            var subject = "[CorpFileHub] Системная ошибка";
            var message = $@"
                В системе CorpFileHub произошла ошибка:

                ❌ Сообщение: {errorMessage}
                ⏰ Время: {DateTime.Now:dd.MM.yyyy HH:mm}
                🔍 Детали: {exception.Message}

                Стек вызовов:
                {exception.StackTrace}

                Требуется внимание администратора.

                ---
                Автоматическое уведомление системы CorpFileHub
                ";

            await SendEmailAsync(adminEmail, subject, message);
        }

        public async Task SendBulkNotificationAsync(IEnumerable<string> emails, string subject, string message)
        {
            if (!_emailEnabled) return;

            var tasks = emails.Select(email => SendEmailAsync(email, subject, message));
            await Task.WhenAll(tasks);
        }

        private async Task SendEmailAsync(string to, string subject, string messageText)
        {
            try
            {
                var message = new MimeMessage();

                var fromName = _configuration["Email:FromName"] ?? "CorpFileHub System";
                var fromEmail = _configuration["Email:Username"];

                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                message.Body = new TextPart("plain")
                {
                    Text = messageText
                };

                using var client = new SmtpClient();

                var smtpServer = _configuration["Email:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");

                _logger.LogInformation($"Подключение к {smtpServer}:{smtpPort}...");

                // ИСПРАВЛЕНИЕ: Явно указываем StartTls для Gmail
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                var username = _configuration["Email:Username"];
                var password = _configuration["Email:Password"];

                _logger.LogInformation("Аутентификация...");
                await client.AuthenticateAsync(username, password);

                _logger.LogInformation($"Отправка сообщения на {to}...");
                await client.SendAsync(message);

                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email успешно отправлен на {to}: {subject}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки email на {to}. Тема: {subject}");

                // Не пробрасываем исключение, чтобы не ломать основной функционал
                // если есть проблемы с почтой
            }
        }
    }
}