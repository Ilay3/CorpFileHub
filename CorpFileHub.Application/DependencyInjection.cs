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

            // Регистрация Use Cases для файлов
            services.AddScoped<UploadFileUseCase>();
            services.AddScoped<DownloadFileUseCase>();
            services.AddScoped<OpenForEditingUseCase>();
            services.AddScoped<DeleteFileUseCase>();
            services.AddScoped<RollbackFileVersionUseCase>();

            // Регистрация Use Cases для папок
            services.AddScoped<CreateFolderUseCase>();
            services.AddScoped<DeleteFolderUseCase>();
            services.AddScoped<MoveFolderUseCase>();

            // Регистрация Use Cases для прав доступа
            services.AddScoped<UseCases.Access.CheckAccessUseCase>();
            services.AddScoped<UseCases.Access.SetAccessRightsUseCase>();

            // Регистрация Use Cases для аудита
            services.AddScoped<UseCases.Audit.GetAuditLogUseCase>();

            // Добавление AutoMapper если будет использоваться
            // services.AddAutoMapper(typeof(DependencyInjection));

            return services;
        }
    }
}