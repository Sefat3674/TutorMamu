using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TutorMamu.Application.Interfaces;
using TutorMamu.Domain.Entities;
using TutorMamu.Infrastructure.Data;
using TutorMamu.Infrastructure.Repositories;
using TutorMamu.Infrastructure.Services;

namespace TutorMamu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(
            options => options.UseSqlServer(connectionString));

        // Identity
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>();

        // Repository
        services.AddScoped(
            typeof(IRepository<>),
            typeof(Repository<>));

        // Authentication
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();

        // Notifications
        services.AddScoped<
            INotificationService,
            LoggingNotificationService>();

        // SSL Wireless SMS
        services.AddHttpClient<ISmsService, SmsService>();

        return services;
    }
}