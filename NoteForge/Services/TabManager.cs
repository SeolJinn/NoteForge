using System;
using System.Collections.ObjectModel;
using System.Linq;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Services;

public class TabManager : ITabManager
{
    private Tab? _activeTab;

    public ObservableCollection<Tab> Tabs { get; } = [];

    public Tab? ActiveTab
    {
        get => _activeTab;
        private set
        {
            if (_activeTab != value)
            {
                if (_activeTab is not null)
                    _activeTab.IsActive = false;

                _activeTab = value;

                if (_activeTab is not null)
                    _activeTab.IsActive = true;

                ActiveTabChanged?.Invoke(this, _activeTab);
            }
        }
    }

    public event EventHandler<Tab?>? ActiveTabChanged;

    public void OpenTab(Note note)
    {
        if (note is null || string.IsNullOrEmpty(note.FilePath))
        {
            return;
        }

        var existingTab = Tabs.FirstOrDefault(t => t.FilePath == note.FilePath);

        if (existingTab is not null)
        {
            ActivateTab(existingTab);
        }
        else if (ActiveTab?.IsNewTab is true)
        {
            ActiveTab.FilePath = note.FilePath;
            ActiveTab.DisplayName = note.Filename;
            ActiveTab.IsNewTab = false;
            ActiveTab.IsDirty = false;

            ActiveTabChanged?.Invoke(this, ActiveTab);
        }
        else
        {
            var newTab = new Tab
            {
                FilePath = note.FilePath,
                DisplayName = note.Filename,
                IsDirty = false,
                IsActive = false,
                IsNewTab = false
            };

            Tabs.Add(newTab);
            ActivateTab(newTab);
        }
    }

    public Tab OpenNewTab()
    {
        var newTab = new Tab
        {
            FilePath = string.Empty,
            DisplayName = "New tab",
            IsDirty = false,
            IsActive = false,
            IsNewTab = true
        };

        Tabs.Add(newTab);
        ActivateTab(newTab);
        return newTab;
    }

    public void CloseTab(Tab tab)
    {
        if (!Tabs.Contains(tab))
        {
            return;
        }

        int index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (tab == ActiveTab)
        {
            if (Tabs.Count > 0)
            {
                var nextIndex = Math.Min(index, Tabs.Count - 1);
                ActivateTab(Tabs[nextIndex]);
            }
            else
            {
                OpenNewTab();
            }
        }
        else if (Tabs.Count == 0)
        {
            OpenNewTab();
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
        if (tab is not null)
        {
            tab.IsDirty = isDirty;
        }
    }

    public void ReorderTab(Tab tab, int newIndex)
    {
        if (tab is null || newIndex < 0 || newIndex >= Tabs.Count)
        {
            return;
        }

        int oldIndex = Tabs.IndexOf(tab);
        if (oldIndex == -1 || oldIndex == newIndex)
        { 
            return;
        }

        Tabs.Move(oldIndex, newIndex);
    }
}