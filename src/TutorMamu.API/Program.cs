
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using TutorMamu.API.Middleware;
using TutorMamu.API.Workers;

using TutorMamu.Application;
using TutorMamu.Application.Common;

using TutorMamu.Domain.Entities;

using TutorMamu.Infrastructure;
using TutorMamu.Infrastructure.Data;
using TutorMamu.Infrastructure.Settings;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. CONFIGURATION
// =====================================================

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<MarketplaceOptions>(
    builder.Configuration.GetSection("Marketplace"));

builder.Services.Configure<SmsSettings>(
    builder.Configuration.GetSection("SmsSettings"));

var jwt = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "Missing Jwt configuration.");

// =====================================================
// 2. APPLICATION + INFRASTRUCTURE LAYERS
// =====================================================

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing DefaultConnection."));

// =====================================================
// 3. ASP.NET IDENTITY + ROLE MANAGEMENT
// =====================================================

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// =====================================================
// 4. JWT AUTHENTICATION
// =====================================================

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),

                ClockSkew = TimeSpan.FromMinutes(1)
            };
    });

builder.Services.AddAuthorization();

// =====================================================
// 5. CORS
// =====================================================

var origins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================
// 6. CONTROLLERS + SWAGGER
// =====================================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TutorMamu API",
        Version = "v1",
        Description = "TutorMamu - Tutor and Student Marketplace API"
    });

    options.AddSecurityDefinition("Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the JWT token only."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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

// =====================================================
// 7. BACKGROUND WORKERS
// =====================================================

builder.Services.AddHostedService<BookingExpiryWorker>();

// =====================================================
// 8. BUILD APPLICATION
// =====================================================

var app = builder.Build();

// =====================================================
// 9. GLOBAL EXCEPTION MIDDLEWARE
// =====================================================

app.UseMiddleware<ExceptionMiddleware>();

// =====================================================
// 10. DEVELOPMENT CONFIGURATION
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();
    }

    await DataSeeder.SeedAsync(
        app.Services,
        app.Configuration);
}

// =====================================================
// 11. HTTP MIDDLEWARE
// =====================================================

app.UseStaticFiles();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// 12. MAP CONTROLLERS
// =====================================================

app.MapControllers();

// =====================================================
// 13. RUN
// =====================================================

app.Run();

public partial class Program { }
