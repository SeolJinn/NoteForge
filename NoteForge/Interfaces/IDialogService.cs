using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace NoteForge.Interfaces;

public interface IDialogService
{
    Task ShowErrorAsync(string message, XamlRoot xamlRoot);
    Task ShowErrorAsync(string title, string message, XamlRoot xamlRoot);
    Task<string?> PickFolderAsync();
}