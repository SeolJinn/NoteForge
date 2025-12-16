using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Controls;
using NoteForge.Models;

namespace NoteForge.Services;

public class SidebarCoordinator
{
    public void ToggleSidebar(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder)
    {
        if (sidebar.Visibility is Visibility.Visible)
        {
            sidebar.Visibility = Visibility.Collapsed;
            splitterBorder.Visibility = Visibility.Collapsed;
            sidebarColumn.Width = new GridLength(0);
            titleBarColumn.Width = GridLength.Auto;
        }
        else
        {
            sidebar.Visibility = Visibility.Visible;
            splitterBorder.Visibility = Visibility.Visible;
            sidebarColumn.Width = new GridLength(250);
            titleBarColumn.Width = new GridLength(250);
        }
    }

    public void ShowFolderView(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder)
    {
        sidebar.Visibility = Visibility.Visible;
        splitterBorder.Visibility = Visibility.Visible;
        sidebar.SetViewMode(SidebarViewMode.Folder);

        if (sidebarColumn.ActualWidth is 0)
        {
            sidebarColumn.Width = new GridLength(250);
            titleBarColumn.Width = new GridLength(250);
        }
    }

    public void ShowSearchView(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder,
        List<Note> allNotes)
    {
        sidebar.LoadNotesForSearch(allNotes);
        sidebar.SetViewMode(SidebarViewMode.Search);
        sidebar.Visibility = Visibility.Visible;
        splitterBorder.Visibility = Visibility.Visible;

        if (sidebarColumn.ActualWidth is 0)
        {
            sidebarColumn.Width = new GridLength(250);
            titleBarColumn.Width = new GridLength(250);
        }
    }
}