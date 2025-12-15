using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using NoteForge.Controls;
using NoteForge.Handlers;
using NoteForge.Interfaces;
using NoteForge.Models;

namespace NoteForge.Views;

public sealed partial class WorkspacePage : Page
{
    private readonly INoteService _noteService;
    private readonly ITabManager _tabManager;
    private readonly IMarkdownPreviewService _previewService;
    private readonly IDialogService _dialogService;
    private readonly IMediator _mediator;
    private readonly ILogger<WorkspacePage> _logger;
    private Note? _selectedNote;
    private CancellationTokenSource? _saveCts;
    private CancellationTokenSource? _renameCts;
    private CancellationTokenSource? _summaryCts;
    private bool _isLoading;
    private bool _isSyncingTitle;
    private bool _isSplitterDragging;
    private double _splitterStartX;

    public WorkspacePage()
    {
        InitializeComponent();
        _noteService = App.NoteService;
        _tabManager = App.TabManager;
        _previewService = App.PreviewService;
        _dialogService = App.DialogService;
        _mediator = App.Mediator;
        _logger = App.LoggerFactory.CreateLogger<WorkspacePage>();

        TabBarControl.SetItemsSource(_tabManager.Tabs);

        _tabManager.ActiveTabChanged += OnActiveTabChanged;

        Loaded += WorkspacePage_Loaded;
    }

