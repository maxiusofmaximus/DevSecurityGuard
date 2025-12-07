using DevSecurityGuard.Service.Database;
using DevSecurityGuard.API.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS for web UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)  // Allow any origin for development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR
    });
});

// Add Database
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
    "DevSecurityGuard",
    "devsecurity.db"
);

builder.Services.AddDbContext<DevSecurityDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Serve static files from DevSecurityGuard.Web directory
var webPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "DevSecurityGuard.Web");
if (Directory.Exists(webPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new List<string> { "index.html" },
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webPath)
    });
    
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webPath),
        RequestPath = ""
    });
    
    Console.WriteLine($"Serving static files from: {webPath}");
}

app.UseAuthorization();

app.MapControllers();

// Map SignalR hub
app.MapHub<DevSecurityHub>("/hubs/devsecurity");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DevSecurityDbContext>();
    db.Database.EnsureCreated();
}

Console.WriteLine("DevSecurityGuard API starting...");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine("SignalR Hub: /hubs/devsecurity");

app.Run();
