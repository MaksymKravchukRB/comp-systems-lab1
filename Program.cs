using Microsoft.EntityFrameworkCore;
using lab1.Data;
using lab1.Services;
using Microsoft.Extensions.Configuration;

// --- Configuration ---
string connectionString;
int port = 5000;

// Try environment variable first
var envConn = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrEmpty(envConn))
{
    connectionString = envConn;
    Console.WriteLine("Using DB connection string from environment variable.");
}
else
{
    var configPath = "/etc/mywebapp/config.json";
    if (!File.Exists(configPath))
    {
        Console.Error.WriteLine($"Configuration file not found at {configPath} and no DB_CONNECTION_STRING env var.");
        return 1;
    }
    var configuration = new ConfigurationBuilder()
        .AddJsonFile(configPath, optional: false, reloadOnChange: true)
        .Build();
    connectionString = configuration.GetConnectionString("DefaultConnection");
    port = configuration.GetValue<int>("Port", 5000);
}

// Override port from environment if present
var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out int p))
{
    port = p;
}

// --- Handle migration command ---
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

// --- Normal web application startup ---
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddControllers();

var app = builder.Build();
app.UseRouting();
app.MapControllers();

app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

await app.RunAsync();
return 0;
