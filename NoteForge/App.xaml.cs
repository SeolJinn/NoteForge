using System;
using System.IO;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using NoteForge.Interfaces;
using NoteForge.Services;
using NoteForge.Services.Embeddings;
using NoteForge.Services.Search;

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
    public static ILoggerFactory LoggerFactory => Services.GetRequiredService<ILoggerFactory>();
    public static SectionService SectionService => Services.GetRequiredService<SectionService>();
    public static FolderService FolderService => Services.GetRequiredService<FolderService>();
    public static ISearchService SearchService => Services.GetRequiredService<ISearchService>();
    public static EmbeddingRepository? EmbeddingRepository { get; set; }
    public static EmbeddingService? EmbeddingService { get; set; }

    public App()
    {
        this.InitializeComponent();
        Services = ConfigureServices();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddDebug();
        });

        // Register services
        services.AddSingleton<INoteService, NoteService>();
        services.AddSingleton<ITabManager, TabManager>();
        services.AddSingleton<IMarkdownPreviewService, MarkdownPreviewService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFolderDialogService, FolderDialogService>();
        services.AddSingleton<OllamaService>();
        services.AddSingleton<SectionService>();
        services.AddSingleton<FolderService>();
        services.AddSingleton<FolderTreeService>();
        services.AddSingleton<SidebarCoordinator>();
        services.AddSingleton<ISearchService, SearchService>();

        // Register embedding services
        services.AddSingleton<SemanticSearchStrategy>();
        services.AddSingleton<EmbeddingDebugHelper>();
        services.AddSingleton<TfidfCalculator>();

        // Register Mediator
        services.AddMediator();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}