using System.Linq;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Handlers.Folders;
using NoteForge.Handlers.Notes;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private async void OnCreateFolderClicked(object sender, Folder? folder)
    {
        var folderName = await _folderDialogService.ShowCreateFolderDialogAsync(XamlRoot);
        if (folderName is not null)
        {
            var folderPath = folder?.DirectoryPath ?? _noteService.CurrentNotebookPath;
            var result = await _mediator.Send(new CreateFolderCommandRequest(folderPath, folderName));
            if (result.Success)
                await LoadNotes();
            else
                Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnRenameFolderClicked(object sender, (Folder Folder, string NewName) data)
    {
        var result = await _mediator.Send(new RenameFolderCommandRequest(data.Folder.DirectoryPath, data.NewName));
        if (result.Success)
            await LoadNotes();
        else
            Toast.Show(result.ErrorMessage!);
    }

    private async void OnDeleteFolderClicked(object sender, Folder folder)
    {
        if (!folder.IsEmpty)
        {
            Toast.Show("Cannot delete a non-empty folder.");
            return;
        }

        var confirmed = await _folderDialogService.ShowDeleteFolderDialogAsync(folder.Name, XamlRoot);
        if (confirmed)
        {
            var result = await _mediator.Send(new DeleteFolderCommandRequest(folder.DirectoryPath));
            if (result.Success)
                await LoadNotes();
            else
                Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnRenameNoteClicked(object sender, (Note Note, string NewName) data)
    {
        var oldPath = data.Note.FilePath;
        var result = await _mediator.Send(new RenameNoteCommandRequest(data.Note, data.NewName));
        if (result.Success)
        {
            var tab = _tabManager.Tabs.FirstOrDefault(t => t.FilePath == oldPath);
            if (tab is not null)
            {
                tab.DisplayName = data.Note.Filename;
                tab.FilePath = data.Note.FilePath;
            }

            await LoadNotes();
        }
        else
        {
            Toast.Show(result.ErrorMessage!);
        }

        if (_selectedNote is not null)
            Sidebar.SetSelectedNote(_selectedNote);
    }

    private async void OnDeleteNoteClicked(object sender, Note note)
    {
        var confirmed = await _folderDialogService.ShowDeleteNoteDialogAsync(note.Filename, XamlRoot);
        if (!confirmed) return;

        var result = await _mediator.Send(new DeleteNoteCommandRequest(note));
        if (result.Success)
        {
            if (_selectedNote?.FilePath == note.FilePath)
                SetSelectedNote(null);

            var tabToClose = _tabManager.Tabs.FirstOrDefault(t => t.FilePath == note.FilePath);
            if (tabToClose is not null)
                _tabManager.CloseTab(tabToClose);

            await LoadNotes();
        }
        else
        {
            Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnNoteMovedToFolder(object sender, (Note Note, Folder TargetFolder) e)
    {
        var wasSelected = _selectedNote?.FilePath == e.Note.FilePath;
        var expandedStates = Sidebar.GetFolderExpandedStates();

        var result = await _mediator.Send(new MoveNoteToFolderCommandRequest(e.Note, e.TargetFolder.DirectoryPath));
        if (!result.Success)
        {
            Toast.Show(result.ErrorMessage!);
            return;
        }

        await LoadNotes();

        Sidebar.RestoreFolderExpandedStates(expandedStates);

        if (wasSelected && _selectedNote is not null)
        {
            Sidebar.SetSelectedNote(_selectedNote);
        }
    }

    private async void OnToggleFavoriteClicked(object sender, Note note)
    {
        await _mediator.Send(new ToggleFavoriteCommandRequest(note));
        await LoadNotes();
    }
}
