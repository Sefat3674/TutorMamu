
using Microsoft.Extensions.DependencyInjection;
using TutorMamu.Application.Interfaces;
using TutorMamu.Application.Services;

namespace TutorMamu.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<ITopicService, TopicService>();

        // Existing tutor profile functionality
        services.AddScoped<ITutorProfileService, TutorService>();

        services.AddScoped<IBookingService, BookingService>();

        return services;
    }
}
