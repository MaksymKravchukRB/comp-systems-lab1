using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using lab1.Controllers;
using lab1.Data;

namespace NotesService.Tests;

public class HealthControllerTests
{
    [Fact]
    public void Alive_ReturnsOk()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        var controller = new HealthController(context);
        var result = controller.Alive() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("OK", result.Value);
    }

    [Fact]
    public async Task Ready_WhenDatabaseAvailable_ReturnsOk()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        var controller = new HealthController(context);
        var result = await controller.Ready() as OkObjectResult;

        Assert.NotNull(result);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("OK", result.Value);
    }
}