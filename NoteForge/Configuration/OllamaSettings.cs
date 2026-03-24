using Windows.Storage;

namespace NoteForge.Configuration;

public static class OllamaSettings
{
    private const string OllamaUrlKey = "OllamaUrl";
    private const string OllamaModelKey = "OllamaModel";
    private const string EmbeddingModelKey = "OllamaEmbeddingModel";

    private const string DefaultUrl = "http://localhost:11434";
    private const string DefaultModel = "ibm/granite4:1b-h";
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    private static ApplicationDataContainer Settings => ApplicationData.Current.LocalSettings;

    public static string OllamaUrl
    {
        get => Settings.Values[OllamaUrlKey] as string ?? DefaultUrl;
        set => Settings.Values[OllamaUrlKey] = value;
    }

    public static string OllamaModel
    {
        get => Settings.Values[OllamaModelKey] as string ?? DefaultModel;
        set => Settings.Values[OllamaModelKey] = value;
    }

    public static string EmbeddingModel
    {
        get => Settings.Values[EmbeddingModelKey] as string ?? DefaultEmbeddingModel;
        set => Settings.Values[EmbeddingModelKey] = value;
    }
}
