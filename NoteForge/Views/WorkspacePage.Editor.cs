using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Handlers.AI;
using NoteForge.Handlers.Notes;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private void OnNoteTitleTextChanged(object sender, string newTitle)
    {
        if (_isLoading || _isSyncingTitle) return;

        _pendingTitleRename = newTitle;
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        EditorView.SetTitle(title);
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, EventArgs e)
    {
        if (_pendingTitleRename is not null)
        {
            await RenameCurrentNote(_pendingTitleRename);
            _pendingTitleRename = null;
        }
    }

    private async Task RenameCurrentNote(string? newTitle)
    {
        if (_selectedNote is null) return;

        var currentTitle = _selectedNote.Filename;
        newTitle = newTitle?.Trim();

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            SyncTitles(currentTitle);
            return;
        }

        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase)) return;

        var result = await _mediator.Send(new RenameNoteCommandRequest(_selectedNote, newTitle));
        if (result.Success && _tabManager.ActiveTab is not null)
        {
            _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
            _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
            await LoadNotes();
            Sidebar.SetSelectedNote(_selectedNote);
        }
        else if (!result.Success)
        {
            SyncTitles(currentTitle);
            Toast.Show(result.ErrorMessage!);
        }
    }

    private async void OnNoteContentChanged(object sender, EventArgs e)
    {
        if (_isLoading || _selectedNote is null) return;

        _tabManager.SetDirty(_selectedNote.FilePath, true);

        var noteToSave = _selectedNote;
        try
        {
            noteToSave.Text = await EditorView.GetContentAsync();
            await _mediator.Send(new SaveNoteCommandRequest(noteToSave));
            _tabManager.SetDirty(noteToSave.FilePath, false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Failed to get content for save");
        }
    }

    private async void OnEditorSaveRequested(object sender, EventArgs e)
    {
        if (_selectedNote is null) return;
        try
        {
            _selectedNote.Text = await EditorView.GetContentAsync();
            await _mediator.Send(new SaveNoteCommandRequest(_selectedNote));
            _tabManager.SetDirty(_selectedNote.FilePath, false);
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex, "Failed to save on Ctrl+S");
        }
    }

    private async void OnEditorLinkClicked(object sender, string href)
    {
        if (href.StartsWith("[[") && href.EndsWith("]]"))
        {
            var noteName = href[2..^2];
            List<Note> allNotes = [.. await _mediator.Send(new GetNotesQueryRequest())];
            var targetNote = allNotes.FirstOrDefault(n =>
                n.Filename.Equals(noteName, StringComparison.OrdinalIgnoreCase));
            if (targetNote is not null)
                _tabManager.OpenTab(targetNote);
        }
        else if (Uri.TryCreate(href, UriKind.Absolute, out var uri))
        {
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }

    private async void OnGenerateSummaryClicked(object sender, EventArgs e)
    {
        if (_selectedNote is null || string.IsNullOrWhiteSpace(_selectedNote.Text)) return;

        _summaryCts?.Cancel();
        _summaryCts = new CancellationTokenSource();

        await _mediator.Send(new GenerateInlineAiSummaryCommandRequest(_selectedNote,
            OnSummaryStarted: () => { EditorView.ShowAiSummary("Generating summary..."); EditorView.SetSummaryButtonEnabled(false); },
            OnFirstToken: () => EditorView.SetAiSummaryText(string.Empty),
            OnTokenReceived: EditorView.AppendAiSummaryText,
            OnSummaryCompleted: () => EditorView.SetSummaryButtonEnabled(true),
            OnSummaryCancelled: () => { EditorView.SetAiSummaryText("Summary generation cancelled."); EditorView.SetSummaryButtonEnabled(true); },
            OnSummaryFailed: error => { EditorView.SetAiSummaryText($"Error: {error}"); EditorView.SetSummaryButtonEnabled(true); }
        ), _summaryCts.Token);
    }

    private void OnCloseSummaryClicked(object sender, EventArgs e)
    {
        _summaryCts?.Cancel();
        EditorView.HideAiSummary();
    }
}
