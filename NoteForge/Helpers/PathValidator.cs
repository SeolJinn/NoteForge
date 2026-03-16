using System;
using System.IO;

namespace NoteForge.Helpers;

public static class PathValidator
{
    public static bool IsWithinVault(string path, string vaultRoot)
    {
        var fullPath = Path.GetFullPath(path);
        var fullVault = Path.GetFullPath(vaultRoot);
        var fullVaultPrefix = fullVault.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.Equals(fullVault, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(fullVaultPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
