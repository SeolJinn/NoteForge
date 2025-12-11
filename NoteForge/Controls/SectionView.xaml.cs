using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class SectionView : UserControl
{
    public NoteSection Section { get; }

    public event EventHandler? HeaderClicked;
    public event EventHandler<Note>? NoteSelected;
    public event EventHandler<Note>? ToggleFavoriteRequested;
    public event EventHandler? RenameSectionRequested;
    public event EventHandler? DeleteSectionRequested;
    public event EventHandler<(Note Note, string TargetSectionId)>? NoteMovedToSection;

    public SectionView(NoteSection section)
    {
        Section = section;
        InitializeComponent();
    }

    public void SetSelectedNote(Note? note)
    {
        foreach (var sectionNote in Section.Notes)
        {
            sectionNote.IsSelected = note is not null && sectionNote.FilePath == note.FilePath;
        }
    }

    private void OnHeaderTapped(object sender, TappedRoutedEventArgs e)
    {
        Section.IsExpanded = !Section.IsExpanded;
        HeaderClicked?.Invoke(this, EventArgs.Empty);
    }

    private void OnHeaderPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 42, 42, 42));
        }
    }

    private void OnHeaderPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            grid.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }
    }

    private void OnContextMenuOpening(object? sender, object e)
    {
        if (Section.IsBuiltIn && sender is MenuFlyout flyout)
        {
            flyout.Hide();
        }
    }

    private void OnNoteTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is Note note)
        {
            NoteSelected?.Invoke(this, note);
        }
    }

    private void OnToggleFavoriteClicked(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is Note note)
        {
            ToggleFavoriteRequested?.Invoke(this, note);
        }
    }

    private void OnRenameSectionClicked(object sender, RoutedEventArgs e)
    {
        RenameSectionRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnDeleteSectionClicked(object sender, RoutedEventArgs e)
    {
        DeleteSectionRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnNoteDragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Border border && border.DataContext is Note note)
        {
            args.Data.Properties.Add("NoteFilePath", note.FilePath);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
    }

    private void OnNoteDragOver(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.ContainsKey("NoteFilePath"))
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.Caption = $"Move to {Section.Name}";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }
    }

    private void OnNoteDrop(object sender, DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("NoteFilePath", out var noteFilePathObj) && noteFilePathObj is string noteFilePath)
        {
            Note? note = Section.Notes.FirstOrDefault(n => n.FilePath == noteFilePath);
            if (note is null)
            {
                foreach (var section in App.SectionService.Sections)
                {
                    note = section.Notes.FirstOrDefault(n => n.FilePath == noteFilePath);
                    if (note is not null) break;
                }
            }

            if (note is not null)
            {
                NoteMovedToSection?.Invoke(this, (Note: note, TargetSectionId: Section.Id));
            }
        }
    }
}
