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
    public event EventHandler<(Note Note, string NewName)>? RenameNoteRequested;
    public event EventHandler<Note>? DeleteNoteRequested;

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
            subfolderView.RenameNoteRequested += (s, data) => RenameNoteRequested?.Invoke(this, data);
            subfolderView.DeleteNoteRequested += (s, note) => DeleteNoteRequested?.Invoke(this, note);

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

    private void OnRenameNoteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Note note)
        {
            StartInlineRename(note);
        }
    }

    private void StartInlineRename(Note note)
    {
        var border = FindNoteBorder(note);
        if (border is null) return;

        var textBlock = border.Child as TextBlock;
        if (textBlock is null) return;

        var originalName = note.Filename;
        var originalPadding = border.Padding;
        var borderWidth = 1.5;

        border.BorderBrush = (Brush)Application.Current.Resources["Primary"];
        border.BorderThickness = new Thickness(borderWidth);
        border.Padding = new Thickness(
            originalPadding.Left - borderWidth,
            originalPadding.Top - borderWidth,
            originalPadding.Right - borderWidth,
            originalPadding.Bottom - borderWidth);

        var textBox = new TextBox
        {
            Text = System.IO.Path.GetFileNameWithoutExtension(originalName),
            FontSize = 14,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        textBox.Resources["TextControlThemeMinHeight"] = 0d;
        textBox.Resources["TextControlThemePadding"] = new Thickness(0);
        textBox.Resources["DeleteButtonVisibility"] = Visibility.Collapsed;
        textBox.Resources["TextControlBackgroundPointerOver"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBackgroundFocused"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBorderBrushFocused"] = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

        border.Child = textBox;

        var committed = false;

        void RestoreBorder()
        {
            border.Child = textBlock;
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
            border.Padding = originalPadding;
        }

        void CommitRename()
        {
            if (committed) return;
            committed = true;

            var newName = textBox.Text?.Trim();
            RestoreBorder();

            if (!string.IsNullOrWhiteSpace(newName) && newName != System.IO.Path.GetFileNameWithoutExtension(originalName))
                RenameNoteRequested?.Invoke(this, (note, newName));
            else if (note.IsSelected)
                NoteSelected?.Invoke(this, note);
        }

        void CancelRename()
        {
            if (committed) return;
            committed = true;
            RestoreBorder();

            if (note.IsSelected)
                NoteSelected?.Invoke(this, note);
        }

        textBox.LostFocus += (s, ev) => CommitRename();
        textBox.PreviewKeyDown += (s, ev) =>
        {
            if (ev.Key is Windows.System.VirtualKey.Enter)
            {
                CommitRename();
                ev.Handled = true;
            }
            else if (ev.Key is Windows.System.VirtualKey.Escape)
            {
                CancelRename();
                ev.Handled = true;
            }
        };

        textBox.SelectAll();
        textBox.Focus(FocusState.Programmatic);
    }

    private Border? FindNoteBorder(Note note)
    {
        var itemsControl = FindItemsControl();
        if (itemsControl is null) return null;

        foreach (var item in itemsControl.Items)
        {
            if (item is Note n && n.FilePath == note.FilePath)
            {
                var container = itemsControl.ContainerFromItem(item);
                if (container is not null)
                    return FindChildBorder(container);
            }
        }
        return null;
    }

    private ItemsControl? FindItemsControl()
    {
        return FindChild<ItemsControl>(this);
    }

    private static Border? FindChildBorder(DependencyObject parent)
    {
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is Border b && b.DataContext is Note)
                return b;
            var found = FindChildBorder(child);
            if (found is not null) return found;
        }
        return null;
    }

    private static T? FindChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindChild<T>(child);
            if (found is not null) return found;
        }
        return null;
    }

    private void OnDeleteNoteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Note note)
        {
            DeleteNoteRequested?.Invoke(this, note);
        }
    }

    private void OnFolderDragLeave(object sender, DragEventArgs e)
    {
        DropZoneOverlay.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }
}
