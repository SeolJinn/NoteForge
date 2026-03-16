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
    private GraphData _graphData = new();
    private GraphSettings _settings = new();
    private readonly Dictionary<GraphNode, Ellipse> _nodeVisuals = [];
    private readonly Dictionary<GraphEdge, Line> _edgeVisuals = [];
    private readonly List<(INotifyPropertyChanged Source, PropertyChangedEventHandler Handler)> _propertyChangedSubscriptions = [];
    private GraphNode? _draggedNode;
    private bool _isSimulationRunning;
    private DispatcherTimer? _simulationTimer;
    private const double NodeRadius = 8.0;
    private const double SimulationDamping = 0.8;
    private const double DefaultCanvasCenter = 400.0;
    private const double DefaultSpreadRadius = 240.0;

    public GraphViewControl()
    {
        InitializeComponent();
        _mediator = App.Mediator;
        _logger = App.Services.GetRequiredService<ILogger<GraphViewControl>>();

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

            InitializeNodePositions();
            RenderGraph();
            StartPhysicsSimulation();
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

    private void InitializeNodePositions()
    {
        var random = new Random();
        var centerX = GraphCanvas.ActualWidth / 2;
        var centerY = GraphCanvas.ActualHeight / 2;
        var spreadRadius = Math.Min(centerX, centerY) * 0.6;

        if (centerX is 0 || centerY is 0)
        {
            centerX = DefaultCanvasCenter;
            centerY = DefaultCanvasCenter;
            spreadRadius = DefaultSpreadRadius;
        }

        foreach (var node in _graphData.Nodes)
        {
            var angle = random.NextDouble() * 2 * Math.PI;
            var distance = random.NextDouble() * spreadRadius;

            node.X = centerX + Math.Cos(angle) * distance;
            node.Y = centerY + Math.Sin(angle) * distance;
            node.VelocityX = 0;
            node.VelocityY = 0;
        }
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
                EdgeType.Explicit => new SolidColorBrush(Colors.DodgerBlue),
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

    private void StartPhysicsSimulation()
    {
        if (_isSimulationRunning)
            return;

        _isSimulationRunning = true;
        _simulationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _simulationTimer.Tick += OnSimulationTick;
        _simulationTimer.Start();
    }

    private void StopPhysicsSimulation()
    {
        _isSimulationRunning = false;
        _simulationTimer?.Stop();
    }

    private void OnSimulationTick(object? sender, object e)
    {
        ApplyForces();
        UpdatePositions();
        UpdateVisuals();

        var totalEnergy = _graphData.Nodes.Sum(n =>
            Math.Sqrt(n.VelocityX * n.VelocityX + n.VelocityY * n.VelocityY));

        if (totalEnergy < 0.5 && _draggedNode is null)
        {
            StopPhysicsSimulation();
        }
    }

    private void ApplyForces()
    {
        var centerX = GraphCanvas.ActualWidth / 2;
        var centerY = GraphCanvas.ActualHeight / 2;

        if (centerX is 0 || centerY is 0)
        {
            centerX = DefaultCanvasCenter;
            centerY = DefaultCanvasCenter;
        }

        foreach (var node in _graphData.Nodes)
        {
            if (node.IsDragging)
                continue;

            node.VelocityX = 0;
            node.VelocityY = 0;

            foreach (var other in _graphData.Nodes)
            {
                if (node == other)
                    continue;

                var dx = node.X - other.X;
                var dy = node.Y - other.Y;
                var distanceSquared = Math.Max(dx * dx + dy * dy, 1);
                var repulsionForce = _settings.RepulsionStrength / distanceSquared;

                node.VelocityX += dx * repulsionForce;
                node.VelocityY += dy * repulsionForce;
            }

            foreach (var edge in _graphData.Edges)
            {
                GraphNode? connectedNode = null;

                if (edge.Source == node)
                    connectedNode = edge.Target;
                else if (edge.Target == node)
                    connectedNode = edge.Source;

                if (connectedNode is not null && edge.IsVisible)
                {
                    var dx = connectedNode.X - node.X;
                    var dy = connectedNode.Y - node.Y;
                    var attractionForce = _settings.AttractionStrength * edge.Strength;

                    node.VelocityX += dx * attractionForce;
                    node.VelocityY += dy * attractionForce;
                }
            }

            var centerDx = centerX - node.X;
            var centerDy = centerY - node.Y;
            node.VelocityX += centerDx * _settings.CenterGravity;
            node.VelocityY += centerDy * _settings.CenterGravity;
        }
    }

    private void UpdatePositions()
    {
        var maxX = GraphCanvas.ActualWidth > 0 ? GraphCanvas.ActualWidth - NodeRadius : 800;
        var maxY = GraphCanvas.ActualHeight > 0 ? GraphCanvas.ActualHeight - NodeRadius : 600;

        foreach (var node in _graphData.Nodes)
        {
            if (node.IsDragging)
                continue;

            node.X += node.VelocityX * SimulationDamping;
            node.Y += node.VelocityY * SimulationDamping;

            node.X = Math.Clamp(node.X, NodeRadius, maxX);
            node.Y = Math.Clamp(node.Y, NodeRadius, maxY);
        }
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

            if (!_isSimulationRunning)
                UpdateVisuals();
        }
    }

    private void OnCanvasPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_draggedNode is not null)
        {
            _draggedNode.IsDragging = false;
            _draggedNode = null;
            StartPhysicsSimulation();
        }
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e)
    {
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
        InitializeNodePositions();
        StartPhysicsSimulation();
    }

    public void Cleanup()
    {
        StopPhysicsSimulation();
        UnsubscribePropertyChangedHandlers();
        _graphData.Clear();
        _nodeVisuals.Clear();
        _edgeVisuals.Clear();
    }
}
