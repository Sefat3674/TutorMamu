using TutorMamu.Application.Interfaces;

namespace TutorMamu.API.Workers;

public class BookingExpiryWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpiryWorker> _logger;

    public BookingExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<BookingExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookings = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var count = await bookings.ExpireUnansweredAsync();
                if (count > 0)
                    _logger.LogInformation("Refunded {Count} unanswered booking(s).", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking expiry run failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
