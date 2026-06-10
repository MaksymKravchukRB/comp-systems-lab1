using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using lab1.Controllers;

namespace NotesService.Tests;

public class HomeControllerTests
{
    private HomeController CreateController(string acceptHeader = null)
    {
        var controller = new HomeController();
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
    public void Index_WhenAcceptHtml_ReturnsHtmlContent()
    {
        var controller = CreateController("text/html");
        var result = controller.Index() as ContentResult;
        
        Assert.NotNull(result);
        Assert.Equal("text/html", result.ContentType);
        Assert.Contains("Notes Service API", result.Content);
    }

    [Fact]
    public void Index_WhenAcceptNotHtml_Returns406()
    {
        var controller = CreateController("application/json");
        var result = controller.Index() as ObjectResult;   // ← was StatusCodeResult

        Assert.NotNull(result);
        Assert.Equal(406, result.StatusCode);
        Assert.Contains("Only text/html", result.Value.ToString());
    }
}