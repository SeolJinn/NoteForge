using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using NoteForge.Handlers.Graph;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed partial class GraphViewControl : UserControl
{
    public event EventHandler<Note>? NodeClicked;

    private readonly IMediator _mediator;
    private readonly ILogger<GraphViewControl> _logger;
    private readonly GraphPhysicsSimulator _simulator = new();
    private GraphData _graphData = new();
    private GraphSettings _settings = new();
    private readonly Dictionary<GraphNode, Ellipse> _nodeVisuals = [];
    private readonly Dictionary<GraphEdge, Line> _edgeVisuals = [];
    private readonly List<(INotifyPropertyChanged Source, PropertyChangedEventHandler Handler)> _propertyChangedSubscriptions = [];
    private GraphNode? _draggedNode;
    private const double NodeRadius = 8.0;

    public GraphViewControl()
    {
        InitializeComponent();
        _mediator = App.Mediator;
        _logger = App.Services.GetRequiredService<ILogger<GraphViewControl>>();
        _simulator.Tick += OnSimulatorTick;

        SemanticThresholdSlider.ValueChanged += (s, e) =>
        {
            SemanticThresholdText.Text = $"{(int)(SemanticThresholdSlider.Value * 100)}%";
        };
        TfidfThresholdSlider.ValueChanged += (s, e) =>
        {
            TfidfThresholdText.Text = $"{(int)(TfidfThresholdSlider.Value * 100)}%";
        };

        SemanticThresholdText.Text = $"{(int)(SemanticThresholdSlider.Value * 100)}%";
        TfidfThresholdText.Text = $"{(int)(TfidfThresholdSlider.Value * 100)}%";
    }

    public async Task LoadGraphAsync(List<Note> notes)
    {
        LoadingOverlay.Visibility = Visibility.Visible;

        try
        {
            UpdateSettingsFromUI();
            _graphData = await _mediator.Send(new BuildGraphQueryRequest(notes, _settings));

            _simulator.Configure(_graphData, _settings);
            _simulator.UpdateCanvasSize(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
            _simulator.InitializeNodePositions(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
            RenderGraph();
            _simulator.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load graph");
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void OnSimulatorTick(object? sender, EventArgs e)
    {
        UpdateVisuals();
    }

    private void UnsubscribePropertyChangedHandlers()
    {
        foreach (var (source, handler) in _propertyChangedSubscriptions)
        {
            source.PropertyChanged -= handler;
        }
        _propertyChangedSubscriptions.Clear();
    }

    private void RenderGraph()
    {
        UnsubscribePropertyChangedHandlers();
        NodesCanvas.Children.Clear();
        EdgesCanvas.Children.Clear();
        _nodeVisuals.Clear();
        _edgeVisuals.Clear();

        foreach (var edge in _graphData.Edges)
        {
            var line = CreateEdgeVisual(edge);
            _edgeVisuals[edge] = line;
            EdgesCanvas.Children.Add(line);
        }

        foreach (var node in _graphData.Nodes)
        {
            var ellipse = CreateNodeVisual(node);
            _nodeVisuals[node] = ellipse;
            NodesCanvas.Children.Add(ellipse);

            var textBlock = CreateNodeLabel(node);
            NodesCanvas.Children.Add(textBlock);
        }
    }

    private Line CreateEdgeVisual(GraphEdge edge)
    {
        var line = new Line
        {
            Stroke = edge.Type switch
            {
                EdgeType.Explicit => new SolidColorBrush(Windows.UI.Color.FromArgb(255, 79, 195, 247)),
                EdgeType.Semantic => new SolidColorBrush(Colors.MediumPurple),
                EdgeType.Hybrid => new SolidColorBrush(Colors.Cyan),
                _ => new SolidColorBrush(Colors.Gray)
            },
            StrokeThickness = edge.Type == EdgeType.Explicit ? 2.0 : 1.0,
            Opacity = Math.Clamp(edge.Strength, 0.3, 0.9)
        };

        PropertyChangedEventHandler handler = (s, e) =>
        {
            if (e.PropertyName is nameof(GraphEdge.IsVisible))
                line.Visibility = edge.IsVisible ? Visibility.Visible : Visibility.Collapsed;
        };
        edge.PropertyChanged += handler;
        _propertyChangedSubscriptions.Add((edge, handler));

        return line;
    }

    private Ellipse CreateNodeVisual(GraphNode node)
    {
        var ellipse = new Ellipse
        {
            Width = NodeRadius * 2,
            Height = NodeRadius * 2,
            Fill = new SolidColorBrush(Colors.LightBlue),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };

        ellipse.PointerEntered += (s, e) =>
        {
            node.IsHovered = true;
            ellipse.Fill = new SolidColorBrush(Colors.Yellow);
        };

        ellipse.PointerExited += (s, e) =>
        {
            if (!node.IsSelected)
            {
                node.IsHovered = false;
                ellipse.Fill = new SolidColorBrush(Colors.LightBlue);
            }
        };

        ellipse.PointerPressed += (s, e) =>
        {
            _draggedNode = node;
            node.IsDragging = true;
            _simulator.SetDragState(true);
            e.Handled = true;
        };

        ellipse.Tapped += (s, e) =>
        {
            if (!node.IsDragging)
            {
                NodeClicked?.Invoke(this, node.Note);
            }
            node.IsDragging = false;
        };

        return ellipse;
    }

    private TextBlock CreateNodeLabel(GraphNode node)
    {
        var textBlock = new TextBlock
        {
            Text = node.DisplayName,
            FontSize = 11,
            Foreground = new SolidColorBrush(Colors.White)
        };

        PropertyChangedEventHandler handler = (s, e) =>
        {
            if (e.PropertyName is nameof(GraphNode.X))
            {
                Canvas.SetLeft(textBlock, node.X - textBlock.ActualWidth / 2);
            }
            else if (e.PropertyName is nameof(GraphNode.Y))
            {
                Canvas.SetTop(textBlock, node.Y + NodeRadius + 4);
            }
        };
        node.PropertyChanged += handler;
        _propertyChangedSubscriptions.Add((node, handler));

        Canvas.SetLeft(textBlock, node.X);
        Canvas.SetTop(textBlock, node.Y + NodeRadius + 4);

        return textBlock;
    }

    private void UpdateVisuals()
    {
        foreach (var (node, ellipse) in _nodeVisuals)
        {
            Canvas.SetLeft(ellipse, node.X - NodeRadius);
            Canvas.SetTop(ellipse, node.Y - NodeRadius);
        }

        foreach (var (edge, line) in _edgeVisuals)
        {
            line.X1 = edge.Source.X;
            line.Y1 = edge.Source.Y;
            line.X2 = edge.Target.X;
            line.Y2 = edge.Target.Y;
        }
    }

    private void OnCanvasPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedNode is not null)
        {
            var position = e.GetCurrentPoint(GraphCanvas).Position;
            _draggedNode.X = position.X;
            _draggedNode.Y = position.Y;

            if (!_simulator.IsRunning)
                UpdateVisuals();
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedNode is not null)
        {
            _draggedNode.IsDragging = false;
            _draggedNode = null;
            _simulator.SetDragState(false);
            _simulator.Start();
        }
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _simulator.UpdateCanvasSize(e.NewSize.Width, e.NewSize.Height);

        if (_graphData.Nodes.Count > 0 && e.PreviousSize.Width > 0 && e.PreviousSize.Height > 0)
        {
            var scaleX = e.NewSize.Width / e.PreviousSize.Width;
            var scaleY = e.NewSize.Height / e.PreviousSize.Height;

            foreach (var node in _graphData.Nodes)
            {
                node.X *= scaleX;
                node.Y *= scaleY;
            }
        }
    }

    private async void OnSettingsChanged(object sender, RoutedEventArgs e)
    {
        UpdateSettingsFromUI();

        List<Note> notes = [.. _graphData.Nodes.Select(n => n.Note)];
        if (notes.Count > 0)
        {
            await LoadGraphAsync(notes);
        }
    }

    private void UpdateSettingsFromUI()
    {
        if (ShowExplicitLinksCheckbox is not null)
            _settings.ShowExplicitLinks = ShowExplicitLinksCheckbox.IsChecked ?? true;

        if (ShowSemanticLinksCheckbox is not null)
            _settings.ShowSemanticLinks = ShowSemanticLinksCheckbox.IsChecked ?? true;

        if (SemanticThresholdSlider is not null)
            _settings.SemanticThreshold = (float)SemanticThresholdSlider.Value;

        if (TfidfThresholdSlider is not null)
            _settings.TfidfThreshold = (float)TfidfThresholdSlider.Value;
    }

    private void OnResetLayoutClicked(object sender, RoutedEventArgs e)
    {
        _simulator.InitializeNodePositions(GraphCanvas.ActualWidth, GraphCanvas.ActualHeight);
        _simulator.Start();
    }

    public void Cleanup()
    {
        _simulator.Tick -= OnSimulatorTick;
        _simulator.Stop();
        UnsubscribePropertyChangedHandlers();
        _graphData.Clear();
        _nodeVisuals.Clear();
        _edgeVisuals.Clear();
    }
}
