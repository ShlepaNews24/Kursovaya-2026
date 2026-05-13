// [ФАЙЛ] GamesPlatform.API/Program.cs
using Serilog;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using GamesPlatform.API.Data;
using GamesPlatform.API.Features.Auth;
using GamesPlatform.API.Interfaces;
using GamesPlatform.API.Repositories;

// ============================================================================
// 🔍 1. ИНИЦИАЛИЗАЦИЯ SERILOG (ДО создания builder)
// ============================================================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        shared: true)
    .CreateLogger();

try
{
    Log.Information("🚀 Starting GamesPlatform API...");

    var builder = WebApplication.CreateBuilder(args);

    // ✅ Подключаем Serilog к хосту
    builder.Host.UseSerilog();

    // ============================================================================
    // 🗄️ 2. БАЗА ДАННЫХ
    // ============================================================================
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // ============================================================================
    // 🔐 3. JWT АУТЕНТИФИКАЦИЯ (соответствует вашему AuthService.cs)
    // ============================================================================
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]))
            };
        });

    builder.Services.AddAuthorization();

    // ============================================================================
    // 🏗️ 4. DEPENDENCY INJECTION
    // ============================================================================
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    builder.Services.AddScoped<IAuthService, AuthService>();

    // ============================================================================
    // 🎮 5. КОНТРОЛЛЕРЫ + JSON + CORS
    // ============================================================================
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.WriteIndented = false;
        });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    // ============================================================================
    // 📖 6. SWAGGER + JWT SUPPORT
    // ============================================================================
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
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // ============================================================================
    // 🚀 7. MIDDLEWARE PIPELINE
    // ============================================================================
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Games Platform API v1"));
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ============================================================================
    // 🗄️ 8. ИНИЦИАЛИЗАЦИЯ БД (EnsureCreated закомментирован)
    // ============================================================================
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // ⛔ Закомментировано по вашему запросу. 
        // Теперь БД управляется только через миграции: dotnet ef database update
        // context.Database.EnsureCreated(); 
    }

    Log.Information("🌐 Server is running on: {Urls}", string.Join(", ", app.Urls));
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💀 Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}