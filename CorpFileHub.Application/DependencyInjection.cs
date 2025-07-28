using CorpFileHub.Application.UseCases.Files;
using CorpFileHub.Application.UseCases.Folders;
using Microsoft.Extensions.DependencyInjection;

namespace CorpFileHub.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
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

            return services;
        }
    }
}