using Microsoft.Extensions.Logging;
using Todo_asa.Pages;
using Todo_asa.Services;
using Todo_asa.ViewModels;

namespace Todo_asa
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("Inter-Variable.ttf", "Inter");
                });

            // Register Services
            builder.Services.AddSingleton<DatabaseService>();

            // Register ViewModels
            builder.Services.AddTransient<TaskListViewModel>();
            builder.Services.AddTransient<TaskDetailViewModel>();

            // Register Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<TaskDetailPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
