using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace NoteForge.Services;

public static class LocalSettingsStore
{
    private static readonly object Sync = new();
    private static readonly string SettingsFilePath = ResolveSettingsFilePath();
    private static Dictionary<string, JsonElement>? _cache;

    public static string? GetString(string key) =>
        TryGet(key, out var element) && element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    public static bool? GetBool(string key)
    {
        if (!TryGet(key, out var element))
            return null;
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    public static double? GetDouble(string key) =>
        TryGet(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out var d) ? d : null;

    public static void SetString(string key, string value) => Set(key, JsonSerializer.SerializeToElement(value));

    public static void SetBool(string key, bool value) => Set(key, JsonSerializer.SerializeToElement(value));

    public static void SetDouble(string key, double value) => Set(key, JsonSerializer.SerializeToElement(value));

    private static bool TryGet(string key, out JsonElement element)
    {
        lock (Sync)
        {
            EnsureLoaded();
            return _cache!.TryGetValue(key, out element);
        }
    }

    private static void Set(string key, JsonElement element)
    {
        lock (Sync)
        {
            EnsureLoaded();
            _cache![key] = element;
            Save();
        }
    }

    private static void EnsureLoaded()
    {
        if (_cache is not null)
            return;

        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json) ?? [];
                return;
            }
        }
        catch
        {
        }

        _cache = [];
    }

    private static void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
        }
    }

    private static string ResolveSettingsFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "NoteForge", "settings.json");
    }
}
