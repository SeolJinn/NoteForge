using System;
using Windows.Security.Credentials;

namespace NoteForge.Services.Ai;

public static class ApiKeyVault
{
    private const string ResourcePrefix = "NoteForge.AiProvider.";

    public static void Save(AiProviderType provider, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty.", nameof(apiKey));
        }

        var vault = new PasswordVault();
        var resource = ResourcePrefix + provider;

        try
        {
            foreach (var existing in vault.FindAllByResource(resource))
            {
                vault.Remove(existing);
            }
        }
        catch (Exception)
        {
        }

        vault.Add(new PasswordCredential(resource, provider.ToString(), apiKey));
    }

    public static string? TryGet(AiProviderType provider)
    {
        var vault = new PasswordVault();
        var resource = ResourcePrefix + provider;

        try
        {
            var credential = vault.Retrieve(resource, provider.ToString());
            credential.RetrievePassword();
            return credential.Password;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void Delete(AiProviderType provider)
    {
        var vault = new PasswordVault();
        var resource = ResourcePrefix + provider;

        try
        {
            foreach (var existing in vault.FindAllByResource(resource))
            {
                vault.Remove(existing);
            }
        }
        catch (Exception)
        {
        }
    }

    public static bool HasKey(AiProviderType provider) => TryGet(provider) is not null;
}
