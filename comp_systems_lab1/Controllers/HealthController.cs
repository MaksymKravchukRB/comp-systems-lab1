using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using lab1.Data;

namespace lab1.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public HealthController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("alive")]
    public IActionResult Alive()
    {
        return Ok("OK");
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Try to execute a simple query to check DB connection
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            return Ok("OK");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Database connection failed: {ex.Message}");
        }
    }
}