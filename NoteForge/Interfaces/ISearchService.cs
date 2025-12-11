using System.Collections.Generic;
using NoteForge.Models;

namespace NoteForge.Interfaces;

public interface ISearchService
{
    IEnumerable<Note> SearchByName(IEnumerable<Note> notes, string query);
}