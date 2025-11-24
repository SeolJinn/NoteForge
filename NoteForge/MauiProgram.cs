using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using NoteForge.Services;
using NoteForge.Views;

namespace NoteForge;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                fonts.AddFont("Inter-Semibold.ttf", "InterSemibold");
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<INoteService, NoteService>();
        builder.Services.AddSingleton<ITabManager, TabManager>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<VaultPage>();
        builder.Services.AddTransient<CreateVaultPage>();

        return builder.Build();
    }
}