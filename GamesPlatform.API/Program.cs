using Serilog;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using GamesPlatform.API.Data;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using GamesPlatform.API.Features.Auth;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Repositories;
using Microsoft.Extensions.Logging;

// ✅ 1. Инициализация Serilog ДО создания builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt", 
        rollingInterval: RollingInterval.Day, 
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 Starting GamesPlatform API...");

    var builder = WebApplication.CreateBuilder(args);

    // ✅ 2. Подключаем Serilog к хосту
    builder.Host.UseSerilog();

    // --- Сервисы ---
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

    // --- Аутентификация (JWT) ---
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                ValidAudience = builder.Configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!))
            };
        });

    builder.Services.AddAuthorization();

    // --- База данных ---
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // --- Контроллеры & JSON ---
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
        });

    // --- CORS ---
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // --- Swagger ---
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Games Platform API",
            Version = "v1",
            Description = "API для платформы браузерных игр"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Введите токен в формате: Bearer {ваш_токен}",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference 
                    { 
                        Type = ReferenceType.SecurityScheme, 
                        Id = "Bearer" 
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // --- Middleware ---
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Games Platform API v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // --- Инициализация БД ---
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        Log.Information("✅ Database initialized");
    }

    Log.Information("🌐 Server is running on {Url}", app.Urls.FirstOrDefault() ?? "Unknown");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💀 Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}