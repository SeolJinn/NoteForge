namespace NoteForge.Services.Ai;

public sealed class AiAuthException(string providerName, string message, int statusCode, string? body = null)
    : AiProviderException(providerName, message, statusCode, body);
