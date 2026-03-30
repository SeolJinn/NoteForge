using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using NoteForge.Configuration;

namespace NoteForge.Controls;

public class SettingsPopup
{
    private Popup? _popup;

    public void Show(XamlRoot xamlRoot)
    {
        var ollamaUrlBox = CreateTextBox(OllamaSettings.OllamaUrl, "http://localhost:11434");
        var ollamaModelBox = CreateTextBox(OllamaSettings.OllamaModel, "ibm/granite4:1b-h");
        var embeddingModelBox = CreateTextBox(OllamaSettings.EmbeddingModel, "nomic-embed-text");

        var aiToggle = new ToggleSwitch
        {
            IsOn = OllamaSettings.AiEnabled,
            OnContent = "Enabled",
            OffContent = "Disabled",
            Header = new StackPanel
            {
                Spacing = 2,
                Children =
                {
                    new TextBlock
                    {
                        Text = "AI Features",
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = (Brush)Application.Current.Resources["TextSecondary"]
                    },
                    new TextBlock
                    {
                        Text = "Enable AI-powered search and summaries",
                        FontSize = 12,
                        Foreground = (Brush)Application.Current.Resources["TextSecondary"],
                        Opacity = 0.7
                    }
                }
            }
        };

        var ollamaSection = CreateSection("AI / Ollama", [
            CreateField("Ollama URL", ollamaUrlBox),
            CreateField("Text generation model", ollamaModelBox),
            CreateField("Embedding model", embeddingModelBox)
        ]);
        ollamaSection.Visibility = OllamaSettings.AiEnabled ? Visibility.Visible : Visibility.Collapsed;

        var showExplicitLinksCheckbox = new CheckBox
        {
            Content = "Show Explicit Links",
            IsChecked = OllamaSettings.GraphShowExplicitLinks,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"]
        };

        var showSemanticLinksCheckbox = new CheckBox
        {
            Content = "Show Semantic Links",
            IsChecked = OllamaSettings.GraphShowSemanticLinks,
            IsEnabled = aiToggle.IsOn,
            Foreground = (Brush)Application.Current.Resources["TextPrimary"]
        };

        var semanticThresholdText = new TextBlock
        {
            Text = $"{(int)(OllamaSettings.GraphSemanticThreshold * 100)}%",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var semanticThresholdSlider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = OllamaSettings.GraphSemanticThreshold,
            StepFrequency = 0.05,
            IsEnabled = aiToggle.IsOn
        };
        semanticThresholdSlider.ValueChanged += (s, e) =>
        {
            semanticThresholdText.Text = $"{(int)(semanticThresholdSlider.Value * 100)}%";
        };

        var tfidfThresholdText = new TextBlock
        {
            Text = $"{(int)(OllamaSettings.GraphTfidfThreshold * 100)}%",
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"],
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var tfidfThresholdSlider = new Slider
        {
            Minimum = 0,
            Maximum = 1,
            Value = OllamaSettings.GraphTfidfThreshold,
            StepFrequency = 0.05,
            IsEnabled = aiToggle.IsOn
        };
        tfidfThresholdSlider.ValueChanged += (s, e) =>
        {
            tfidfThresholdText.Text = $"{(int)(tfidfThresholdSlider.Value * 100)}%";
        };

        var graphSection = CreateGraphSection(
            showExplicitLinksCheckbox,
            showSemanticLinksCheckbox,
            semanticThresholdSlider,
            semanticThresholdText,
            tfidfThresholdSlider,
            tfidfThresholdText);

        aiToggle.Toggled += (s, e) =>
        {
            ollamaSection.Visibility = aiToggle.IsOn ? Visibility.Visible : Visibility.Collapsed;
            showSemanticLinksCheckbox.IsEnabled = aiToggle.IsOn;
            semanticThresholdSlider.IsEnabled = aiToggle.IsOn;
            tfidfThresholdSlider.IsEnabled = aiToggle.IsOn;
        };

        var content = new StackPanel
        {
            Spacing = 20,
            Width = 400,
            Children =
            {
                CreateSectionHeader("Settings"),
                aiToggle,
                ollamaSection,
                graphSection,
                CreateButtonRow(aiToggle, ollamaUrlBox, ollamaModelBox, embeddingModelBox,
                    showExplicitLinksCheckbox, showSemanticLinksCheckbox,
                    semanticThresholdSlider, tfidfThresholdSlider)
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

    private static StackPanel CreateSection(string title, StackPanel[] fields)
    {
        var section = new StackPanel { Spacing = 12 };

        section.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["TextSecondary"]
        });

        foreach (var field in fields)
            section.Children.Add(field);

        return section;
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
            BorderThickness = new Thickness(1)
        };
        textBox.Resources["TextControlBackgroundPointerOver"] = Application.Current.Resources["AppBackground"];
        textBox.Resources["TextControlBackgroundFocused"] = Application.Current.Resources["AppBackground"];
        textBox.Resources["TextControlBorderBrushPointerOver"] = Application.Current.Resources["Gray600"];
        textBox.Resources["TextControlBorderBrushFocused"] = Application.Current.Resources["Primary"];
        return textBox;
    }

    private Grid CreateButtonRow(
        ToggleSwitch aiToggle, TextBox urlBox, TextBox modelBox, TextBox embeddingBox,
        CheckBox explicitLinksCheckbox, CheckBox semanticLinksCheckbox,
        Slider semanticSlider, Slider tfidfSlider)
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

        saveButton.Click += (s, e) =>
        {
            SaveSettings(aiToggle, urlBox, modelBox, embeddingBox,
                explicitLinksCheckbox, semanticLinksCheckbox,
                semanticSlider, tfidfSlider);
            _popup!.IsOpen = false;
        };

        cancelButton.Click += (s, e) => _popup!.IsOpen = false;

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

    private static void SaveSettings(
        ToggleSwitch aiToggle, TextBox urlBox, TextBox modelBox, TextBox embeddingBox,
        CheckBox explicitLinksCheckbox, CheckBox semanticLinksCheckbox,
        Slider semanticSlider, Slider tfidfSlider)
    {
        OllamaSettings.AiEnabled = aiToggle.IsOn;

        var url = urlBox.Text?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(url))
            OllamaSettings.OllamaUrl = url;

        var model = modelBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(model))
            OllamaSettings.OllamaModel = model;

        var embeddingModel = embeddingBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(embeddingModel))
            OllamaSettings.EmbeddingModel = embeddingModel;

        OllamaSettings.GraphShowExplicitLinks = explicitLinksCheckbox.IsChecked ?? true;
        OllamaSettings.GraphShowSemanticLinks = semanticLinksCheckbox.IsChecked ?? true;
        OllamaSettings.GraphSemanticThreshold = (float)semanticSlider.Value;
        OllamaSettings.GraphTfidfThreshold = (float)tfidfSlider.Value;
    }
}
