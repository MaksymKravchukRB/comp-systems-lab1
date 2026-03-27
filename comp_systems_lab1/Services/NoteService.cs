using Microsoft.EntityFrameworkCore;
using lab1.Data;
using lab1.Models;

namespace lab1.Services;

public class NoteService : INoteService
{
    private readonly ApplicationDbContext _context;

    public NoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Note>> GetAllNotesAsync()
    {
        return await _context.Notes
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        return await _context.Notes.FindAsync(id);
    }

    public async Task<Note> CreateNoteAsync(string title, string content)
    {
        var note = new Note
        {
            Title = title,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }
}