using System;

namespace NoteForge.Services.Ai;

public class AiProviderException : Exception
{
    public string ProviderName { get; }
    public int? StatusCode { get; }
    public string? Body { get; }

    public AiProviderException(string providerName, string message, int? statusCode = null, string? body = null, Exception? innerException = null)
        : base(message, innerException)
    {
        ProviderName = providerName;
        StatusCode = statusCode;
        Body = body;
    }
}
