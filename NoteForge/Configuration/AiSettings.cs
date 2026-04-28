using System;
using NoteForge.Services;
using NoteForge.Services.Ai;

namespace NoteForge.Configuration;

public static class AiSettings
{
    private const string ActiveProviderKey = "AiActiveProvider";
    private const string LegacyAiEnabledKey = "AiEnabled";

    private const string OllamaUrlKey = "OllamaUrl";
    private const string OllamaChatModelKey = "OllamaModel";
    private const string OllamaEmbeddingModelKey = "OllamaEmbeddingModel";

    private const string OpenAiChatModelKey = "OpenAiChatModel";
    private const string OpenAiEmbeddingModelKey = "OpenAiEmbeddingModel";

    private const string GeminiChatModelKey = "GeminiChatModel";
    private const string GeminiEmbeddingModelKey = "GeminiEmbeddingModel";

    private const string GraphSemanticThresholdKey = "GraphSemanticThreshold";
    private const string GraphTfidfThresholdKey = "GraphTfidfThreshold";
    private const string GraphShowExplicitLinksKey = "GraphShowExplicitLinks";
    private const string GraphShowSemanticLinksKey = "GraphShowSemanticLinks";

    private const string DefaultOllamaUrl = "http://localhost:11434";
    private const string DefaultOllamaChatModel = "ibm/granite4:1b-h";
    private const string DefaultOllamaEmbeddingModel = "nomic-embed-text";
    private const string DefaultOpenAiChatModel = "gpt-5-nano";
    private const string DefaultOpenAiEmbeddingModel = "text-embedding-3-small";
    private const string DefaultGeminiChatModel = "gemini-2.5-flash-lite";
    private const string DefaultGeminiEmbeddingModel = "gemini-embedding-001";

    public static event Action? ActiveProviderChanged;

    static AiSettings()
    {
        MigrateLegacyAiEnabled();
    }

    public static bool IsAiEnabled => ActiveProvider != AiProviderType.Disabled;

    public static AiProviderType ActiveProvider
    {
        get
        {
            var raw = LocalSettingsStore.GetString(ActiveProviderKey);
            if (raw is not null && Enum.TryParse<AiProviderType>(raw, ignoreCase: true, out var parsed))
            {
                return parsed;
            }
            return AiProviderType.Disabled;
        }
        set
        {
            if (ActiveProvider != value)
            {
                LocalSettingsStore.SetString(ActiveProviderKey, value.ToString());
                ActiveProviderChanged?.Invoke();
            }
        }
    }

    public static string OllamaUrl
    {
        get => LocalSettingsStore.GetString(OllamaUrlKey) ?? DefaultOllamaUrl;
        set => LocalSettingsStore.SetString(OllamaUrlKey, value);
    }

    public static string OllamaChatModel
    {
        get => LocalSettingsStore.GetString(OllamaChatModelKey) ?? DefaultOllamaChatModel;
        set => LocalSettingsStore.SetString(OllamaChatModelKey, value);
    }

    public static string OllamaEmbeddingModel
    {
        get => LocalSettingsStore.GetString(OllamaEmbeddingModelKey) ?? DefaultOllamaEmbeddingModel;
        set => LocalSettingsStore.SetString(OllamaEmbeddingModelKey, value);
    }

    public static string OpenAiChatModel
    {
        get => LocalSettingsStore.GetString(OpenAiChatModelKey) ?? DefaultOpenAiChatModel;
        set => LocalSettingsStore.SetString(OpenAiChatModelKey, value);
    }

    public static string OpenAiEmbeddingModel
    {
        get => LocalSettingsStore.GetString(OpenAiEmbeddingModelKey) ?? DefaultOpenAiEmbeddingModel;
        set => LocalSettingsStore.SetString(OpenAiEmbeddingModelKey, value);
    }

    public static string GeminiChatModel
    {
        get => LocalSettingsStore.GetString(GeminiChatModelKey) ?? DefaultGeminiChatModel;
        set => LocalSettingsStore.SetString(GeminiChatModelKey, value);
    }

    public static string GeminiEmbeddingModel
    {
        get => LocalSettingsStore.GetString(GeminiEmbeddingModelKey) ?? DefaultGeminiEmbeddingModel;
        set => LocalSettingsStore.SetString(GeminiEmbeddingModelKey, value);
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

    internal static void MigrateLegacyAiEnabled()
    {
        if (LocalSettingsStore.GetString(ActiveProviderKey) is not null)
        {
            return;
        }

        var legacy = LocalSettingsStore.GetBool(LegacyAiEnabledKey);
        if (legacy is null)
        {
            return;
        }

        var migrated = legacy is true ? AiProviderType.Ollama : AiProviderType.Disabled;
        LocalSettingsStore.SetString(ActiveProviderKey, migrated.ToString());
        LocalSettingsStore.RemoveKey(LegacyAiEnabledKey);
    }
}
