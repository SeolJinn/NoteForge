using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services;

public class NoteService : INoteService
{
    private const string VaultPathKey = "VaultPath";
    private const string RecentVaultsKey = "RecentVaults";
    
    public string CurrentNotebookPath { get; private set; } = string.Empty;

    public string CurrentVaultName => string.IsNullOrEmpty(CurrentNotebookPath) 
        ? string.Empty 
        : new DirectoryInfo(CurrentNotebookPath).Name;

    public bool IsConfigured => !string.IsNullOrEmpty(CurrentNotebookPath) && Directory.Exists(CurrentNotebookPath);

    public NoteService()
    {
        CurrentNotebookPath = GetSetting(VaultPathKey, string.Empty);
    }

    public async Task<string?> PickFolderAsync()
    {
        var dialogService = App.DialogService;
        var path = await dialogService.PickFolderAsync();
        
        if (!string.IsNullOrEmpty(path))
        {
            SetVaultPath(path);
            return CurrentNotebookPath;
        }

        return null;
    }

    public void SetVaultPath(string path)
    {
        CurrentNotebookPath = path;
        SetSetting(VaultPathKey, path);
        AddToRecentVaults(path);
    }

    public List<VaultInfo> GetRecentVaults()
    {
        string json = GetSetting(RecentVaultsKey, "[]");

        try
        {
            return JsonSerializer.Deserialize<List<VaultInfo>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void AddToRecentVaults(string path)
    {
        var recent = GetRecentVaults();
        
        recent.RemoveAll(v => v.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

        recent.Insert(0, new VaultInfo 
        { 
            Name = new DirectoryInfo(path).Name, 
            Path = path, 
            LastAccessed = DateTime.Now 
        });

        if (recent.Count > 5)
        {
            recent = [.. recent.Take(5)];
        }

        string json = JsonSerializer.Serialize(recent);
        SetSetting(RecentVaultsKey, json);
    }

    private static void SetSetting(string key, string value)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        localSettings.Values[key] = value;
    }

    private static string GetSetting(string key, string defaultValue)
    {
        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        return localSettings.Values.TryGetValue(key, out var value) ? (string)value : defaultValue;
    }
}