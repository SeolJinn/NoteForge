using System.Collections.Generic;
using System.Threading.Tasks;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface INoteService
{
    string CurrentNotebookPath { get; }
    string CurrentVaultName { get; }
    bool IsConfigured { get; }
    Task<string?> PickFolderAsync();
    void SetVaultPath(string path);
    List<VaultInfo> GetRecentVaults();
}