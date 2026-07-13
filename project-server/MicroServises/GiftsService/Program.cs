using GiftsService.Data;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, config) => config
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "GiftsService")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://seq:5341"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var mongoConnectionString = builder.Configuration.GetConnectionString("MongoConnection")
    ?? "mongodb://mongodb:27017";

var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";

builder.Services.AddSingleton(_ => new GiftsDbContext(mongoConnectionString));
builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

builder.Services.AddHealthChecks()
    .AddMongoDb(mongoConnectionString, name: "mongodb")
    .AddRedis(redisConnection, name: "redis");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Headers.ContainsKey("X-Correlation-Id"))
        ctx.Request.Headers["X-Correlation-Id"] = Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-Id"] = ctx.Request.Headers["X-Correlation-Id"];
    await next();
});

app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
