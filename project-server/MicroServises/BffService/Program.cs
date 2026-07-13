var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHttpClient("OrdersClient", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Orders"] ?? "http://orders-service:8080"));

builder.Services.AddHttpClient("GiftsClient", c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Gifts"] ?? "http://gifts-service-1:8080"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();
