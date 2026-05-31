using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.Extensions.Logging;
using NoteForge.Helpers;
using NoteForge.Interfaces;

namespace NoteForge.Handlers.Notes;

public class ImportFolderCommandHandler(
    INoteService noteService,
    ISemanticSearchStrategy semanticSearch,
    ILogger<ImportFolderCommandHandler> logger) : IRequestHandler<ImportFolderCommandRequest, ImportResult>
{
    public async ValueTask<ImportResult> Handle(ImportFolderCommandRequest request, CancellationToken cancellationToken)
    {
        if (!noteService.IsConfigured)
            return new ImportResult(false, 0, 0, null, "No vault is selected.");

        if (string.IsNullOrWhiteSpace(request.SourceFolderPath) || !Directory.Exists(request.SourceFolderPath))
            return new ImportResult(false, 0, 0, null, "The selected folder no longer exists.");

        try
        {
            return await Task.Run(() => ImportTree(request.SourceFolderPath), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to import folder {Source}", request.SourceFolderPath);
            return new ImportResult(false, 0, 0, null, "Failed to import the folder.");
        }
    }

    private ImportResult ImportTree(string sourceFolder)
    {
        var vaultRoot = noteService.CurrentNotebookPath;
        var baseName = Path.GetFileName(sourceFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "Imported";

        var destRoot = Path.Combine(vaultRoot, baseName);
        var counter = 2;
        while (Directory.Exists(destRoot) || File.Exists(destRoot))
        {
            destRoot = Path.Combine(vaultRoot, $"{baseName} {counter}");
            counter++;
        }

        if (!PathValidator.IsWithinVault(destRoot, vaultRoot))
            return new ImportResult(false, 0, 0, null, "Target folder is outside the vault.");

        var createdFolderName = Path.GetFileName(destRoot);
        var imported = 0;
        var skipped = 0;

        foreach (var sourceFile in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            if (!TextImportConverter.IsSupported(sourceFile))
            {
                skipped++;
                continue;
            }

            try
            {
                var bytes = File.ReadAllBytes(sourceFile);
                if (!TextImportConverter.TryConvert(bytes, out var text))
                {
                    skipped++;
                    continue;
                }

                var relative = Path.GetRelativePath(sourceFolder, sourceFile);
                var targetFile = Path.Combine(destRoot, Path.ChangeExtension(relative, ".md"));

                if (!PathValidator.IsWithinVault(targetFile, vaultRoot))
                {
                    skipped++;
                    continue;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                var tempPath = targetFile + ".tmp";
                File.WriteAllText(tempPath, text);
                File.Move(tempPath, targetFile, overwrite: true);
                imported++;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Skipped file during import: {File}", sourceFile);
                skipped++;
            }
        }

        if (imported is 0)
        {
            if (Directory.Exists(destRoot))
            {
                try { Directory.Delete(destRoot, recursive: true); }
                catch (Exception ex) { logger.LogWarning(ex, "Failed to clean up empty import folder {Dest}", destRoot); }
            }
            return new ImportResult(true, 0, skipped, null, null);
        }

        semanticSearch.InvalidateIndex();
        return new ImportResult(true, imported, skipped, createdFolderName, null);
    }
}

public sealed record ImportFolderCommandRequest(string SourceFolderPath) : IRequest<ImportResult>;

public sealed record ImportResult(bool Success, int Imported, int Skipped, string? CreatedFolderName, string? ErrorMessage);
