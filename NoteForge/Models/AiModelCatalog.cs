using System.Collections.Generic;
using NoteForge.Services.Ai;

namespace NoteForge.Models;

public sealed record AiChatModel(string Id, string DisplayName);

public sealed record AiEmbeddingModel(string Id, string DisplayName, int Dimension, double PricePerMillionTokens);

public static class AiModelCatalog
{
    public static IReadOnlyList<AiChatModel> ChatModels(AiProviderType provider) => provider switch
    {
        AiProviderType.Ollama => [],
        AiProviderType.OpenAi =>
        [
            new("gpt-5-nano", "GPT-5 Nano (fastest, cheapest)"),
            new("gpt-5-mini", "GPT-5 Mini (more capable)")
        ],
        AiProviderType.Gemini =>
        [
            new("gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite (fastest, cheapest)"),
            new("gemini-2.5-flash", "Gemini 2.5 Flash (more capable)")
        ],
        _ => []
    };

    public static IReadOnlyList<AiEmbeddingModel> EmbeddingModels(AiProviderType provider) => provider switch
    {
        AiProviderType.Ollama =>
        [
            new("nomic-embed-text", "nomic-embed-text (768 dim)", 768, 0.0)
        ],
        AiProviderType.OpenAi =>
        [
            new("text-embedding-3-small", "text-embedding-3-small (1536 dim)", 1536, 0.02),
            new("text-embedding-3-large", "text-embedding-3-large (3072 dim)", 3072, 0.13)
        ],
        AiProviderType.Gemini =>
        [
            new("gemini-embedding-001", "gemini-embedding-001 (1536 dim)", 1536, 0.15),
            new("gemini-embedding-2", "gemini-embedding-2 (1536 dim)", 1536, 0.20)
        ],
        _ => []
    };

    public static AiEmbeddingModel? FindEmbeddingModel(AiProviderType provider, string id)
    {
        foreach (var model in EmbeddingModels(provider))
        {
            if (model.Id == id) return model;
        }
        return null;
    }
}
