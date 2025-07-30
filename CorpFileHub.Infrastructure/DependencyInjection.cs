using CorpFileHub.Domain.Interfaces.Repositories;
using CorpFileHub.Domain.Interfaces.Services;
using CorpFileHub.Infrastructure.Repositories;
using CorpFileHub.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

namespace CorpFileHub.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Регистрация HttpClient
            services.AddHttpClient();

            // Регистрация репозиториев
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IFileRepository, FileRepository>();
            services.AddScoped<IFolderRepository, FolderRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IAuthLogRepository, AuthLogRepository>();
            services.AddScoped<IUnauthorizedAccessLogRepository, UnauthorizedAccessLogRepository>();
            services.AddScoped<ISystemErrorLogRepository, SystemErrorLogRepository>();

            // Регистрация внешних сервисов
            services.AddScoped<IYandexDiskService, YandexDiskService>();
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<INotificationService, EmailNotificationService>();

            // Регистрация UI сервисов
            //services.AddScoped<ISignalRService, SignalRService>();

            return services;
        }
    }
}