using System.Collections.Generic;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface ISectionService
{
    void AddFavorite(string noteFilePath);
    void RemoveFavorite(string noteFilePath);
    bool IsFavorite(string noteFilePath);
    List<string> GetFavorites();
    NoteSection? GetFavoritesSection(List<Note> allNotes);
}
