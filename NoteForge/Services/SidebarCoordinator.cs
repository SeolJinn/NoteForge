using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Controls;
using NoteForge.Models;

namespace NoteForge.Services;

public class SidebarCoordinator
{
    private const double SidebarWidth = 250;
    private double _lastSidebarWidth = SidebarWidth;
    private bool _isCollapsed;

    public void ToggleSidebar(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder)
    {
        if (!_isCollapsed)
        {
            _lastSidebarWidth = sidebarColumn.ActualWidth > 0 ? sidebarColumn.ActualWidth : SidebarWidth;
            sidebar.Visibility = Visibility.Collapsed;
            splitterBorder.Visibility = Visibility.Collapsed;
            sidebarColumn.Width = new GridLength(0);
            titleBarColumn.Width = GridLength.Auto;
            _isCollapsed = true;
        }
        else
        {
            RestoreSidebar(sidebar, sidebarColumn, titleBarColumn, splitterBorder);
        }
    }

    public void ShowFolderView(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder)
    {
        sidebar.SetViewMode(SidebarViewMode.Folder);
        RestoreSidebar(sidebar, sidebarColumn, titleBarColumn, splitterBorder);
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
        RestoreSidebar(sidebar, sidebarColumn, titleBarColumn, splitterBorder);
    }

    private void RestoreSidebar(
        WorkspaceSidebar sidebar,
        ColumnDefinition sidebarColumn,
        ColumnDefinition titleBarColumn,
        Border splitterBorder)
    {
        sidebar.Visibility = Visibility.Visible;
        splitterBorder.Visibility = Visibility.Visible;

        if (_isCollapsed)
        {
            sidebarColumn.Width = new GridLength(_lastSidebarWidth);
            titleBarColumn.Width = new GridLength(_lastSidebarWidth);
            _isCollapsed = false;
        }
    }
}
