using System;
using Windows.Storage;

namespace NoteForge.Configuration;

public static class OllamaSettings
{
    private const string OllamaUrlKey = "OllamaUrl";
    private const string OllamaModelKey = "OllamaModel";
    private const string EmbeddingModelKey = "OllamaEmbeddingModel";
    private const string AiEnabledKey = "AiEnabled";
    private const string GraphSemanticThresholdKey = "GraphSemanticThreshold";
    private const string GraphTfidfThresholdKey = "GraphTfidfThreshold";
    private const string GraphShowExplicitLinksKey = "GraphShowExplicitLinks";
    private const string GraphShowSemanticLinksKey = "GraphShowSemanticLinks";

    private const string DefaultUrl = "http://localhost:11434";
    private const string DefaultModel = "ibm/granite4:1b-h";
    private const string DefaultEmbeddingModel = "nomic-embed-text";

    private static ApplicationDataContainer Settings => ApplicationData.Current.LocalSettings;

    public static event Action? AiEnabledChanged;

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

    public static bool AiEnabled
    {
        get => Settings.Values[AiEnabledKey] as bool? ?? false;
        set
        {
            if (AiEnabled != value)
            {
                Settings.Values[AiEnabledKey] = value;
                AiEnabledChanged?.Invoke();
            }
        }
    }

    public static float GraphSemanticThreshold
    {
        get => Settings.Values[GraphSemanticThresholdKey] is double d ? (float)d : 0.1f;
        set => Settings.Values[GraphSemanticThresholdKey] = (double)value;
    }

    public static float GraphTfidfThreshold
    {
        get => Settings.Values[GraphTfidfThresholdKey] is double d ? (float)d : 0.1f;
        set => Settings.Values[GraphTfidfThresholdKey] = (double)value;
    }

    public static bool GraphShowExplicitLinks
    {
        get => Settings.Values[GraphShowExplicitLinksKey] as bool? ?? true;
        set => Settings.Values[GraphShowExplicitLinksKey] = value;
    }

    public static bool GraphShowSemanticLinks
    {
        get => Settings.Values[GraphShowSemanticLinksKey] as bool? ?? true;
        set => Settings.Values[GraphShowSemanticLinksKey] = value;
    }
}
