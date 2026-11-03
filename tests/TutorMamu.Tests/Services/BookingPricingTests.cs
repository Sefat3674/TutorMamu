using TutorMamu.Application.Services;

namespace TutorMamu.Tests.Services;

public class BookingPricingTests
{
    [Theory]
    [InlineData(400, 1, 0.20, 400, 80)]
    [InlineData(400, 2, 0.20, 800, 160)]
    [InlineData(400, 3, 0.20, 1200, 240)]
    [InlineData(350, 3, 0.15, 1050, 157.5)]
    public void CalculatePrice_UsesRateTimesHoursAndCommission(
        double rate, int hours, double commissionRate, double expectedTotal, double expectedCommission)
    {
        var (total, commission) = BookingService.CalculatePrice((decimal)rate, hours, (decimal)commissionRate);

        Assert.Equal((decimal)expectedTotal, total);
        Assert.Equal((decimal)expectedCommission, commission);
    }

    [Fact]
    public void CalculatePrice_RejectsZeroHours()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BookingService.CalculatePrice(400m, 0, 0.2m));
    }

    [Fact]
    public void Slugify_ProducesUrlSafeSlug()
    {
        Assert.Equal("integration-by-parts", TopicService.Slugify("Integration by Parts!"));
    }
}
