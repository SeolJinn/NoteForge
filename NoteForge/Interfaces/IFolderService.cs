using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface IFolderService
{
    Folder BuildFolderTree(string vaultPath);
    OperationResult CreateFolder(string parentPath, string name);
    OperationResult RenameFolder(string folderPath, string newName);
    OperationResult DeleteFolder(string folderPath);
    bool IsFolderEmpty(string folderPath);
    void SaveExpandedState(Folder rootFolder);
    void LoadExpandedState(Folder rootFolder);
    string GetNewPath(string oldPath, string newParentPath);
}
