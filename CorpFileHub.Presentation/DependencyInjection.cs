using Microsoft.Extensions.DependencyInjection;

namespace CorpFileHub.Presentation
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services)
        {
            services.AddScoped<Services.ISignalRService, Services.SignalRService>();
            services.AddScoped<Services.IUIService, Services.UIService>();
            return services;
        }
    }
}