    private async void WorkspacePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadNotes();
    }

    private async Task LoadNotes()
    {
        var workspace = await _mediator.Send(new LoadWorkspaceQueryRequest());

        Sidebar.SetVaultName(workspace.VaultName);

        if (workspace.RootFolder is not null)
        {
            Sidebar.LoadFolders(workspace.RootFolder, workspace.FavoritesSection);
        }

        if (workspace.InitialNoteFilePath is not null)
        {
            var allNotes = (await _mediator.Send(new GetNotesQueryRequest())).ToList();
            var activeNote = allNotes.FirstOrDefault(n => n.FilePath == workspace.InitialNoteFilePath);
            if (activeNote is not null)
            {
                SetSelectedNote(activeNote);
            }
        }
    }

    private void SetSelectedNote(Note? note)
    {
        _selectedNote = note;
        UpdateEditorState();
    }

    private async Task ResetWorkspaceAsync()
    {
        _tabManager.Tabs.Clear();
        SetSelectedNote(null);
        await LoadNotes();
    }

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

    private void OnToggleSidebarClicked(object sender, RoutedEventArgs e)
    {
        if (Sidebar.Visibility is Visibility.Visible)
        {
            Sidebar.Visibility = Visibility.Collapsed;
            SplitterBorder.Visibility = Visibility.Collapsed;
            SidebarColumn.Width = new GridLength(0);
            TitleBarSidebarColumn.Width = GridLength.Auto;
        }
        else
        {
            Sidebar.Visibility = Visibility.Visible;
            SplitterBorder.Visibility = Visibility.Visible;
            SidebarColumn.Width = new GridLength(250);
            TitleBarSidebarColumn.Width = new GridLength(250);
        }
    }

    private void OnFolderViewClicked(object sender, RoutedEventArgs e)
    {
        Sidebar.Visibility = Visibility.Visible;
        SplitterBorder.Visibility = Visibility.Visible;
        Sidebar.SetViewMode(SidebarViewMode.Folder);

        if (SidebarColumn.ActualWidth is 0)
        {
            SidebarColumn.Width = new GridLength(250);
            TitleBarSidebarColumn.Width = new GridLength(250);
        }
    }

    private async void OnSearchViewClicked(object sender, RoutedEventArgs e)
    {
        var allNotes = (await _mediator.Send(new GetNotesQueryRequest())).ToList();

        Sidebar.LoadNotesForSearch(allNotes);
        Sidebar.SetViewMode(SidebarViewMode.Search);
        Sidebar.Visibility = Visibility.Visible;
        SplitterBorder.Visibility = Visibility.Visible;

        if (SidebarColumn.ActualWidth is 0)
        {
            SidebarColumn.Width = new GridLength(250);
            TitleBarSidebarColumn.Width = new GridLength(250);
        }
    }

    private void OnNoteSelected(object sender, Note selectedNote)
    {
        if (_tabManager.ActiveTab?.FilePath == selectedNote.FilePath)
        {
            SetSelectedNote(selectedNote);
            return;
        }
        _tabManager.OpenTab(selectedNote);
    }

    private void OnMatchingLineSelected(object sender, (Note Note, int LineNumber) args)
    {
        if (_tabManager.ActiveTab?.FilePath == args.Note.FilePath)
        {
            SetSelectedNote(args.Note);
        }
        else
        {
            _tabManager.OpenTab(args.Note);
        }

        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () 
            => EditorView.NavigateToLine(args.LineNumber));
    }

    private void OnTabSelected(object sender, Tab tab)
    {
        _tabManager.ActivateTab(tab);
    }

    private void OnCloseTabClicked(object sender, Tab tab)
    {
        _tabManager.CloseTab(tab);
    }

    private void OnActiveTabChanged(object? sender, Tab? activeTab)
    {
        DispatcherQueue.TryEnqueue(async () => await HandleActiveTabChangedAsync(activeTab));
    }

    private async Task HandleActiveTabChangedAsync(Tab? activeTab)
    {
        TabBarControl.SetSelectedItem(activeTab);

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
        {
            _tabManager.CloseTab(activeTab);
        }
    }

    private void UpdateEditorState()
    {
        var showEditor = _selectedNote is not null && _tabManager.ActiveTab?.IsNewTab is not true;
        NewTabView.Visibility = showEditor ? Visibility.Collapsed : Visibility.Visible;
        EditorView.Visibility = showEditor ? Visibility.Visible : Visibility.Collapsed;

        if (!showEditor)
        {
            return;
        }

        _summaryCts?.Cancel();
        EditorView.HideAiSummary();

        _isLoading = true;
        EditorView.SetTitle(_selectedNote!.Filename);
        EditorView.SetContent(_selectedNote.Text);
        _isLoading = false;
        UpdatePreviewAsync();
    }

    private async void UpdatePreviewAsync()
    {
        if (_selectedNote is null || EditorView.GetPreviewColumnWidth() is 0)
        {
            return;
        }

        try
        {
            var previewWebView = EditorView.GetPreviewWebView();
            if (previewWebView.CoreWebView2 is null) await previewWebView.EnsureCoreWebView2Async();
            var html = _previewService.ConvertToHtml(_selectedNote.Text ?? "");
            previewWebView.NavigateToString(_previewService.WrapInHtmlDocument(html));
        }
        catch { }
    }

    private void OnTogglePreviewClicked(object sender, EventArgs e)
    {
        var isHidden = EditorView.GetPreviewColumnWidth() is 0;
        EditorView.SetPreviewColumnWidth(isHidden ? 1 : 0);
        if (isHidden) UpdatePreviewAsync();
    }

    private void OnNoteTitleTextChanged(object sender, string newTitle)
    {
        if (_isLoading || _isSyncingTitle)
        {
            return;
        }

        _renameCts?.Cancel();
        _renameCts = new CancellationTokenSource();
        var token = _renameCts.Token;

        Task.Delay(200, token).ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                return;
            }

            DispatcherQueue.TryEnqueue(async () =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                await RenameCurrentNote(newTitle);
            });
        });
    }

    private void SyncTitles(string title)
    {
        _isSyncingTitle = true;
        EditorView.SetTitle(title);
        _isSyncingTitle = false;
    }

    private async void OnTitleUnfocused(object sender, EventArgs e)
    {
        _renameCts?.Cancel();
        await RenameCurrentNote(null);
    }

    private async Task RenameCurrentNote(string? newTitle)
    {
        if (_selectedNote is null)
        {
            return;
        }

        var currentTitle = _selectedNote.Filename;
        newTitle = newTitle?.Trim();

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            SyncTitles(currentTitle);
            return;
        }

        if (string.Equals(newTitle, currentTitle, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (await _mediator.Send(new RenameNoteCommandRequest(_selectedNote, newTitle)) && _tabManager.ActiveTab is not null)
        {
            _tabManager.ActiveTab.DisplayName = _selectedNote.Filename;
            _tabManager.ActiveTab.FilePath = _selectedNote.FilePath;
            await LoadNotes();
        }
        else SyncTitles(currentTitle);
    }

    private async void OnNoteContentChanged(object sender, string newContent)
    {
        if (_isLoading || _selectedNote is null)
        {
            return;
        }

        _tabManager.SetDirty(_selectedNote.FilePath, true);
        _saveCts?.Cancel();
        _saveCts = new CancellationTokenSource();
        var token = _saveCts.Token;

        try
        {
            await Task.Delay(200, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            _selectedNote.Text = newContent;
            UpdatePreviewAsync();
            await _mediator.Send(new SaveNoteCommandRequest(_selectedNote), token);
            _tabManager.SetDirty(_selectedNote.FilePath, false);
        }
        catch (TaskCanceledException) { }
    }

    private void OnNewTabClicked(object sender, EventArgs e)
    {
        _tabManager.OpenNewTab();
    }

    private async void OnCreateNewNoteClicked(object sender, RoutedEventArgs e)
    {
        var newNote = await _mediator.Send(new CreateNoteCommandRequest());
        if (newNote is not null)
        {
            await LoadNotes();
            _tabManager.OpenTab(newNote);
        }
    }

    private async void OnGoToFileClicked(object sender, RoutedEventArgs e)
    {
        var allNotes = (await _mediator.Send(new GetNotesQueryRequest())).ToList();

        var searchBox = new AutoSuggestBox
        {
            PlaceholderText = "Type to search for a note...",
            Width = 400,
            QueryIcon = new SymbolIcon(Symbol.Find),
            Height = 36
        };

        searchBox.Resources["ControlCornerRadius"] = new CornerRadius(8);
        searchBox.Resources["OverlayCornerRadius"] = new CornerRadius(8);
        searchBox.Resources["TextControlBackground"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlBackgroundPointerOver"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlBackgroundFocused"] = Application.Current.Resources["AppSurface"];
        searchBox.Resources["TextControlForeground"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlForegroundPointerOver"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlForegroundFocused"] = Application.Current.Resources["TextPrimary"];
        searchBox.Resources["TextControlBorderBrush"] = Application.Current.Resources["Separator"];
        searchBox.Resources["TextControlBorderBrushPointerOver"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlBorderBrushFocused"] = Application.Current.Resources["Primary"];
        searchBox.Resources["TextControlPlaceholderForeground"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlPlaceholderForegroundPointerOver"] = Application.Current.Resources["TextSecondary"];
        searchBox.Resources["TextControlPlaceholderForegroundFocused"] = Application.Current.Resources["TextSecondary"];

        searchBox.ItemTemplate = (Microsoft.UI.Xaml.DataTemplate)XamlReader.Load(@"
            <DataTemplate xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"">
                <Grid Padding=""8,6"">
                    <TextBlock Text=""{Binding Filename}""
                               FontSize=""13""
                               Foreground=""#DADADA""
                               TextTrimming=""CharacterEllipsis""/>
                </Grid>
            </DataTemplate>");

        var popup = new Popup
        {
            Child = searchBox,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            XamlRoot = XamlRoot
        };

        var bounds = XamlRoot.Size;
        popup.HorizontalOffset = (bounds.Width - 400) / 2;
        popup.VerticalOffset = 100;

        searchBox.TextChanged += (s, args) =>
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = searchBox.Text;
                var searchResults = App.SearchService.SearchByName(allNotes, query);
                searchBox.ItemsSource = searchResults.Take(10).ToList();
            }
        };

        searchBox.QuerySubmitted += (s, args) =>
        {
            Note? selectedNote = null;

            if (args.ChosenSuggestion is Note note)
            {
                selectedNote = note;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                var searchResults = App.SearchService.SearchByName(allNotes, args.QueryText);
                selectedNote = searchResults.FirstOrDefault();
            }

            if (selectedNote is not null)
            {
                _tabManager.OpenTab(selectedNote);
                popup.IsOpen = false;
            }
        };

        searchBox.PreviewKeyDown += (s, args) =>
        {
            if (args.Key is Windows.System.VirtualKey.Escape)
            {
                popup.IsOpen = false;
                args.Handled = true;
            }
        };

        popup.IsOpen = true;
        await Task.Delay(50);

        searchBox.ItemsSource = allNotes.Take(10).ToList();
        searchBox.Focus(FocusState.Programmatic);
    }

    private async void OnToggleFavoriteClicked(object sender, Note note)
    {
        await _mediator.Send(new ToggleFavoriteCommandRequest(note));
        await LoadNotes();
    }

    private async void OnCreateFolderClicked(object sender, Folder? folder)
    {
        var dialog = new ContentDialog
        {
            Title = "New Folder",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var nameBox = new TextBox
        {
            PlaceholderText = "Folder name",
            Margin = new Thickness(0, 8, 0, 0)
        };

        dialog.Content = nameBox;

        var result = await dialog.ShowAsync();
        if (result is ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            var folderPath = folder?.DirectoryPath ?? _noteService.CurrentNotebookPath;
            await _mediator.Send(new CreateFolderCommandRequest(folderPath, nameBox.Text));
            await LoadNotes();
        }
    }

    private async void OnRenameFolderClicked(object sender, Folder folder)
    {
        var dialog = new ContentDialog
        {
            Title = "Rename Folder",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var nameBox = new TextBox
        {
            Text = folder.Name,
            Margin = new Thickness(0, 8, 0, 0)
        };

        dialog.Content = nameBox;

        var result = await dialog.ShowAsync();
        if (result is ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            await _mediator.Send(new RenameFolderCommandRequest(folder.DirectoryPath, nameBox.Text));
            await LoadNotes();
        }
    }

    private async void OnDeleteFolderClicked(object sender, Folder folder)
    {
        if (!folder.IsEmpty)
        {
            await _dialogService.ShowErrorAsync("Cannot delete a non-empty folder. Please remove all notes and subfolders first.", XamlRoot);
            return;
        }

        var dialog = new ContentDialog
        {
            Title = "Delete Folder",
            Content = $"Are you sure you want to delete '{folder.Name}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result is ContentDialogResult.Primary)
        {
            await _mediator.Send(new DeleteFolderCommandRequest(folder.DirectoryPath));
            await LoadNotes();
        }
    }

    private async void OnNoteMovedToFolder(object sender, (Note Note, Folder TargetFolder) e)
    {
        var wasSelected = _selectedNote?.FilePath == e.Note.FilePath;
        var expandedStates = Sidebar.GetFolderExpandedStates();

        await _mediator.Send(new MoveNoteToFolderCommandRequest(e.Note, e.TargetFolder.DirectoryPath));
        await LoadNotes();

        Sidebar.RestoreFolderExpandedStates(expandedStates);

        if (wasSelected && _selectedNote is not null)
        {
            Sidebar.SetSelectedNote(_selectedNote);
        }
    }

    private async void OnGenerateSummaryClicked(object sender, EventArgs e)
    {
        if (_selectedNote is null || string.IsNullOrWhiteSpace(_selectedNote.Text))
        {
            return;
        }

        _summaryCts?.Cancel();
        _summaryCts = new CancellationTokenSource();

        await _mediator.Send(new GenerateInlineAiSummaryCommandRequest(
            _selectedNote,
            OnSummaryStarted: () =>
            {
                EditorView.ShowAiSummary("Generating summary...");
                EditorView.SetSummaryButtonEnabled(false);
            },
            OnFirstToken: () => EditorView.SetAiSummaryText(string.Empty),
            OnTokenReceived: token => EditorView.AppendAiSummaryText(token),
            OnSummaryCompleted: () => EditorView.SetSummaryButtonEnabled(true),
            OnSummaryCancelled: () =>
            {
                EditorView.SetAiSummaryText("Summary generation cancelled.");
                EditorView.SetSummaryButtonEnabled(true);
            },
            OnSummaryFailed: error =>
            {
                EditorView.SetAiSummaryText($"Error: {error}");
                EditorView.SetSummaryButtonEnabled(true);
            }), _summaryCts.Token);
    }

    private void OnCloseSummaryClicked(object sender, EventArgs e)
    {
        _summaryCts?.Cancel();
        EditorView.HideAiSummary();
    }

    private void OnSplitterPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        SplitterIndicator.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["Primary"];
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);
    }

    private void OnSplitterPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
        {
            SplitterIndicator.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["Separator"];
        }
        ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);
    }

    private void OnSplitterPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isSplitterDragging = true;
        _splitterStartX = e.GetCurrentPoint(this).Position.X;
        SplitterBorder.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
            return;

        var currentPoint = e.GetCurrentPoint(this);
        var deltaX = currentPoint.Position.X - _splitterStartX;

        var currentWidth = SidebarColumn.ActualWidth;
        var newWidth = currentWidth + deltaX;

        var totalWidth = ActualWidth;
        var maxWidth = totalWidth * 0.8;

        if (newWidth >= SidebarColumn.MinWidth && newWidth <= maxWidth && newWidth <= totalWidth - 200)
        {
            SidebarColumn.Width = new GridLength(newWidth);
            TitleBarSidebarColumn.Width = new GridLength(newWidth);
            _splitterStartX = currentPoint.Position.X;
        }

        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_isSplitterDragging)
        {
            _isSplitterDragging = false;
            SplitterBorder.ReleasePointerCapture(e.Pointer);
            SplitterIndicator.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["Separator"];
        }
        e.Handled = true;
    }
}