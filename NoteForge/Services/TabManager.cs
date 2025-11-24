using System.Collections.ObjectModel;
using NoteForge.Models;
using Tab = NoteForge.Models.Tab;

namespace NoteForge.Services;

public interface ITabManager
{
    ObservableCollection<Tab> Tabs { get; }
    Tab? ActiveTab { get; }
    event EventHandler<Tab?>? ActiveTabChanged;

    void OpenTab(Note note);
    void CloseTab(Tab tab);
    void ActivateTab(Tab tab);
    void SetDirty(string filePath, bool isDirty);
}

public class TabManager : ITabManager
{
    private Tab? _activeTab;

    public ObservableCollection<Tab> Tabs { get; } = new();

    public Tab? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                if (_activeTab != null)
                    _activeTab.IsActive = false;

                _activeTab = value;

                if (_activeTab != null)
                    _activeTab.IsActive = true;

                ActiveTabChanged?.Invoke(this, _activeTab);
            }
        }
    }

    public event EventHandler<Tab?>? ActiveTabChanged;

    public void OpenTab(Note note)
    {
        if (note == null || string.IsNullOrEmpty(note.FilePath))
            return;

        var existingTab = Tabs.FirstOrDefault(t => t.FilePath == note.FilePath);

        if (existingTab != null)
        {
            ActivateTab(existingTab);
        }
        else
        {
            var newTab = new Tab
            {
                FilePath = note.FilePath,
                DisplayName = note.Filename, // Or Path.GetFileName(note.FilePath)
                IsDirty = false,
                IsActive = false
            };

            Tabs.Add(newTab);
            ActivateTab(newTab);
        }
    }

    public void CloseTab(Tab tab)
    {
        if (!Tabs.Contains(tab)) return;

        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (tab == ActiveTab)
        {
            // Activate nearest neighbor
            if (Tabs.Count > 0)
            {
                // Try to select the one at the same index (next one that shifted left)
                // or the last one if we closed the last one.
                var nextIndex = Math.Min(index, Tabs.Count - 1);
                ActivateTab(Tabs[nextIndex]);
            }
            else
            {
                ActiveTab = null;
            }
        }
    }

    public void ActivateTab(Tab tab)
    {
        if (Tabs.Contains(tab))
        {
            ActiveTab = tab;
        }
    }

    public void SetDirty(string filePath, bool isDirty)
    {
        var tab = Tabs.FirstOrDefault(t => t.FilePath == filePath);
        if (tab != null)
        {
            tab.IsDirty = isDirty;
        }
    }
}

