using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Controls;
using NoteForge.Handlers.Notes;
using NoteForge.Handlers.Workspace;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private async void OnVaultItemClicked(object sender, VaultInfo selectedVault)
    {
        var result = await _mediator.Send(new SwitchVaultCommandRequest(selectedVault.Path));
        if (result.Success)
        {
            await ResetWorkspaceAsync();
        }
        else
        {
            await _dialogService.ShowErrorAsync(result.ErrorMessage!, XamlRoot);
        }
    }

    private void OnManageVaultsClicked(object sender, EventArgs e)
    {
        var vaultWindow = new VaultManagerWindow();
        vaultWindow.VaultSelected += (s, path)
            => DispatcherQueue.TryEnqueue(async () => await ResetWorkspaceAsync());
        vaultWindow.Activate();
    }

    private void OnToggleSidebarClicked(object sender, RoutedEventArgs e) =>
        _sidebarCoordinator.ToggleSidebar(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);

    private void OnFolderViewClicked(object sender, RoutedEventArgs e)
    {
        GraphView.Visibility = Visibility.Collapsed;
        _sidebarCoordinator.ShowFolderView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);
    }

    private async void OnSearchViewClicked(object sender, RoutedEventArgs e)
    {
        GraphView.Visibility = Visibility.Collapsed;
        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        _sidebarCoordinator.ShowSearchView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder, allNotes);
    }

    private async void OnGraphViewClicked(object sender, RoutedEventArgs e)
    {
        EditorView.Visibility = Visibility.Collapsed;
        NewTabView.Visibility = Visibility.Collapsed;
        GraphView.Visibility = Visibility.Visible;

        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        await GraphView.LoadGraphAsync(allNotes);
    }

    private void OnGraphNodeClicked(object sender, Note note)
    {
        GraphView.Visibility = Visibility.Collapsed;

        _sidebarCoordinator.ShowFolderView(Sidebar, SidebarColumn, TitleBarSidebarColumn, SplitterBorder);

        _tabManager.OpenTab(note);
    }

    private void OnTabSelected(object sender, Tab tab)
    {
        if (GraphView.Visibility is Visibility.Visible)
        {
            GraphView.Visibility = Visibility.Collapsed;
            UpdateEditorState();
        }

        _tabManager.ActivateTab(tab);
    }

    private void OnCloseTabClicked(object sender, Tab tab) => _tabManager.CloseTab(tab);

    private void OnActiveTabChanged(object? sender, Tab? activeTab) =>
        DispatcherQueue.TryEnqueue(async () => await HandleActiveTabChangedAsync(activeTab));

    private async Task HandleActiveTabChangedAsync(Tab? activeTab)
    {
        if (_selectedNote is not null && EditorView.IsEditorReady)
        {
            try
            {
                _selectedNote.Text = await EditorView.GetContentAsync(TimeSpan.FromSeconds(1));
                await _mediator.Send(new SaveNoteCommandRequest(_selectedNote));
                _tabManager.SetDirty(_selectedNote.FilePath, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save outgoing tab content");
            }
        }

        TabBarControl.SetSelectedItem(activeTab);
        GraphView.Visibility = Visibility.Collapsed;

        if (activeTab is null or { IsNewTab: true })
        {
            Sidebar.SetSelectedNote(null);
            SetSelectedNote(null);
            if (activeTab?.IsNewTab is true) UpdateEditorState();
            return;
        }

        var loadedNote = await _mediator.Send(new GetNoteByPathQueryRequest(activeTab.FilePath));
        if (loadedNote is not null)
        {
            SetSelectedNote(loadedNote);
            Sidebar.SetSelectedNote(loadedNote);
        }
        else
            _tabManager.CloseTab(activeTab);
    }

    private void OnNoteSelected(object sender, Note selectedNote)
    {
        if (_tabManager.ActiveTab?.FilePath == selectedNote.FilePath)
            SetSelectedNote(selectedNote);
        else
            _tabManager.OpenTab(selectedNote);
    }

    private async void OnMatchingLineSelected(object sender, (Note Note, int LineNumber) args)
    {
        if (_tabManager.ActiveTab?.FilePath != args.Note.FilePath)
            _tabManager.OpenTab(args.Note);
        else
            SetSelectedNote(args.Note);

        await Task.Delay(100);
        await EditorView.NavigateToLineAsync(args.LineNumber);
    }

    private async void OnGoToFileClicked(object sender, RoutedEventArgs e)
    {
        if (_quickFileNavigator is null)
        {
            _quickFileNavigator = new QuickFileNavigator();
            _quickFileNavigator.NoteSelected += (s, note) => _tabManager.OpenTab(note);
        }

        List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
        _quickFileNavigator.Show(allNotes, XamlRoot);
    }

    private async void OnCreateNewNoteClicked(object sender, RoutedEventArgs e) => await CreateNewNote();

    private async void OnSidebarCreateNewNoteClicked(object? sender, EventArgs? e) => await CreateNewNote();

    private async Task CreateNewNote()
    {
        var newNote = await _mediator.Send(new CreateNoteCommandRequest());
        if (newNote is not null)
        {
            await LoadNotes();
            _tabManager.OpenTab(newNote);
        }
        else
        {
            Toast.Show("Failed to create a new note.");
        }
    }

    private void OnNewTabClicked(object sender, EventArgs e) => _tabManager.OpenNewTab();

    private void OnSettingsClicked(object? sender, EventArgs? e)
    {
        var settingsPopup = new SettingsPopup();
        settingsPopup.Show(XamlRoot);
    }
}
