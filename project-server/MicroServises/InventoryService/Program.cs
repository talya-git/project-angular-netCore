using InventoryService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=sqlserver;Database=InventoryDb;User Id=sa;Password=YourSecurePassword123!;TrustServerCertificate=True;";

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var masterConn = connectionString.Replace("Database=InventoryDb", "Database=master");
    bool serverReady = false;
    for (var retry = 0; retry < 12; retry++)
    {
        try
        {
            using var conn = new Microsoft.Data.SqlClient.SqlConnection(masterConn);
            conn.Open();
            serverReady = true;
            break;
        }
        catch
        {
            Console.WriteLine($"Waiting for SQL Server for InventoryService... attempt {retry + 1}/12");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }

    if (!serverReady) throw new Exception("InventoryService could not connect to SQL Server.");

    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
