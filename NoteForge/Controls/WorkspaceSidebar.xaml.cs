using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NoteForge.Configuration;
using NoteForge.Interfaces;
using NoteForge.Models;
using NoteForge.Services;
using NoteForge.Services.Search;

namespace NoteForge.Controls;

public enum SidebarViewMode
{
    Folder,
    Search
}

public sealed partial class WorkspaceSidebar : UserControl
{
    public event EventHandler<Note>? NoteSelected;
    public event EventHandler<(Note Note, int LineNumber)>? MatchingLineSelected;
    public event EventHandler<VaultInfo>? VaultSelected;
    public event EventHandler? ManageVaultsRequested;
    public event EventHandler? CreateNoteRequested;
    public event EventHandler<Folder?>? CreateFolderRequested;
    public event EventHandler<(Folder Folder, string NewName)>? RenameFolderRequested;
    public event EventHandler<Folder>? DeleteFolderRequested;
    public event EventHandler<(Note Note, Folder TargetFolder)>? NoteMovedToFolder;
    public event EventHandler? SettingsRequested;
    public event EventHandler<Note>? ToggleFavoriteRequested;
    public event EventHandler<(Note Note, string NewName)>? RenameNoteRequested;
    public event EventHandler<Note>? DeleteNoteRequested;

    private readonly FolderTreeService _folderTreeService;
    private readonly ISemanticSearchStrategy _semanticSearch;
    private readonly SubstringSearchStrategy _substringSearch;
    private ISearchStrategy<IEnumerable<Note>, SearchResult> _activeSearch;
    private Folder? _rootFolder;
    private SectionView? _favoritesView;
    private List<Note> _allNotes = [];
    private SidebarViewMode _currentMode = SidebarViewMode.Folder;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _searchDebounceTimer;
    private CancellationTokenSource? _searchInFlightCts;
    private string _pendingSearchQuery = string.Empty;
    private static readonly TimeSpan SearchDebounce = TimeSpan.FromMilliseconds(300);

    public WorkspaceSidebar()
    {
        InitializeComponent();
        _folderTreeService = App.Services.GetRequiredService<FolderTreeService>();
        _semanticSearch = App.Services.GetRequiredService<ISemanticSearchStrategy>();
        _substringSearch = App.Services.GetRequiredService<SubstringSearchStrategy>();
        _activeSearch = AiSettings.IsAiEnabled ? _semanticSearch : _substringSearch;
        AiSettings.ActiveProviderChanged += OnAiEnabledChanged;
        Unloaded += (_, _) => AiSettings.ActiveProviderChanged -= OnAiEnabledChanged;
    }

