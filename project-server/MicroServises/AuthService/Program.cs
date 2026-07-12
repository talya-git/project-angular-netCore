using AuthService.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// חיבור לבסיס הנתונים הייעודי של האימות (AuthDb)
var connectionString = builder.Configuration.GetConnectionString("AuthConnection") 
    ?? "Server=sqlserver;Database=AuthDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// מיגרציות אוטומטיות
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    for (var retry = 0; retry < 12 && !db.Database.CanConnect(); retry++)
    {
        Console.WriteLine($"Waiting for SQL Server for AuthService... attempt {retry + 1}/12");
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }

    if (!db.Database.CanConnect())
    {
        throw new Exception("AuthService could not connect to SQL Server.");
    }

    if (db.Database.GetMigrations().Any())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

app.Run();