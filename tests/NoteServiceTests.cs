using Xunit;
using Microsoft.EntityFrameworkCore;
using lab1.Data;
using lab1.Models;
using lab1.Services;

namespace NotesService.Tests;

public class NoteServiceTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldAddNoteToDatabase()
    {
        var context = GetDbContext();
        var service = new NoteService(context);
        var title = "Test Title";
        var content = "Test Content";

        var note = await service.CreateNoteAsync(title, content);

        Assert.NotEqual(0, note.Id);
        Assert.Equal(title, note.Title);
        Assert.Equal(content, note.Content);
        Assert.True((DateTime.UtcNow - note.CreatedAt).TotalSeconds < 5);
        Assert.Equal(1, await context.Notes.CountAsync());
    }

    [Fact]
    public async Task GetAllNotesAsync_ShouldReturnNotesOrderedByCreatedAtDesc()
    {
        var context = GetDbContext();
        var service = new NoteService(context);
        var note1 = new Note { Title = "First", Content = "A", CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var note2 = new Note { Title = "Second", Content = "B", CreatedAt = DateTime.UtcNow };
        context.Notes.AddRange(note1, note2);
        await context.SaveChangesAsync();

        var notes = await service.GetAllNotesAsync();

        Assert.Equal(2, notes.Count());
        Assert.Equal("Second", notes.First().Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_ExistingId_ReturnsNote()
    {
        var context = GetDbContext();
        var service = new NoteService(context);
        var note = new Note { Title = "Existing", Content = "Content" };
        context.Notes.Add(note);
        await context.SaveChangesAsync();
        var id = note.Id;

        var result = await service.GetNoteByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Existing", result.Title);
    }

    [Fact]
    public async Task GetNoteByIdAsync_NonExistingId_ReturnsNull()
    {
        var context = GetDbContext();
        var service = new NoteService(context);

        var result = await service.GetNoteByIdAsync(999);

        Assert.Null(result);
    }
}