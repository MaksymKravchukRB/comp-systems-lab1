using Microsoft.AspNetCore.Mvc;
using lab1.Models;
using lab1.Services;
using System.Text;

namespace lab1.Controllers;

[ApiController]
[Route("[controller]")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    // GET /notes
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var notes = await _noteService.GetAllNotesAsync();

        if (Request.Headers.Accept.ToString().Contains("text/html"))
        {
            // Return simple HTML table
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine("<h1>All Notes</h1>");
            sb.AppendLine("<table border='1'><tr><th>ID</th><th>Title</th></tr>");
            foreach (var note in notes)
            {
                sb.AppendLine($"<tr><td>{note.Id}</td><td>{note.Title}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</body></html>");
            return Content(sb.ToString(), "text/html");
        }

        // Default JSON
        var list = notes.Select(n => new { n.Id, n.Title });
        return Ok(list);
    }

    // POST /notes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNoteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Title and Content are required.");
        }

        var note = await _noteService.CreateNoteAsync(request.Title, request.Content);

        if (Request.Headers.Accept.ToString().Contains("text/html"))
        {
            // Redirect to the details page (GET /notes/{id}) as HTML
            return Redirect($"/notes/{note.Id}");
        }

        return CreatedAtAction(nameof(GetById), new { id = note.Id }, note);
    }

    // GET /notes/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var note = await _noteService.GetNoteByIdAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        if (Request.Headers.Accept.ToString().Contains("text/html"))
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><body>");
            sb.AppendLine($"<h1>Note #{note.Id}</h1>");
            sb.AppendLine($"<p><strong>Title:</strong> {note.Title}</p>");
            sb.AppendLine($"<p><strong>Created:</strong> {note.CreatedAt}</p>");
            sb.AppendLine($"<p><strong>Content:</strong> {note.Content}</p>");
            sb.AppendLine("<a href='/notes'>Back to list</a>");
            sb.AppendLine("</body></html>");
            return Content(sb.ToString(), "text/html");
        }

        return Ok(new { note.Id, note.Title, note.Content, note.CreatedAt });
    }
}

public class CreateNoteRequest
{
    public required string Title { get; set; }
    public required string Content { get; set; }
}