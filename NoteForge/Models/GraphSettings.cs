using System;

namespace NoteForge.Models;

public partial class GraphSettings : ObservableObject
{
    private float _semanticThreshold = 0.3f;
    private float _tfidfThreshold = 0.2f;
    private bool _showExplicitLinks = true;
    private bool _showSemanticLinks = true;
    private float _repulsionStrength = 100f;
    private float _attractionStrength = 0.01f;
    private float _centerGravity = 0.05f;

    public float SemanticThreshold
    {
        get => _semanticThreshold;
        set => SetProperty(ref _semanticThreshold, Math.Clamp(value, 0f, 1f));
    }

    public float TfidfThreshold
    {
        get => _tfidfThreshold;
        set => SetProperty(ref _tfidfThreshold, Math.Clamp(value, 0f, 1f));
    }

    public bool ShowExplicitLinks
    {
        get => _showExplicitLinks;
        set => SetProperty(ref _showExplicitLinks, value);
    }

    public bool ShowSemanticLinks
    {
        get => _showSemanticLinks;
        set => SetProperty(ref _showSemanticLinks, value);
    }

    public float RepulsionStrength
    {
        get => _repulsionStrength;
        set => SetProperty(ref _repulsionStrength, value);
    }

    public float AttractionStrength
    {
        get => _attractionStrength;
        set => SetProperty(ref _attractionStrength, value);
    }

    public float CenterGravity
    {
        get => _centerGravity;
        set => SetProperty(ref _centerGravity, value);
    }
}
