using System;
using NoteForge.Services;

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

    public static event Action? AiEnabledChanged;

    public static string OllamaUrl
    {
        get => LocalSettingsStore.GetString(OllamaUrlKey) ?? DefaultUrl;
        set => LocalSettingsStore.SetString(OllamaUrlKey, value);
    }

    public static string OllamaModel
    {
        get => LocalSettingsStore.GetString(OllamaModelKey) ?? DefaultModel;
        set => LocalSettingsStore.SetString(OllamaModelKey, value);
    }

    public static string EmbeddingModel
    {
        get => LocalSettingsStore.GetString(EmbeddingModelKey) ?? DefaultEmbeddingModel;
        set => LocalSettingsStore.SetString(EmbeddingModelKey, value);
    }

    public static bool AiEnabled
    {
        get => LocalSettingsStore.GetBool(AiEnabledKey) ?? false;
        set
        {
            if (AiEnabled != value)
            {
                LocalSettingsStore.SetBool(AiEnabledKey, value);
                AiEnabledChanged?.Invoke();
            }
        }
    }

    public static float GraphSemanticThreshold
    {
        get => (float)(LocalSettingsStore.GetDouble(GraphSemanticThresholdKey) ?? 0.1);
        set => LocalSettingsStore.SetDouble(GraphSemanticThresholdKey, value);
    }

    public static float GraphTfidfThreshold
    {
        get => (float)(LocalSettingsStore.GetDouble(GraphTfidfThresholdKey) ?? 0.1);
        set => LocalSettingsStore.SetDouble(GraphTfidfThresholdKey, value);
    }

    public static bool GraphShowExplicitLinks
    {
        get => LocalSettingsStore.GetBool(GraphShowExplicitLinksKey) ?? true;
        set => LocalSettingsStore.SetBool(GraphShowExplicitLinksKey, value);
    }

    public static bool GraphShowSemanticLinks
    {
        get => LocalSettingsStore.GetBool(GraphShowSemanticLinksKey) ?? true;
        set => LocalSettingsStore.SetBool(GraphShowSemanticLinksKey, value);
    }
}
