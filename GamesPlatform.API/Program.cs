using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;  

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Настройка Entity Framework с SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>  // ← Добавить настройку JSON!
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = false; 
    });

// Настройка CORS (для клиентского приложения)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Настройка Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Games Platform API",
        Version = "v1",
        Description = "API для платформы браузерных игр"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Games Platform API v1");
        c.RoutePrefix = string.Empty; // Swagger UI на корне
    });
}

app.UseHttpsRedirection();

// Используем CORS
app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

// Создаем базу данных при запуске (для разработки)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

app.Run();