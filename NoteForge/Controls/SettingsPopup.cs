using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using NoteForge.Configuration;
using NoteForge.Handlers.Notes;
using NoteForge.Models;
using NoteForge.Services.Ai;

namespace NoteForge.Controls;

public class SettingsPopup
{
    private Popup? _popup;

    public void Show(XamlRoot xamlRoot)
    {
        var aiSection = BuildAiSection();
        var graphState = BuildGraphSectionState();
        var graphSection = CreateGraphSection(
            graphState.ExplicitCheckbox,
            graphState.SemanticCheckbox,
            graphState.SemanticSlider,
            graphState.SemanticText,
            graphState.TfidfSlider,
            graphState.TfidfText);

        void SyncGraphForActiveProvider()
        {
            var enabled = AiSettings.IsAiEnabled;
            graphState.SemanticCheckbox.IsEnabled = enabled;
            graphState.SemanticSlider.IsEnabled = enabled;
            graphState.TfidfSlider.IsEnabled = enabled;
        }
        SyncGraphForActiveProvider();

        var saveButton = CreateSaveButton();
        var cancelButton = CreateCancelButton();

        saveButton.Click += async (s, e) =>
        {
            saveButton.IsEnabled = false;
            try
            {
                if (!await aiSection.SaveAsync(xamlRoot)) return;

                AiSettings.GraphShowExplicitLinks = graphState.ExplicitCheckbox.IsChecked ?? true;
                AiSettings.GraphShowSemanticLinks = graphState.SemanticCheckbox.IsChecked ?? true;
                AiSettings.GraphSemanticThreshold = (float)graphState.SemanticSlider.Value;
                AiSettings.GraphTfidfThreshold = (float)graphState.TfidfSlider.Value;

                _popup!.IsOpen = false;
            }
            finally
            {
                saveButton.IsEnabled = true;
            }
        };
        cancelButton.Click += (s, e) => _popup!.IsOpen = false;

        var buttons = CreateButtonRow(cancelButton, saveButton);

        var content = new StackPanel
        {
            Spacing = 20,
            Width = 400,
            Children =
            {
                CreateSectionHeader("Settings"),
                aiSection.Root,
                graphSection,
                buttons
            }
        };

        var container = new Border
        {
            Background = (Brush)Application.Current.Resources["SideBar"],
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(24),
            Child = content
        };

        _popup = new Popup
        {
            Child = container,
            IsLightDismissEnabled = true,
            LightDismissOverlayMode = LightDismissOverlayMode.On,
            XamlRoot = xamlRoot
        };

        var bounds = xamlRoot.Size;
        _popup.HorizontalOffset = (bounds.Width - 448) / 2;
        _popup.VerticalOffset = 80;
        _popup.IsOpen = true;
    }

    private sealed class AiSection
    {
        public required FrameworkElement Root { get; init; }
        public required Func<XamlRoot, Task<bool>> SaveAsync { get; init; }
    }

    private sealed class ProviderPanelState
    {
        public required FrameworkElement Root { get; init; }
        public required Func<XamlRoot, Task<bool>> SaveAsync { get; init; }
    }

    private sealed record ProviderItem(AiProviderType Type, string DisplayName);

