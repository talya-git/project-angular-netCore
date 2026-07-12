using GiftsService.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. הוספת תמיכה בקונטרולרים
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 2. הגדרת חיבור למסד הנתונים הייעודי של מתנות (GiftsDb)
var connectionString = builder.Configuration.GetConnectionString("GiftsConnection") 
    ?? "Server=sqlserver;Database=GiftsDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;";

builder.Services.AddDbContext<GiftsDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

// 3. הגדרת Swagger לסביבת פיתוח
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// 4. הרצה אוטומטית של מיגרציות ויצירת בסיס הנתונים בהפעלה
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GiftsDbContext>();
    for (var retry = 0; retry < 12 && !db.Database.CanConnect(); retry++)
    {
        Console.WriteLine($"Waiting for SQL Server for GiftsService... attempt {retry + 1}/12");
        Thread.Sleep(TimeSpan.FromSeconds(5));
    }

    if (!db.Database.CanConnect())
    {
        throw new Exception("GiftsService could not connect to SQL Server.");
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