using CorpFileHub.Application.UseCases.Files;
using CorpFileHub.Application.UseCases.Folders;
using Microsoft.Extensions.DependencyInjection;

namespace CorpFileHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Регистрация HTTP Context Accessor для аудита
            //services.AddHttpContextAccessor();

            // Регистрация Application сервисов
            services.AddScoped<Services.IAuditService, Services.AuditService>();
            services.AddScoped<Services.IFileManagementService, Services.FileManagementService>();
            services.AddScoped<Services.IAccessControlService, Services.AccessControlService>();
            services.AddScoped<Services.IAuthenticationService, Services.AuthenticationService>();
            services.AddScoped<Services.IUserContextService, Services.UserContextService>();

            // Регистрация Use Cases для файлов
            services.AddScoped<UploadFileUseCase>();
            services.AddScoped<DownloadFileUseCase>();
            services.AddScoped<OpenForEditingUseCase>();
            services.AddScoped<GetPreviewLinkUseCase>();
            services.AddScoped<DeleteFileUseCase>();
            services.AddScoped<RollbackFileVersionUseCase>();
            services.AddScoped<RestoreFileUseCase>();

            // Регистрация Use Cases для папок
            services.AddScoped<CreateFolderUseCase>();
            services.AddScoped<DeleteFolderUseCase>();
            services.AddScoped<MoveFolderUseCase>();

            // Регистрация Use Cases для прав доступа
            services.AddScoped<UseCases.Access.CheckAccessUseCase>();
            services.AddScoped<UseCases.Access.SetAccessRightsUseCase>();
            services.AddScoped<UseCases.Auth.LoginUserUseCase>();

            // Регистрация Use Cases для аудита
            services.AddScoped<UseCases.Audit.GetAuditLogUseCase>();

            // Добавление AutoMapper если будет использоваться
            // services.AddAutoMapper(typeof(DependencyInjection));

            // Фоновые службы
            services.AddHostedService<Services.FileEditMonitorService>();
            services.AddHostedService<Services.AuditCleanupService>();
            services.AddHostedService<Services.FileVersionCleanupService>();

            return services;
        }
    }
}