using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace NoteForge.Interfaces;

public interface IFolderDialogService
{
    Task<string?> ShowCreateFolderDialogAsync(XamlRoot xamlRoot);
    Task<string?> ShowRenameFolderDialogAsync(string currentName, XamlRoot xamlRoot);
    Task<bool> ShowDeleteFolderDialogAsync(string folderName, XamlRoot xamlRoot);
}