    private static AiSection BuildAiSection()
    {
        var providerCombo = new ComboBox
        {
            Header = new TextBlock
            {
                Text = "AI Provider",
                FontSize = 12,
                Foreground = (Brush)Application.Current.Resources["TextSecondary"]
            },
            ItemsSource = new[]
            {
                new ProviderItem(AiProviderType.Disabled, "None (AI off)"),
                new ProviderItem(AiProviderType.Ollama, "Local (Ollama)"),
                new ProviderItem(AiProviderType.OpenAi, "OpenAI"),
                new ProviderItem(AiProviderType.Gemini, "Google Gemini")
            },
            DisplayMemberPath = nameof(ProviderItem.DisplayName),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        providerCombo.SelectedIndex = AiSettings.ActiveProvider switch
        {
            AiProviderType.Ollama => 1,
            AiProviderType.OpenAi => 2,
            AiProviderType.Gemini => 3,
            _ => 0
        };

        var conditionalHost = new ContentControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        ProviderPanelState? currentPanel = null;

        void Rebuild()
        {
            var selected = ((ProviderItem)providerCombo.SelectedItem).Type;
            currentPanel = selected switch
            {
                AiProviderType.Ollama => BuildOllamaPanel(),
                AiProviderType.OpenAi => BuildCloudProviderPanel(AiProviderType.OpenAi),
                AiProviderType.Gemini => BuildCloudProviderPanel(AiProviderType.Gemini),
                _ => BuildDisabledPanel()
            };
            conditionalHost.Content = currentPanel.Root;
        }
        providerCombo.SelectionChanged += (s, e) => Rebuild();
        Rebuild();

        var root = new StackPanel
        {
            Spacing = 12,
            Children = { providerCombo, conditionalHost }
        };

        async Task<bool> SaveAsync(XamlRoot xamlRoot)
        {
            var selected = ((ProviderItem)providerCombo.SelectedItem).Type;

            if (currentPanel is not null)
            {
                if (!await currentPanel.SaveAsync(xamlRoot))
                {
                    return false;
                }
            }

            var oldProvider = AiSettings.ActiveProvider;

            if (selected == AiProviderType.Disabled)
            {
                if (selected != oldProvider)
                    AiSettings.ActiveProvider = selected;
                return true;
            }

            if (await NeedsReembedAsync(selected))
            {
                var notes = await GetVaultNotesAsync();
                var confirmed = await ConfirmReembedAsync(xamlRoot, selected, notes.Count);
                if (!confirmed) return false;

                if (selected != oldProvider)
                    AiSettings.ActiveProvider = selected;
                _ = App.EmbeddingService.RegenerateAllAsync(notes);
            }
            else if (selected != oldProvider)
            {
                AiSettings.ActiveProvider = selected;
            }

            return true;
        }

        return new AiSection { Root = root, SaveAsync = SaveAsync };
    }

    private static ProviderPanelState BuildDisabledPanel()
    {
        var text = new TextBlock
        {
            Text = "AI features are disabled. Pick a provider to enable summaries and semantic search.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        };
        return new ProviderPanelState
        {
            Root = text,
            SaveAsync = (_) => Task.FromResult(true)
        };
    }

    private static ProviderPanelState BuildOllamaPanel()
    {
        var urlBox = CreateTextBox(AiSettings.OllamaUrl, "http://localhost:11434");
        var chatBox = CreateTextBox(AiSettings.OllamaChatModel, "ibm/granite4:1b-h");

        var embeddingCombo = new ComboBox
        {
            Header = new TextBlock { Text = "Embedding model", FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextSecondary"] },
            ItemsSource = AiModelCatalog.EmbeddingModels(AiProviderType.Ollama),
            DisplayMemberPath = nameof(AiEmbeddingModel.DisplayName),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        embeddingCombo.SelectedItem = AiModelCatalog.FindEmbeddingModel(AiProviderType.Ollama, AiSettings.OllamaEmbeddingModel)
                                      ?? AiModelCatalog.EmbeddingModels(AiProviderType.Ollama)[0];

        var testButton = new Button { Content = "Test connection", HorizontalAlignment = HorizontalAlignment.Stretch };
        var testStatus = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
        testButton.Click += async (s, e) =>
        {
            testStatus.Text = "Testing...";
            var savedUrl = AiSettings.OllamaUrl;
            AiSettings.OllamaUrl = (urlBox.Text ?? string.Empty).Trim().TrimEnd('/');
            try
            {
                var result = await App.Services.GetRequiredService<AiProviderRegistry>().For(AiProviderType.Ollama).TestConnectionAsync();
                testStatus.Text = result.Success ? "Connection OK." : $"Failed: {result.ErrorMessage}";
            }
            finally
            {
                AiSettings.OllamaUrl = savedUrl;
            }
        };

        var root = new StackPanel
        {
            Spacing = 12,
            Children =
            {
                CreateField("Ollama URL", urlBox),
                CreateField("Text generation model", chatBox),
                embeddingCombo,
                testButton,
                testStatus
            }
        };

        Task<bool> SaveAsync(XamlRoot xamlRoot)
        {
            var url = (urlBox.Text ?? string.Empty).Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(url)) AiSettings.OllamaUrl = url;

            var chatModel = (chatBox.Text ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(chatModel)) AiSettings.OllamaChatModel = chatModel;

            if (embeddingCombo.SelectedItem is AiEmbeddingModel emb)
            {
                AiSettings.OllamaEmbeddingModel = emb.Id;
            }
            return Task.FromResult(true);
        }

        return new ProviderPanelState { Root = root, SaveAsync = SaveAsync };
    }

    private static ProviderPanelState BuildCloudProviderPanel(AiProviderType provider)
    {
        var hasKey = ApiKeyVault.HasKey(provider);
        var keyBox = new PasswordBox
        {
            Header = new TextBlock { Text = "API key", FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextSecondary"] },
            PlaceholderText = hasKey ? "•••••••• (saved)" : "Enter API key",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var keyDirty = false;
        keyBox.PasswordChanged += (s, e) => keyDirty = true;

        var chatModels = AiModelCatalog.ChatModels(provider);
        var chatCombo = new ComboBox
        {
            Header = new TextBlock { Text = "Chat model", FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextSecondary"] },
            ItemsSource = chatModels,
            DisplayMemberPath = nameof(AiChatModel.DisplayName),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var currentChatId = provider is AiProviderType.OpenAi ? AiSettings.OpenAiChatModel : AiSettings.GeminiChatModel;
        chatCombo.SelectedItem = chatModels.FirstOrDefault(m => m.Id == currentChatId) ?? chatModels[0];

        var embedModels = AiModelCatalog.EmbeddingModels(provider);
        var embedCombo = new ComboBox
        {
            Header = new TextBlock { Text = "Embedding model", FontSize = 12, Foreground = (Brush)Application.Current.Resources["TextSecondary"] },
            ItemsSource = embedModels,
            DisplayMemberPath = nameof(AiEmbeddingModel.DisplayName),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        var currentEmbedId = provider is AiProviderType.OpenAi ? AiSettings.OpenAiEmbeddingModel : AiSettings.GeminiEmbeddingModel;
        embedCombo.SelectedItem = embedModels.FirstOrDefault(m => m.Id == currentEmbedId) ?? embedModels[0];

        var testButton = new Button { Content = "Test connection", HorizontalAlignment = HorizontalAlignment.Stretch };
        var testStatus = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap };
        var testPassed = false;
        testButton.Click += async (s, e) =>
        {
            testStatus.Text = "Testing...";
            if (keyDirty)
            {
                var entered = keyBox.Password;
                if (string.IsNullOrEmpty(entered))
                {
                    testStatus.Text = "Enter an API key first.";
                    return;
                }
                ApiKeyVault.Save(provider, entered);
            }
            else if (!ApiKeyVault.HasKey(provider))
            {
                testStatus.Text = "Enter an API key first.";
                return;
            }

            var instance = App.Services.GetRequiredService<AiProviderRegistry>().For(provider);
            var result = await instance.TestConnectionAsync();
            if (result.Success)
            {
                testPassed = true;
                keyDirty = false;
                testStatus.Text = "Connection OK.";
            }
            else
            {
                testPassed = false;
                testStatus.Text = $"Failed: {result.ErrorMessage}";
            }
        };

        var root = new StackPanel
        {
            Spacing = 12,
            Children = { keyBox, chatCombo, embedCombo, testButton, testStatus }
        };

        Task<bool> SaveAsync(XamlRoot xamlRoot)
        {
            if (!testPassed && (keyDirty || !ApiKeyVault.HasKey(provider)))
            {
                testStatus.Text = "Run Test connection successfully before saving.";
                return Task.FromResult(false);
            }

            if (chatCombo.SelectedItem is AiChatModel chat)
            {
                if (provider is AiProviderType.OpenAi) AiSettings.OpenAiChatModel = chat.Id;
                else AiSettings.GeminiChatModel = chat.Id;
            }
            if (embedCombo.SelectedItem is AiEmbeddingModel emb)
            {
                if (provider is AiProviderType.OpenAi) AiSettings.OpenAiEmbeddingModel = emb.Id;
                else AiSettings.GeminiEmbeddingModel = emb.Id;
            }
            return Task.FromResult(true);
        }

        return new ProviderPanelState { Root = root, SaveAsync = SaveAsync };
    }

    private static async Task<bool> NeedsReembedAsync(AiProviderType newProvider)
    {
        if (!App.EmbeddingRepository.IsInitialized) return false;

        var existing = await App.EmbeddingRepository.GetAllEmbeddingsAsync();
        if (existing.Count is 0) return false;

        var metadata = await App.EmbeddingRepository.GetMetadataAsync();
        var newProviderInstance = App.Services.GetRequiredService<AiProviderRegistry>().For(newProvider);
        var newDimension = newProviderInstance.EmbeddingDimension;
        var newProviderName = newProvider.ToString();
        var newModelId = newProvider switch
        {
            AiProviderType.OpenAi => AiSettings.OpenAiEmbeddingModel,
            AiProviderType.Gemini => AiSettings.GeminiEmbeddingModel,
            AiProviderType.Ollama => AiSettings.OllamaEmbeddingModel,
            _ => string.Empty
        };

        if (metadata is null)
        {
            return true;
        }

        if (metadata.ProviderName != newProviderName || metadata.Dimension != newDimension)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(metadata.ModelId) && metadata.ModelId != newModelId)
        {
            return true;
        }

        return false;
    }

    private static async Task<List<Note>> GetVaultNotesAsync()
    {
        var notes = await App.Mediator.Send(new GetNotesQueryRequest());
        return [.. notes];
    }

    private static async Task<bool> ConfirmReembedAsync(XamlRoot xamlRoot, AiProviderType newProvider, int noteCount)
    {
        var totalChars = await EstimateTotalCharsAsync();
        var approxTokens = totalChars / 4.0;
        var embedModelId = newProvider switch
        {
            AiProviderType.OpenAi => AiSettings.OpenAiEmbeddingModel,
            AiProviderType.Gemini => AiSettings.GeminiEmbeddingModel,
            _ => AiSettings.OllamaEmbeddingModel
        };
        var pricePerMillion = AiModelCatalog.FindEmbeddingModel(newProvider, embedModelId)?.PricePerMillionTokens ?? 0;
        var estimatedCost = approxTokens / 1_000_000.0 * pricePerMillion;

        var costLine = pricePerMillion > 0
            ? $"Estimated cost: ~${estimatedCost:F2} USD."
            : "No API cost (local model).";

        var dialog = new ContentDialog
        {
            Title = "Switch AI provider?",
            Content = $"This will re-embed all {noteCount} notes via {newProvider}. {costLine}",
            PrimaryButtonText = "Re-embed",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = xamlRoot
        };
        var result = await dialog.ShowAsync();
        return result is ContentDialogResult.Primary;
    }

    private static async Task<long> EstimateTotalCharsAsync()
    {
        var notes = await GetVaultNotesAsync();
        long total = 0;
        foreach (var note in notes)
        {
            total += note.Text?.Length ?? 0;
        }
        return total;
    }

    private sealed class GraphSectionState
    {
        public required CheckBox ExplicitCheckbox { get; init; }
        public required CheckBox SemanticCheckbox { get; init; }
        public required Slider SemanticSlider { get; init; }
        public required TextBlock SemanticText { get; init; }
        public required Slider TfidfSlider { get; init; }
        public required TextBlock TfidfText { get; init; }
    }

    private static GraphSectionState BuildGraphSectionState()
    {
        var explicitCheckbox = new CheckBox
        {
            Content = "Show Explicit Links",
            IsChecked = AiSettings.GraphShowExplicitLinks,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"]
        };
        var semanticCheckbox = new CheckBox
        {
            Content = "Show Semantic Links",
            IsChecked = AiSettings.GraphShowSemanticLinks,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"]
        };

        var semanticText = new TextBlock
        {
            Text = $"{(int)(AiSettings.GraphSemanticThreshold * 100)}%",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var semanticSlider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = AiSettings.GraphSemanticThreshold,
            StepFrequency = 0.05
        };
        semanticSlider.ValueChanged += (s, e) =>
        {
            semanticText.Text = $"{(int)(semanticSlider.Value * 100)}%";
        };

        var tfidfText = new TextBlock
        {
            Text = $"{(int)(AiSettings.GraphTfidfThreshold * 100)}%",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var tfidfSlider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = AiSettings.GraphTfidfThreshold,
            StepFrequency = 0.05
        };
        tfidfSlider.ValueChanged += (s, e) =>
        {
            tfidfText.Text = $"{(int)(tfidfSlider.Value * 100)}%";
        };

        return new GraphSectionState
        {
            ExplicitCheckbox = explicitCheckbox,
            SemanticCheckbox = semanticCheckbox,
            SemanticSlider = semanticSlider,
            SemanticText = semanticText,
            TfidfSlider = tfidfSlider,
            TfidfText = tfidfText
        };
    }

    private static StackPanel CreateGraphSection(
        CheckBox explicitLinksCheckbox,
        CheckBox semanticLinksCheckbox,
        Slider semanticSlider,
        TextBlock semanticText,
        Slider tfidfSlider,
        TextBlock tfidfText)
    {
        var section = new StackPanel { Spacing = 12 };

        section.Children.Add(new TextBlock
        {
            Text = "Knowledge Graph",
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        });

        section.Children.Add(explicitLinksCheckbox);
        section.Children.Add(semanticLinksCheckbox);

        var semanticField = new StackPanel { Spacing = 6 };
        semanticField.Children.Add(new TextBlock
        {
            Text = "Semantic Threshold",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        });
        semanticField.Children.Add(semanticSlider);
        semanticField.Children.Add(semanticText);
        section.Children.Add(semanticField);

        var tfidfField = new StackPanel { Spacing = 6 };
        tfidfField.Children.Add(new TextBlock
        {
            Text = "TF-IDF Threshold",
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        });
        tfidfField.Children.Add(tfidfSlider);
        tfidfField.Children.Add(tfidfText);
        section.Children.Add(tfidfField);

        return section;
    }

    private static TextBlock CreateSectionHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"]
        };
    }

    private static StackPanel CreateField(string label, TextBox textBox)
    {
        var field = new StackPanel { Spacing = 4 };
        field.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        });
        field.Children.Add(textBox);
        return field;
    }

    private static TextBox CreateTextBox(string text, string placeholder)
    {
        var textBox = new TextBox
        {
            Text = text,
            PlaceholderText = placeholder,
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["AppBackground"],
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        textBox.Resources["TextControlBackgroundPointerOver"] = Application.Current.Resources["AppBackground"];
        textBox.Resources["TextControlBackgroundFocused"] = Application.Current.Resources["AppBackground"];
        textBox.Resources["TextControlBorderBrushPointerOver"] = Application.Current.Resources["Gray600"];
        textBox.Resources["TextControlBorderBrushFocused"] = Application.Current.Resources["Primary"];
        return textBox;
    }

    private static Button CreateSaveButton()
    {
        var saveButton = new Button
        {
            Content = "Save",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = (Brush)Application.Current.Resources["Primary"],
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(0, 8, 0, 8)
        };
        saveButton.Resources["ButtonBackgroundPointerOver"] = Application.Current.Resources["AccentButtonBackgroundPointerOver"];
        saveButton.Resources["ButtonBackgroundPressed"] = Application.Current.Resources["AccentButtonBackgroundPressed"];
        saveButton.Resources["ButtonForegroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        saveButton.Resources["ButtonForegroundPressed"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));
        return saveButton;
    }

    private static Button CreateCancelButton()
    {
        var cancelButton = new Button
        {
            Content = "Cancel",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = (Brush)Application.Current.Resources["AppSurface"],
            Foreground = (Brush)Application.Current.Resources["TextPrimary"],
            CornerRadius = new CornerRadius(6),
            BorderBrush = (Brush)Application.Current.Resources["Separator"],
            BorderThickness = new Thickness(1),
            Padding = new Thickness(0, 8, 0, 8)
        };
        cancelButton.Resources["ButtonBackgroundPointerOver"] = Application.Current.Resources["Gray600"];
        cancelButton.Resources["ButtonBackgroundPressed"] = Application.Current.Resources["Separator"];
        return cancelButton;
    }

    private static Grid CreateButtonRow(Button cancelButton, Button saveButton)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            },
            ColumnSpacing = 8
        };
        Grid.SetColumn(cancelButton, 0);
        Grid.SetColumn(saveButton, 1);
        grid.Children.Add(cancelButton);
        grid.Children.Add(saveButton);
        return grid;
    }
}
