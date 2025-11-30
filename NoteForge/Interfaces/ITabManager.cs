using System;
using System.Collections.ObjectModel;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface ITabManager
{
    ObservableCollection<Tab> Tabs { get; }
    Tab? ActiveTab { get; }
    event EventHandler<Tab?>? ActiveTabChanged;

    void OpenTab(Note note);
    Tab OpenNewTab();
    void CloseTab(Tab tab);
    void ActivateTab(Tab tab);
    void SetDirty(string filePath, bool isDirty);
    void ReorderTab(Tab tab, int newIndex);
}