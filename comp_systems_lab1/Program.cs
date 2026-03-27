using Microsoft.EntityFrameworkCore;
using lab1.Data;
using lab1.Services;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// 1. Load configuration from the required file
var configPath = "/etc/lab1/config.json";
var configuration = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
    .Build();

// 2. Get connection string and port
var connectionString = configuration.GetConnectionString("DefaultConnection");
var port = configuration.GetValue<int>("Port", 5000); // default to 5000 if not specified

// 3. Register DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// 4. Register services
builder.Services.AddScoped<INoteService, NoteService>();

// 5. Add controllers (and minimal API if needed)
builder.Services.AddControllers();

var app = builder.Build();

// 6. Configure the HTTP pipeline
app.UseRouting();
app.MapControllers();

// 7. Configure Kestrel to listen on localhost:port
app.Urls.Clear();
app.Urls.Add($"http://127.0.0.1:{port}");

app.Run();