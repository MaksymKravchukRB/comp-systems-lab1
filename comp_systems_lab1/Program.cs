using Microsoft.EntityFrameworkCore;
using lab1.Data;
using lab1.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

var configPath = "/etc/lab1/config.json";
if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Configuration file not found at {configPath}");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
    .Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
var port = configuration.GetValue<int>("Port", 5000);

if (args.Contains("--migrate"))
{
    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    optionsBuilder.UseNpgsql(connectionString);
    using var context = new ApplicationDbContext(optionsBuilder.Options);
    try
    {
        Console.WriteLine("Applying database migrations...");
        await context.Database.MigrateAsync();
        Console.WriteLine("Migrations applied successfully.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Migration failed: {ex.Message}");
        return 1;
    }
}

// Normal web application setup
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Urls.Clear();
app.Urls.Add($"http://127.0.0.1:{port}");

await app.RunAsync();

return 0;