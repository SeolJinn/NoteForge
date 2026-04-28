using NoteForge.Configuration;
using NoteForge.Services;
using NoteForge.Services.Ai;

namespace NoteForge.Tests;

[Collection("Settings")]
public class AiSettingsMigrationTests
{
    [Fact]
    public void Migrates_AiEnabled_true_to_Ollama()
    {
        LocalSettingsStore.RemoveKey("AiActiveProvider");
        LocalSettingsStore.SetBool("AiEnabled", true);

        AiSettings.MigrateLegacyAiEnabled();

        Assert.Equal(AiProviderType.Ollama, AiSettings.ActiveProvider);
        Assert.Null(LocalSettingsStore.GetBool("AiEnabled"));
    }

    [Fact]
    public void Migrates_AiEnabled_false_to_Disabled()
    {
        LocalSettingsStore.RemoveKey("AiActiveProvider");
        LocalSettingsStore.SetBool("AiEnabled", false);

        AiSettings.MigrateLegacyAiEnabled();

        Assert.Equal(AiProviderType.Disabled, AiSettings.ActiveProvider);
        Assert.Null(LocalSettingsStore.GetBool("AiEnabled"));
    }

    [Fact]
    public void Migration_is_idempotent()
    {
        LocalSettingsStore.RemoveKey("AiActiveProvider");
        LocalSettingsStore.SetBool("AiEnabled", true);

        AiSettings.MigrateLegacyAiEnabled();
        AiSettings.MigrateLegacyAiEnabled();

        Assert.Equal(AiProviderType.Ollama, AiSettings.ActiveProvider);
    }

    [Fact]
    public void Migration_skipped_when_ActiveProvider_already_set()
    {
        AiSettings.ActiveProvider = AiProviderType.Gemini;
        LocalSettingsStore.SetBool("AiEnabled", true);

        AiSettings.MigrateLegacyAiEnabled();

        Assert.Equal(AiProviderType.Gemini, AiSettings.ActiveProvider);
        Assert.Equal(true, LocalSettingsStore.GetBool("AiEnabled"));
        LocalSettingsStore.RemoveKey("AiEnabled");
    }

    [Fact]
    public void ActiveProvider_invalid_string_falls_back_to_Disabled()
    {
        LocalSettingsStore.SetString("AiActiveProvider", "ThisDoesNotExist");

        Assert.Equal(AiProviderType.Disabled, AiSettings.ActiveProvider);
    }

    [Fact]
    public void ActiveProvider_parses_case_insensitively()
    {
        LocalSettingsStore.SetString("AiActiveProvider", "openai");

        Assert.Equal(AiProviderType.OpenAi, AiSettings.ActiveProvider);
    }

    [Fact]
    public void IsAiEnabled_false_when_Disabled_true_otherwise()
    {
        AiSettings.ActiveProvider = AiProviderType.Disabled;
        Assert.False(AiSettings.IsAiEnabled);

        AiSettings.ActiveProvider = AiProviderType.Ollama;
        Assert.True(AiSettings.IsAiEnabled);

        AiSettings.ActiveProvider = AiProviderType.OpenAi;
        Assert.True(AiSettings.IsAiEnabled);

        AiSettings.ActiveProvider = AiProviderType.Gemini;
        Assert.True(AiSettings.IsAiEnabled);
    }
}
