namespace NoteForge.Models;

public partial class GraphNode : ObservableObject
{
    private double _x;
    private double _y;
    private double _velocityX;
    private double _velocityY;
    private bool _isSelected;
    private bool _isDragging;
    private bool _isHovered;

    public Note Note { get; init; } = null!;

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double VelocityX
    {
        get => _velocityX;
        set => SetProperty(ref _velocityX, value);
    }

    public double VelocityY
    {
        get => _velocityY;
        set => SetProperty(ref _velocityY, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsDragging
    {
        get => _isDragging;
        set => SetProperty(ref _isDragging, value);
    }

    public bool IsHovered
    {
        get => _isHovered;
        set => SetProperty(ref _isHovered, value);
    }

    public string FilePath => Note.FilePath;
    public string DisplayName => Note.Filename;
}
