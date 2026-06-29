using System;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using NoteForge.Interfaces;
using NoteForge.Services;
using NoteForge.Services.Ai;
using NoteForge.Services.Embeddings;
using NoteForge.Services.Search;

namespace NoteForge;

public partial class App : Application
{
    public static Window MainWindow { get; private set; } = null!;
    public static IServiceProvider Services { get; private set; } = null!;
    public static INoteService NoteService => Services.GetRequiredService<INoteService>();
    public static ITabManager TabManager => Services.GetRequiredService<ITabManager>();
    public static IDialogService DialogService => Services.GetRequiredService<IDialogService>();
    public static IMediator Mediator => Services.GetRequiredService<IMediator>();
    public static ILoggerFactory LoggerFactory => Services.GetRequiredService<ILoggerFactory>();
    public static ISectionService SectionService => Services.GetRequiredService<ISectionService>();
    public static IFolderService FolderService => Services.GetRequiredService<IFolderService>();
    public static ISearchService SearchService => Services.GetRequiredService<ISearchService>();
    public static IEmbeddingRepository EmbeddingRepository => Services.GetRequiredService<IEmbeddingRepository>();
    public static IEmbeddingService EmbeddingService => Services.GetRequiredService<IEmbeddingService>();
    public static IAiService AiService => Services.GetRequiredService<IAiService>();

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

        services.AddSingleton<INoteService, NoteService>();
        services.AddSingleton<ITabManager, TabManager>();
        services.AddTransient<EditorInteropService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFolderDialogService, FolderDialogService>();
        services.AddSingleton<DisabledAiProvider>();
        services.AddSingleton<OllamaAiProvider>();
        services.AddSingleton<OpenAiAiProvider>(sp => new OpenAiAiProvider(new System.Net.Http.HttpClient()));
        services.AddSingleton<GeminiAiProvider>(sp => new GeminiAiProvider(new System.Net.Http.HttpClient()));
        services.AddSingleton<AiProviderRegistry>();
        services.AddSingleton<IAiService>(sp => sp.GetRequiredService<AiProviderRegistry>());
        services.AddSingleton<ISectionService, SectionService>();
        services.AddSingleton<IFolderService, FolderService>();
        services.AddSingleton<FolderTreeService>();
        services.AddSingleton<SidebarCoordinator>();
        services.AddSingleton<ISearchService, SearchService>();

        services.AddSingleton<IEmbeddingRepository, EmbeddingRepository>();
        services.AddSingleton<IEmbeddingService, EmbeddingService>();
        services.AddSingleton<ISemanticSearchStrategy, SemanticSearchStrategy>();
        services.AddSingleton<EmbeddingDebugHelper>();
        services.AddSingleton<SearchEvaluationHarness>();
        services.AddSingleton<TfidfCalculator>();
        services.AddSingleton<SubstringSearchStrategy>();

        services.AddMediator();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
