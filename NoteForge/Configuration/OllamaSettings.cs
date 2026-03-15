using System;

namespace NoteForge.Configuration;

public static class OllamaSettings
{
    public static string OllamaUrl { get; } =
        Environment.GetEnvironmentVariable("OLLAMA_URL") ?? "http://localhost:11434";

    public static string OllamaModel { get; } =
        Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "ibm/granite4:1b-h";

    public static string EmbeddingModel { get; } =
        Environment.GetEnvironmentVariable("OLLAMA_EMBEDDING_MODEL") ?? "nomic-embed-text";
}