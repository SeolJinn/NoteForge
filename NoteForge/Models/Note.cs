using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NoteForge.Models;

public partial class Note : ObservableObject
{
    private string _filename = string.Empty;
    private string _text = string.Empty;
    private DateTime _date;
    private string _filePath = string.Empty;
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string Filename
    {
        get => _filename;
        set => SetProperty(ref _filename, value);
    }

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    public DateTime Date
    {
        get => _date;
        set => SetProperty(ref _date, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public static Note FromFile(string filePath)
    {
        return new Note
        {
            FilePath = filePath,
            Filename = Path.GetFileNameWithoutExtension(filePath),
            Text = File.ReadAllText(filePath),
            Date = File.GetLastWriteTime(filePath)
        };
    }

    public static async Task<Note> FromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return new Note
        {
            FilePath = filePath,
            Filename = Path.GetFileNameWithoutExtension(filePath),
            Text = await File.ReadAllTextAsync(filePath, cancellationToken),
            Date = File.GetLastWriteTime(filePath)
        };
    }
}