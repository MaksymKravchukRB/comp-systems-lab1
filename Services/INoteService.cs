using lab1.Models;

namespace lab1.Services;

public interface INoteService
{
    Task<IEnumerable<Note>> GetAllNotesAsync();
    Task<Note?> GetNoteByIdAsync(int id);
    Task<Note> CreateNoteAsync(string title, string content);
}