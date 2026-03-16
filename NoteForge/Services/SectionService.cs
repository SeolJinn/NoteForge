using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services;

public class SectionService : ISectionService
{
    private const string FavoritesKey = "FavoriteNotes";
    private const string FavoritesSectionId = "favorites";

    public void AddFavorite(string noteFilePath)
    {
        var favorites = GetFavorites();
        if (!favorites.Contains(noteFilePath))
        {
            favorites.Add(noteFilePath);
            SetSetting(FavoritesKey, JsonSerializer.Serialize(favorites));
        }
    }

    public void RemoveFavorite(string noteFilePath)
    {
        var favorites = GetFavorites();
        if (favorites.Remove(noteFilePath))
        {
            SetSetting(FavoritesKey, JsonSerializer.Serialize(favorites));
        }
    }

    public bool IsFavorite(string noteFilePath)
    {
        return GetFavorites().Contains(noteFilePath);
    }

    public List<string> GetFavorites()
    {
        var json = GetSetting(FavoritesKey, "[]");
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public NoteSection? GetFavoritesSection(List<Note> allNotes)
    {
        var favorites = GetFavorites();
        if (favorites.Count == 0)
        {
            return null;
        }

        var favoritesSection = new NoteSection
        {
            Id = FavoritesSectionId,
            Name = "Favorites",
            IsExpanded = true,
            IsVisible = true,
            IsBuiltIn = true
        };

        foreach (var note in allNotes.Where(n => favorites.Contains(n.FilePath)))
        {
            favoritesSection.Notes.Add(note);
        }

        return favoritesSection.Notes.Count > 0 ? favoritesSection : null;
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
