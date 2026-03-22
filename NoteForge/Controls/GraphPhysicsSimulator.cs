using System;
using System.Linq;
using Microsoft.UI.Xaml;
using NoteForge.Models;

namespace NoteForge.Controls;

public sealed class GraphPhysicsSimulator
{
    private const double SimulationDamping = 0.8;
    private const double NodeRadius = 8.0;
    private const double DefaultCanvasCenter = 400.0;
    private const double DefaultSpreadRadius = 240.0;
    private const double EnergyThreshold = 0.5;
    private const int TickIntervalMs = 16;

    private GraphData _graphData = new();
    private GraphSettings _settings = new();
    private DispatcherTimer? _simulationTimer;
    private bool _isSimulationRunning;
    private bool _hasNodeBeingDragged;
    private double _canvasWidth;
    private double _canvasHeight;

    public event EventHandler? Tick;

    public bool IsRunning => _isSimulationRunning;

    public void Configure(GraphData graphData, GraphSettings settings)
    {
        _graphData = graphData;
        _settings = settings;
    }

    public void SetDragState(bool isDragging)
    {
        _hasNodeBeingDragged = isDragging;
    }

    public void InitializeNodePositions(double canvasWidth, double canvasHeight)
    {
        var random = new Random();
        var centerX = canvasWidth / 2;
        var centerY = canvasHeight / 2;
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

    public void Start()
    {
        if (_isSimulationRunning)
            return;

        _isSimulationRunning = true;
        _simulationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(TickIntervalMs)
        };
        _simulationTimer.Tick += OnSimulationTick;
        _simulationTimer.Start();
    }

    public void Stop()
    {
        _isSimulationRunning = false;
        _simulationTimer?.Stop();
    }

    private void OnSimulationTick(object? sender, object e)
    {
        ApplyForces();
        UpdatePositions();
        Tick?.Invoke(this, EventArgs.Empty);

        var totalEnergy = _graphData.Nodes.Sum(n =>
            Math.Sqrt(n.VelocityX * n.VelocityX + n.VelocityY * n.VelocityY));

        if (totalEnergy < EnergyThreshold && !_hasNodeBeingDragged)
        {
            Stop();
        }
    }

    private void ApplyForces()
    {
        var centerX = _canvasWidth > 0 ? _canvasWidth / 2 : DefaultCanvasCenter;
        var centerY = _canvasHeight > 0 ? _canvasHeight / 2 : DefaultCanvasCenter;

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
        var maxX = _canvasWidth > 0 ? _canvasWidth - NodeRadius : 800;
        var maxY = _canvasHeight > 0 ? _canvasHeight - NodeRadius : 600;

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

    public void UpdateCanvasSize(double width, double height)
    {
        _canvasWidth = width;
        _canvasHeight = height;
    }
}
