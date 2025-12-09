using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using NoteForge.Models;

namespace NoteForge.Services;

public class SectionService
{
    private const string SectionsKey = "NoteSections";
    private const string FavoritesKey = "FavoriteNotes";
    private const string NoteAssignmentsKey = "NoteAssignments";
    private const string SectionOrderKey = "SectionOrder";
    private const string FavoritesSectionId = "favorites";
    private const string AllNotesSectionId = "all-notes";

    public ObservableCollection<NoteSection> Sections { get; } = [];
    private Dictionary<string, string> _noteAssignments = [];

    public SectionService()
    {
        LoadSections();
        LoadNoteAssignments();
    }

    public void AddSection(string name)
    {
        var section = new NoteSection
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            IsExpanded = true,
            IsVisible = true,
            IsBuiltIn = false
        };

        Sections.Add(section);
        SaveSections();
    }

    public void RemoveSection(string sectionId)
    {
        var section = Sections.FirstOrDefault(s => s.Id == sectionId && !s.IsBuiltIn);
        if (section is not null)
        {
            Sections.Remove(section);

            var assignmentsToRemove = _noteAssignments
                .Where(kv => kv.Value == sectionId)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var noteFilePath in assignmentsToRemove)
            {
                _noteAssignments.Remove(noteFilePath);
            }

            SaveSections();
            if (assignmentsToRemove.Count > 0)
            {
                SaveNoteAssignments();
            }
        }
    }

    public void RenameSection(string sectionId, string newName)
    {
        var section = Sections.FirstOrDefault(s => s.Id == sectionId);
        if (section is not null && !section.IsBuiltIn)
        {
            section.Name = newName;
            SaveSections();
        }
    }

    public void ToggleSectionExpanded(string sectionId)
    {
        var section = Sections.FirstOrDefault(s => s.Id == sectionId);
        if (section is not null)
        {
            section.IsExpanded = !section.IsExpanded;
            SaveSections();
        }
    }

    public void AddFavorite(string noteFilePath)
    {
        var favorites = GetFavorites();
        if (!favorites.Contains(noteFilePath))
        {
            favorites.Add(noteFilePath);
            SetSetting(FavoritesKey, JsonSerializer.Serialize(favorites));
            UpdateFavoritesSectionVisibility();
        }
    }

    public void RemoveFavorite(string noteFilePath)
    {
        var favorites = GetFavorites();
        if (favorites.Remove(noteFilePath))
        {
            SetSetting(FavoritesKey, JsonSerializer.Serialize(favorites));
            UpdateFavoritesSectionVisibility();
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

    public void UpdateFavoritesSectionVisibility()
    {
        var favoritesSection = Sections.FirstOrDefault(s => s.Id == FavoritesSectionId);
        if (favoritesSection is not null)
        {
            favoritesSection.IsVisible = GetFavorites().Count > 0;
        }
    }

    public void OrganizeNotesIntoSections(List<Note> allNotes)
    {
        var favorites = GetFavorites();
        var favoritesSection = Sections.FirstOrDefault(s => s.Id == FavoritesSectionId);
        var allNotesSection = Sections.FirstOrDefault(s => s.Id == AllNotesSectionId);

        foreach (var section in Sections.Where(s => !s.IsBuiltIn))
        {
            section.Notes.Clear();
        }

        var assignedNotes = new HashSet<string>();

        foreach (var (noteFilePath, sectionId) in _noteAssignments)
        {
            var section = Sections.FirstOrDefault(s => s.Id == sectionId);
            var note = allNotes.FirstOrDefault(n => n.FilePath == noteFilePath);
            if (section is not null && note is not null && !section.IsBuiltIn)
            {
                section.Notes.Add(note);
                assignedNotes.Add(noteFilePath);
            }
        }

        if (favoritesSection is not null)
        {
            favoritesSection.Notes.Clear();
            foreach (var note in allNotes.Where(n => favorites.Contains(n.FilePath)))
            {
                favoritesSection.Notes.Add(note);
            }
            favoritesSection.IsVisible = favoritesSection.Notes.Count > 0;
        }

        if (allNotesSection is not null)
        {
            allNotesSection.Notes.Clear();
            foreach (var note in allNotes.Where(n => !assignedNotes.Contains(n.FilePath)))
            {
                allNotesSection.Notes.Add(note);
            }
        }
    }

    public void AssignNoteToSection(string noteFilePath, string sectionId)
    {
        if (sectionId == FavoritesSectionId)
        {
            AddFavorite(noteFilePath);
            return;
        }

        if (sectionId == AllNotesSectionId)
        {
            UnassignNote(noteFilePath);
            return;
        }

        _noteAssignments[noteFilePath] = sectionId;
        SaveNoteAssignments();
    }

    public void UnassignNote(string noteFilePath)
    {
        if (_noteAssignments.Remove(noteFilePath))
        {
            SaveNoteAssignments();
        }
    }

    public string? GetNoteSectionId(string noteFilePath)
    {
        return _noteAssignments.TryGetValue(noteFilePath, out var sectionId) ? sectionId : null;
    }

    public void ReorderSection(string draggedSectionId, string targetSectionId, bool insertAfter)
    {
        var draggedSection = Sections.FirstOrDefault(s => s.Id == draggedSectionId);
        var targetSection = Sections.FirstOrDefault(s => s.Id == targetSectionId);

        if (draggedSection is null || targetSection is null)
        {
            return;
        }

        var draggedIndex = Sections.IndexOf(draggedSection);
        var targetIndex = Sections.IndexOf(targetSection);

        if (draggedIndex == -1 || targetIndex == -1)
        {
            return;
        }

        Sections.RemoveAt(draggedIndex);

        if (draggedIndex < targetIndex)
        {
            targetIndex--;
        }

        if (insertAfter)
        {
            targetIndex++;
        }

        Sections.Insert(targetIndex, draggedSection);
        SaveSections();
    }

    private void LoadSections()
    {
        Sections.Clear();

        var customSectionsDict = new Dictionary<string, SavedSection>();
        var json = GetSetting(SectionsKey, "[]");
        try
        {
            var savedSections = JsonSerializer.Deserialize<List<SavedSection>>(json) ?? [];
            foreach (var saved in savedSections)
            {
                customSectionsDict[saved.Id] = saved;
            }
        }
        catch { }

        var orderJson = GetSetting(SectionOrderKey, "[]");
        List<string> sectionOrder;
        try
        {
            sectionOrder = JsonSerializer.Deserialize<List<string>>(orderJson) ?? [];
        }
        catch
        {
            sectionOrder = [];
        }

        if (sectionOrder.Count == 0)
        {
            sectionOrder = [FavoritesSectionId, .. customSectionsDict.Keys, AllNotesSectionId];
        }

        foreach (var sectionId in sectionOrder)
        {
            if (sectionId == FavoritesSectionId)
            {
                Sections.Add(new NoteSection
                {
                    Id = FavoritesSectionId,
                    Name = "Favorites",
                    IsExpanded = true,
                    IsVisible = false,
                    IsBuiltIn = true
                });
            }
            else if (sectionId == AllNotesSectionId)
            {
                Sections.Add(new NoteSection
                {
                    Id = AllNotesSectionId,
                    Name = "All Notes",
                    IsExpanded = true,
                    IsVisible = true,
                    IsBuiltIn = true
                });
            }
            else if (customSectionsDict.TryGetValue(sectionId, out var saved))
            {
                Sections.Add(new NoteSection
                {
                    Id = saved.Id,
                    Name = saved.Name,
                    IsExpanded = saved.IsExpanded,
                    IsVisible = true,
                    IsBuiltIn = false
                });
                customSectionsDict.Remove(sectionId);
            }
        }

        foreach (var saved in customSectionsDict.Values)
        {
            Sections.Add(new NoteSection
            {
                Id = saved.Id,
                Name = saved.Name,
                IsExpanded = saved.IsExpanded,
                IsVisible = true,
                IsBuiltIn = false
            });
        }

        UpdateFavoritesSectionVisibility();
    }

    private void SaveSections()
    {
        var toSave = Sections
            .Where(s => !s.IsBuiltIn)
            .Select(s => new SavedSection
            {
                Id = s.Id,
                Name = s.Name,
                IsExpanded = s.IsExpanded
            })
            .ToList();

        var json = JsonSerializer.Serialize(toSave);
        SetSetting(SectionsKey, json);

        var sectionOrder = Sections.Select(s => s.Id).ToList();
        var orderJson = JsonSerializer.Serialize(sectionOrder);
        SetSetting(SectionOrderKey, orderJson);
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

    private void LoadNoteAssignments()
    {
        var json = GetSetting(NoteAssignmentsKey, "{}");
        try
        {
            _noteAssignments = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch
        {
            _noteAssignments = [];
        }
    }

    private void SaveNoteAssignments()
    {
        var json = JsonSerializer.Serialize(_noteAssignments);
        SetSetting(NoteAssignmentsKey, json);
    }

    private class SavedSection
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsExpanded { get; set; }
    }
}
