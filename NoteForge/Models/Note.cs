using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NoteForge.Models;

public class Note : INotifyPropertyChanged
{
    private string _filename = string.Empty;
    private string _text = string.Empty;
    private DateTime _date;
    private string _filePath = string.Empty;
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public string Filename
    {
        get => _filename;
        set
        {
            if (_filename != value)
            {
                _filename = value;
                OnPropertyChanged();
            }
        }
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime Date
    {
        get => _date;
        set
        {
            if (_date != value)
            {
                _date = value;
                OnPropertyChanged();
            }
        }
    }

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

