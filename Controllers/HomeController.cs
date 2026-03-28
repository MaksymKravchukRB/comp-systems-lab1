using Microsoft.AspNetCore.Mvc;

namespace lab1.Controllers;

public class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Only accept text/html
        if (Request.Headers.Accept.ToString().Contains("text/html"))
        {
            var html = @"
            <html>
            <body>
                <h1>Notes Service API</h1>
                <ul>
                    <li>GET /notes - List all notes (id, title)</li>
                    <li>POST /notes - Create a new note (JSON: title, content)</li>
                    <li>GET /notes/{id} - Get full note details</li>
                </ul>
                <p>Health endpoints: /health/alive, /health/ready</p>
            </body>
            </html>";
            return Content(html, "text/html");
        }

        // Fallback – though spec says only HTML accepted
        return StatusCode(406, "Only text/html is accepted for the root endpoint.");
    }
}