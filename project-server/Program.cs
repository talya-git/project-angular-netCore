using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore; 


var builder = WebApplication.CreateBuilder(args);
// 1. הוספת תמיכה בקונטרולרים ו-Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lottery API", Version = "v1" });
});

// 2. הגדרת מחרוזת החיבור ל-SQL Server (GiftsDb)
var connectionString = builder.Configuration.GetConnectionString("GiftsConnection") 
    ?? "Server=sqlserver;Database=GiftsDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;";

// רישום ה-DbContext באמצעות פונקציית הלמבדה הכללית כדי לעקוף את הצורך בשם ה-Namespace
builder.Services.AddDbContext<DbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

// 3. הגדרת Swagger לסביבת פיתוח
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// 4. בדיקת חיבור והרצה אוטומטית של יצירת בסיס הנתונים
using (var scope = app.Services.CreateScope())
{
    try
    {
        // שליפת ה-Context הכללי הרשום במערכת
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        
        // לולאת ניסיונות התחברות לשרת ה-SQL
        for (var retry = 0; retry < 12 && !db.Database.CanConnect(); retry++)
        {
            Console.WriteLine($"Waiting for SQL Server... attempt {retry + 1}/12");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }

        if (!db.Database.CanConnect())
        {
            throw new Exception("Could not connect to SQL Server.");
        }

        // יצירת בסיס הנתונים והטבלאות אם אינם קיימים
        db.Database.EnsureCreated();
        Console.WriteLine("Database initialization completed successfully.");

        // --- הוספת נתוני בדיקה אוטומטיים (Seed Data) ---
        // שליפת סוג הישות של המתנה (Gift) מתוך מרחב השמות באופן דינמי
        var giftType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "Gift");

        if (giftType != null)
        {
            // בדיקה דינמית אם קיימים כבר נתונים בטבלה
            var dbSet = db.GetType().GetMethod("Set", Type.EmptyTypes)?.MakeGenericMethod(giftType).Invoke(db, null);
            var anyMethod = typeof(Queryable).GetMethods().First(m => m.Name == "Any" && m.GetParameters().Length == 1).MakeGenericMethod(giftType);
            var hasData = (bool)anyMethod.Invoke(null, new object[] { dbSet });

            if (!hasData)
            {
                // יצירת אובייקטים חדשים של מתנות לבדיקה
                var gift1 = Activator.CreateInstance(giftType);
                giftType.GetProperty("Name")?.SetValue(gift1, "ערכת מתנה חגיגית");
                giftType.GetProperty("Price")?.SetValue(gift1, 150m);

                var gift2 = Activator.CreateInstance(giftType);
                giftType.GetProperty("Name")?.SetValue(gift2, "שעון קיר מעוצב");
                giftType.GetProperty("Price")?.SetValue(gift2, 80m);

                // הוספה ושמירה לדאטה-בייס
                var addRangeMethod = dbSet.GetType().GetMethod("AddRange", new[] { typeof(object[]) });
                if (addRangeMethod != null)
                {
                    var objectsToAdd = Array.CreateInstance(typeof(object), 2);
                    objectsToAdd.SetValue(gift1, 0);
                    objectsToAdd.SetValue(gift2, 1);
                    addRangeMethod.Invoke(dbSet, new object[] { objectsToAdd });
                }
                
                db.SaveChanges();
                Console.WriteLine("Seed data for Gifts inserted successfully.");
            }
        }
        // -------------------------------------------------
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during database initialization: {ex.Message}");
    }
}

app.Run();