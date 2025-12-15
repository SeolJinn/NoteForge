using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class FolderView : UserControl
{
    public Folder Folder { get; }
    public Folder? RootFolder { get; set; }

    public event EventHandler? HeaderClicked;
    public event EventHandler<Note>? NoteSelected;
    public event EventHandler<Folder>? CreateSubfolderRequested;
    public event EventHandler<Folder>? RenameFolderRequested;
    public event EventHandler<Folder>? DeleteFolderRequested;
    public event EventHandler<(Note Note, Folder TargetFolder)>? NoteMovedToFolder;
    public event EventHandler<Note>? ToggleFavoriteRequested;

    public FolderView(Folder folder, Folder? rootFolder = null)
    {
        Folder = folder;
        RootFolder = rootFolder;
        InitializeComponent();
        LoadSubfolders();
    }

    private void LoadSubfolders()
    {
        SubfoldersControl.Children.Clear();

        foreach (var subfolder in Folder.SubFolders)
        {
            var subfolderView = new FolderView(subfolder, RootFolder);
            subfolderView.NoteSelected += (s, note) => NoteSelected?.Invoke(this, note);
            subfolderView.CreateSubfolderRequested += (s, f) => CreateSubfolderRequested?.Invoke(this, f);
            subfolderView.RenameFolderRequested += (s, f) => RenameFolderRequested?.Invoke(this, f);
            subfolderView.DeleteFolderRequested += (s, f) => DeleteFolderRequested?.Invoke(this, f);
            subfolderView.NoteMovedToFolder += (s, e) => NoteMovedToFolder?.Invoke(this, e);
            subfolderView.ToggleFavoriteRequested += (s, note) => ToggleFavoriteRequested?.Invoke(this, note);

            SubfoldersControl.Children.Add(subfolderView);
        }
    }

    public void SetSelectedNote(Note? note)
    {
        foreach (var folderNote in Folder.Notes)
        {
            folderNote.IsSelected = note is not null && folderNote.FilePath == note.FilePath;
        }

        foreach (var subfolder in Folder.SubFolders)
        {
            var subfolderView = FindSubfolderView(subfolder);
            subfolderView?.SetSelectedNote(note);
        }
    }

    private FolderView? FindSubfolderView(Folder subfolder)
    {
        if (SubfoldersControl is null)
        {
            return null;
        }

        foreach (var child in SubfoldersControl.Children)
        {
            if (child is FolderView folderView && folderView.Folder == subfolder)
            {
                return folderView;
            }
        }

        return null;
    }

    private void OnHeaderTapped(object sender, TappedRoutedEventArgs e)
    {
        Folder.IsExpanded = !Folder.IsExpanded;
        HeaderClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnHeaderPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 42, 42, 42));
        }
    }

    private void OnHeaderPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void OnNoteTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Note note)
        {
            NoteSelected?.Invoke(this, note);
        }
    }

    private void OnNoteDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Border border && border.DataContext is Note note)
        {
            args.Data.Properties.Add("NoteFilePath", note.FilePath);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
    }

    private void OnFolderDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("NoteFilePath"))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {Folder.Name}";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            DropZoneOverlay.Visibility = Visibility.Visible;
            e.Handled = true;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    private void OnFolderDrop(object sender, DragEventArgs e)
    {
        DropZoneOverlay.Visibility = Visibility.Collapsed;

        if (e.DataView.Properties.TryGetValue("NoteFilePath", out var noteFilePathObj) && noteFilePathObj is string noteFilePath)
        {
            Note? note = RootFolder is not null
                ? FindNoteInTree(RootFolder, noteFilePath)
                : FindNoteInTree(Folder, noteFilePath);

            if (note is not null)
            {
                NoteMovedToFolder?.Invoke(this, (Note: note, TargetFolder: Folder));
                e.Handled = true;
            }
        }
    }

    private Note? FindNoteInTree(Folder folder, string noteFilePath)
    {
        var note = folder.Notes.FirstOrDefault(n => n.FilePath == noteFilePath);
        if (note is not null)
        {
            return note;
        }

        foreach (var subfolder in folder.SubFolders)
        {
            note = FindNoteInTree(subfolder, noteFilePath);
            if (note is not null)
            {
                return note;
            }
        }

        return null;
    }

    private void OnCreateSubfolderClicked(object sender, RoutedEventArgs e)
    {
        CreateSubfolderRequested?.Invoke(this, Folder);
    }

    private void OnRenameFolderClicked(object sender, RoutedEventArgs e)
    {
        RenameFolderRequested?.Invoke(this, Folder);
    }

    private void OnDeleteFolderClicked(object sender, RoutedEventArgs e)
    {
        DeleteFolderRequested?.Invoke(this, Folder);
    }

    private void OnToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Note note)
        {
            ToggleFavoriteRequested?.Invoke(this, note);
        }
    }

    private void OnFolderDragLeave(object sender, DragEventArgs e)
    {
        DropZoneOverlay.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }
}
