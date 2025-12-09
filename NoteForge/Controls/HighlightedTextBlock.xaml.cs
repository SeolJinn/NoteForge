using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

using Windows.UI;

namespace NoteForge.Controls;

public sealed partial class HighlightedTextBlock : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty HighlightProperty =
        DependencyProperty.Register(
            nameof(Highlight),
            typeof(string),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty TextFontSizeProperty =
        DependencyProperty.Register(
            nameof(TextFontSize),
            typeof(double),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(11.0, OnFontSizeChanged));

    public static readonly DependencyProperty TextForegroundProperty =
        DependencyProperty.Register(
            nameof(TextForeground),
            typeof(Brush),
            typeof(HighlightedTextBlock),
            new PropertyMetadata(null, OnForegroundChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Highlight
    {
        get => (string)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    public double TextFontSize
    {
        get => (double)GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public Brush TextForeground
    {
        get => (Brush)GetValue(TextForegroundProperty);
        set => SetValue(TextForegroundProperty, value);
    }

    public HighlightedTextBlock()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightedTextBlock control)
        {
            control.UpdateContent();
        }
    }

    private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightedTextBlock control)
        {
            control.ContentBlock.FontSize = (double)e.NewValue;
            control.UpdateContent();
        }
    }

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightedTextBlock control)
        {
            control.UpdateContent();
        }
    }

    private void UpdateContent()
    {
        ContentBlock.Blocks.Clear();

        var paragraph = new Paragraph();
        var text = Text ?? string.Empty;
        var highlight = Highlight ?? string.Empty;

        if (string.IsNullOrEmpty(highlight) || string.IsNullOrEmpty(text))
        {
            var run = new Run { Text = text };
            if (TextForeground != null)
            {
                run.Foreground = TextForeground;
            }
            paragraph.Inlines.Add(run);
        }
        else
        {
            var index = 0;
            var comparison = StringComparison.OrdinalIgnoreCase;

            while (index < text.Length)
            {
                var matchIndex = text.IndexOf(highlight, index, comparison);

                if (matchIndex == -1)
                {
                    var remainingText = text.Substring(index);
                    var run = new Run { Text = remainingText };
                    if (TextForeground != null)
                    {
                        run.Foreground = TextForeground;
                    }
                    paragraph.Inlines.Add(run);
                    break;
                }

                if (matchIndex > index)
                {
                    var beforeMatch = text.Substring(index, matchIndex - index);
                    var run = new Run { Text = beforeMatch };
                    if (TextForeground != null)
                    {
                        run.Foreground = TextForeground;
                    }
                    paragraph.Inlines.Add(run);
                }

                var matchedText = text.Substring(matchIndex, highlight.Length);
                var highlightedRun = new Run
                {
                    Text = matchedText,
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 204, 0)),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                paragraph.Inlines.Add(highlightedRun);

                index = matchIndex + highlight.Length;
            }
        }

        ContentBlock.Blocks.Add(paragraph);
    }
}