    private void OnAiEnabledChanged()
    {
        var embeddingService = App.Services.GetRequiredService<IEmbeddingService>();

        if (AiSettings.IsAiEnabled)
        {
            _ = embeddingService.StartBackgroundGenerationAsync(_allNotes);
        }
        else
        {
            embeddingService.CancelGeneration();
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            _activeSearch = AiSettings.IsAiEnabled ? _semanticSearch : _substringSearch;
        });
    }

    public void LoadFolders(Folder rootFolder, NoteSection? favoritesSection)
    {
        _rootFolder = rootFolder;
        FoldersContainer.Children.Clear();

        if (favoritesSection is not null && favoritesSection.Notes.Count > 0)
        {
            _favoritesView = new SectionView(favoritesSection);
            _favoritesView.NoteSelected += (s, note) => NoteSelected?.Invoke(this, note);
            _favoritesView.ToggleFavoriteRequested += (s, note) => ToggleFavoriteRequested?.Invoke(this, note);
            FoldersContainer.Children.Add(_favoritesView);
        }

        foreach (var folder in rootFolder.SubFolders)
        {
            var folderView = CreateFolderView(folder);
            FoldersContainer.Children.Add(folderView);
        }

        foreach (var note in rootFolder.Notes)
        {
            var noteItem = CreateRootNoteItem(note);
            FoldersContainer.Children.Add(noteItem);
        }
    }

    private FolderView CreateFolderView(Folder folder)
    {
        var folderView = new FolderView(folder, _rootFolder!);
        folderView.NoteSelected += (s, note) => NoteSelected?.Invoke(this, note);
        folderView.CreateSubfolderRequested += (s, f) => CreateFolderRequested?.Invoke(this, f);
        folderView.RenameFolderRequested += (s, f) => RenameFolderRequested?.Invoke(this, f);
        folderView.DeleteFolderRequested += (s, f) => DeleteFolderRequested?.Invoke(this, f);
        folderView.NoteMovedToFolder += (s, data) => NoteMovedToFolder?.Invoke(this, data);
        folderView.ToggleFavoriteRequested += (s, note) => ToggleFavoriteRequested?.Invoke(this, note);
        folderView.RenameNoteRequested += (s, data) => RenameNoteRequested?.Invoke(this, data);
        folderView.DeleteNoteRequested += (s, note) => DeleteNoteRequested?.Invoke(this, note);
        return folderView;
    }

    private Border CreateRootNoteItem(Note note)
    {
        var border = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(10, 2, 10, 2),
            Tag = note
        };

        border.Tapped += (s, e) => NoteSelected?.Invoke(this, note);
        border.CanDrag = true;
        border.DragStarting += OnRootNoteDragStarting;

        var textBlock = new TextBlock
        {
            Text = note.Filename,
            FontSize = 14,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimary"],
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var menuFlyout = CreateStyledMenuFlyout();
        menuFlyout.Items.Add(CreateFlyoutItem("Toggle favorite", "\uE734", (s, e)
            => ToggleFavoriteRequested?.Invoke(this, note)));
        menuFlyout.Items.Add(CreateFlyoutItem("Rename note", "\uE8AC", (s, e)
            => StartInlineRename(border, note)));
        var deleteItem = CreateFlyoutItem("Delete note", "\uE74D", (s, e)
            => DeleteNoteRequested?.Invoke(this, note));
        deleteItem.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 251, 70, 76));
        menuFlyout.Items.Add(deleteItem);

        border.ContextFlyout = menuFlyout;
        border.Child = textBlock;

        return border;
    }

    private static MenuFlyout CreateStyledMenuFlyout()
    {
        var flyout = new MenuFlyout { Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.RightEdgeAlignedTop };
        flyout.MenuFlyoutPresenterStyle = new Style(typeof(MenuFlyoutPresenter));
        flyout.MenuFlyoutPresenterStyle.Setters.Add(new Setter(MenuFlyoutPresenter.BackgroundProperty, Application.Current.Resources["SideBar"]));
        flyout.MenuFlyoutPresenterStyle.Setters.Add(new Setter(MenuFlyoutPresenter.BorderBrushProperty, Application.Current.Resources["Separator"]));
        flyout.MenuFlyoutPresenterStyle.Setters.Add(new Setter(MenuFlyoutPresenter.BorderThicknessProperty, new Thickness(1)));
        flyout.MenuFlyoutPresenterStyle.Setters.Add(new Setter(MenuFlyoutPresenter.CornerRadiusProperty, new CornerRadius(8)));
        flyout.MenuFlyoutPresenterStyle.Setters.Add(new Setter(MenuFlyoutPresenter.PaddingProperty, new Thickness(4)));
        return flyout;
    }

    private static MenuFlyoutItem CreateFlyoutItem(string text, string iconGlyph, RoutedEventHandler handler)
    {
        var item = new MenuFlyoutItem
        {
            Text = text,
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimary"],
            Icon = new FontIcon { Glyph = iconGlyph, FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe MDL2 Assets") }
        };
        item.Click += handler;
        return item;
    }

    public void LoadNotesForSearch(List<Note> notes)
    {
        _allNotes = notes;
        if (AiSettings.IsAiEnabled)
        {
            _semanticSearch.InvalidateIndex();
            _semanticSearch.InvalidateEmbeddingsCache();
        }
    }

    public void SetViewMode(SidebarViewMode mode)
    {
        _currentMode = mode;

        if (mode is SidebarViewMode.Folder)
        {
            ClearSearch();
            FoldersContainer.Visibility = Visibility.Visible;
            SearchView.Visibility = Visibility.Collapsed;
        }
        else
        {
            FoldersContainer.Visibility = Visibility.Collapsed;
            SearchView.Visibility = Visibility.Visible;
        }
    }

    public void ClearSearch()
    {
        SearchTextBox.Text = string.Empty;
        ShowSearchOptions();
    }

    public void SetVaultName(string name)
    {
        VaultSelectorControl.SetVaultName(name);
    }

    public void SetSelectedNote(Note? note)
    {
        _favoritesView?.SetSelectedNote(note);

        foreach (var child in FoldersContainer.Children)
        {
            if (child is FolderView folderView)
            {
                folderView.SetSelectedNote(note);
            }
            else if (child is Border border && border.Tag is Note rootNote)
            {
                border.Background = note is not null && rootNote.FilePath == note.FilePath
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 52, 52, 52))
                    : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }
    }

    private void OnSearchTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Focus(FocusState.Programmatic);
    }

    private void OnSearchTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = SearchTextBox.Text?.Trim() ?? string.Empty;

        _searchInFlightCts?.Cancel();

        if (string.IsNullOrWhiteSpace(query))
        {
            _searchDebounceTimer?.Stop();
            _pendingSearchQuery = string.Empty;
            ShowSearchOptions();
            return;
        }

        ShowResults();

        _pendingSearchQuery = query;

        if (_searchDebounceTimer is null)
        {
            _searchDebounceTimer = DispatcherQueue.CreateTimer();
            _searchDebounceTimer.IsRepeating = false;
            _searchDebounceTimer.Interval = SearchDebounce;
            _searchDebounceTimer.Tick += OnSearchDebounceTick;
        }

        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private async void OnSearchDebounceTick(Microsoft.UI.Dispatching.DispatcherQueueTimer timer, object args)
    {
        var query = _pendingSearchQuery;
        if (string.IsNullOrWhiteSpace(query)) return;

        var cts = new CancellationTokenSource();
        _searchInFlightCts = cts;

        try
        {
            var results = await _activeSearch.SearchAsync(_allNotes, query);

            if (cts.IsCancellationRequested) return;

            SearchResultsControl.SetResults(results);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void ShowSearchOptions()
    {
        SearchHintText.Visibility = Visibility.Visible;
        SearchResultsControl.Visibility = Visibility.Collapsed;
    }

    private void ShowResults()
    {
        SearchHintText.Visibility = Visibility.Collapsed;
        SearchResultsControl.Visibility = Visibility.Visible;
    }

    private void OnSearchResultNoteSelected(object sender, Note note)
    {
        NoteSelected?.Invoke(this, note);
    }

    private void OnSearchResultMatchingLineSelected(object sender, (Note Note, int LineNumber) data)
    {
        MatchingLineSelected?.Invoke(this, data);
    }

    private void OnVaultItemSelected(object sender, VaultInfo vaultInfo)
    {
        VaultSelected?.Invoke(this, vaultInfo);
    }

    private void OnManageVaultsRequested(object sender, EventArgs e)
    {
        ManageVaultsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnSettingsClicked(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCreateNoteClicked(object sender, RoutedEventArgs e)
    {
        CreateNoteRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnCreateFolderClicked(object sender, RoutedEventArgs e)
    {
        CreateFolderRequested?.Invoke(this, null);
    }

    private void OnEmptySpaceTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (e.OriginalSource is not TextBox)
            FoldersContainer.Focus(FocusState.Programmatic);
    }

    private void OnRootDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
    }

    private void OnRootDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (e.DataView.Properties.TryGetValue("NoteFilePath", out var value) && value is string noteFilePath)
        {
            var note = _folderTreeService.FindNoteInTree(_rootFolder!, noteFilePath);
            if (note is not null)
            {
                NoteMovedToFolder?.Invoke(this, (Note: note, TargetFolder: _rootFolder!));
            }
        }
    }

    private void StartInlineRename(Border border, Note note)
    {
        var textBlock = border.Child as TextBlock;
        if (textBlock is null) return;

        var originalName = note.Filename;
        var originalPadding = border.Padding;
        var originalBackground = border.Background;
        var borderWidth = 1.5;

        border.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["Primary"];
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
            Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextPrimary"],
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            MinHeight = 0,
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center
        };
        textBox.Resources["TextControlThemeMinHeight"] = 0d;
        textBox.Resources["TextControlThemePadding"] = new Thickness(0);
        textBox.Resources["DeleteButtonVisibility"] = Visibility.Collapsed;
        textBox.Resources["TextControlBackgroundPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBackgroundFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBorderBrushPointerOver"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        textBox.Resources["TextControlBorderBrushFocused"] = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);

        border.Child = textBox;

        var committed = false;

        void RestoreBorder()
        {
            border.Child = textBlock;
            border.BorderBrush = null;
            border.BorderThickness = new Thickness(0);
            border.Padding = originalPadding;
            border.Background = originalBackground;
        }

        void CommitRename()
        {
            if (committed) return;
            committed = true;

            var newName = textBox.Text?.Trim();
            RestoreBorder();

            if (!string.IsNullOrWhiteSpace(newName) && newName != System.IO.Path.GetFileNameWithoutExtension(originalName))
                RenameNoteRequested?.Invoke(this, (note, newName));
            else
                NoteSelected?.Invoke(this, note);
        }

        void CancelRename()
        {
            if (committed) return;
            committed = true;
            RestoreBorder();
            NoteSelected?.Invoke(this, note);
        }

        textBox.LostFocus += (s, ev) =>
        {
            if (!IsFocusInPopup(textBox)) CommitRename();
        };
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

    private void OnRootNoteDragStarting(UIElement sender, Microsoft.UI.Xaml.DragStartingEventArgs args)
    {
        if (sender is Border border && border.Tag is Note note)
        {
            args.Data.Properties.Add("NoteFilePath", note.FilePath);
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
    }

    public Dictionary<string, bool> GetFolderExpandedStates()
    {
        return _rootFolder is not null ? _folderTreeService.GetExpandedStates(_rootFolder) : [];
    }

    public void RestoreFolderExpandedStates(Dictionary<string, bool> states)
    {
        if (_rootFolder is not null)
        {
            _folderTreeService.RestoreExpandedStates(_rootFolder, states);
        }
    }

    private static bool IsFocusInPopup(UIElement reference)
    {
        if (reference.XamlRoot is null) return false;
        var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(reference.XamlRoot) as DependencyObject;
        while (focused is not null)
        {
            if (focused is Microsoft.UI.Xaml.Controls.Primitives.Popup) return true;
            focused = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(focused);
        }
        return false;
    }
}
