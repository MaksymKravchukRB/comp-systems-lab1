using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using lab1.Controllers;
using lab1.Services;
using lab1.Models;

namespace NotesService.Tests;

public class NotesControllerTests
{
    private readonly Mock<INoteService> _noteServiceMock;

    public NotesControllerTests()
    {
        _noteServiceMock = new Mock<INoteService>();
    }

    private NotesController CreateController(string acceptHeader = null)
    {
        var controller = new NotesController(_noteServiceMock.Object);
        var httpContext = new DefaultHttpContext();
        if (acceptHeader != null)
        {
            httpContext.Request.Headers["Accept"] = acceptHeader;
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    [Fact]
    public async Task GetAll_WhenAcceptHtml_ReturnsHtmlTable()
    {
        _noteServiceMock.Setup(s => s.GetAllNotesAsync())
            .ReturnsAsync(new List<Note>
            {
                new Note { Id = 1, Title = "Note1", Content = "Content1" },
                new Note { Id = 2, Title = "Note2", Content = "Content2" }
            });

        var controller = CreateController("text/html");
        var result = await controller.GetAll() as ContentResult;

        Assert.NotNull(result);
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("<table", result.Content);
        Assert.Contains("Note1", result.Content);
    }

    [Fact]
    public async Task GetAll_WhenAcceptJson_ReturnsJsonWithIdAndTitle()
    {
        _noteServiceMock.Setup(s => s.GetAllNotesAsync())
            .ReturnsAsync(new List<Note>
            {
                new Note { Id = 1, Title = "Note1", Content = "should be hidden" }
            });

        var controller = CreateController();
        var result = await controller.GetAll() as OkObjectResult;

        Assert.NotNull(result);
        var data = result.Value as IEnumerable<object>;
        Assert.Single(data);
        var first = data?.First();
        var propertyInfos = first?.GetType().GetProperties();
        Assert.Contains(propertyInfos, p => p.Name == "Id");
        Assert.Contains(propertyInfos, p => p.Name == "Title");
        Assert.DoesNotContain(propertyInfos, p => p.Name == "Content");
    }

    [Fact]
    public async Task Create_WithInvalidModel_ReturnsBadRequest()
    {
        var controller = CreateController();
        var request = new CreateNoteRequest { Title = "", Content = "Something" };

        var result = await controller.Create(request) as BadRequestObjectResult;

        Assert.NotNull(result);
        Assert.Equal(400, result.StatusCode);
        _noteServiceMock.Verify(s => s.CreateNoteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAtAction()
    {
        var newNote = new Note { Id = 42, Title = "Valid", Content = "Valid content", CreatedAt = DateTime.UtcNow };
        _noteServiceMock.Setup(s => s.CreateNoteAsync("Valid", "Valid content"))
            .ReturnsAsync(newNote);

        var controller = CreateController();
        var request = new CreateNoteRequest { Title = "Valid", Content = "Valid content" };

        var result = await controller.Create(request) as CreatedAtActionResult;

        Assert.NotNull(result);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(nameof(NotesController.GetById), result.ActionName);
        Assert.Equal(42, result.RouteValues["id"]);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsNote()
    {
        var note = new Note { Id = 5, Title = "Found", Content = "Details", CreatedAt = DateTime.UtcNow };
        _noteServiceMock.Setup(s => s.GetNoteByIdAsync(5)).ReturnsAsync(note);

        var controller = CreateController();
        var result = await controller.GetById(5) as OkObjectResult;

        Assert.NotNull(result);
        var data = result.Value;

        // Use reflection to verify the anonymous object's properties
        var idProperty = data.GetType().GetProperty("Id");
        var titleProperty = data.GetType().GetProperty("Title");
        var contentProperty = data.GetType().GetProperty("Content");
        var createdAtProperty = data.GetType().GetProperty("CreatedAt");

        Assert.NotNull(idProperty);
        Assert.NotNull(titleProperty);
        Assert.NotNull(contentProperty);
        Assert.NotNull(createdAtProperty);

        Assert.Equal(5, idProperty.GetValue(data));
        Assert.Equal("Found", titleProperty.GetValue(data));
        Assert.Equal("Details", contentProperty.GetValue(data));
    }

    [Fact]
    public async Task GetById_NonExistingId_ReturnsNotFound()
    {
        _noteServiceMock.Setup(s => s.GetNoteByIdAsync(999)).ReturnsAsync((Note)null);

        var controller = CreateController();
        var result = await controller.GetById(999);

        Assert.IsType<NotFoundResult>(result);
    }
}