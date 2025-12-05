using System;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using NoteForge.Interfaces;
using NoteForge.Services;

namespace NoteForge;

public partial class App : Application
{
    public static Window MainWindow { get; private set; } = null!;
    public static IServiceProvider Services { get; private set; } = null!;
    public static INoteService NoteService => Services.GetRequiredService<INoteService>();
    public static ITabManager TabManager => Services.GetRequiredService<ITabManager>();
    public static IMarkdownPreviewService PreviewService => Services.GetRequiredService<IMarkdownPreviewService>();
    public static IDialogService DialogService => Services.GetRequiredService<IDialogService>();
    public static IMediator Mediator => Services.GetRequiredService<IMediator>();

    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<INoteService, NoteService>();
        services.AddSingleton<ITabManager, TabManager>();
        services.AddSingleton<IMarkdownPreviewService, MarkdownPreviewService>();
        services.AddSingleton<IDialogService, DialogService>();

        // Register Mediator
        services.AddMediator();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}