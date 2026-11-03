namespace TutorMamu.Application.Common;

public class JwtOptions
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "TutorMamu";
    public string Audience { get; set; } = "TutorMamuClients";
    public int ExpiryMinutes { get; set; } = 120;
}

public class MarketplaceOptions
{
    /// <summary>Platform commission as a fraction (0.20 = 20%). Test 15-25% in the pilot.</summary>
    public decimal CommissionRate { get; set; } = 0.20m;

    /// <summary>Hours a tutor has to accept or decline a paid request.</summary>
    public int ResponseWindowHours { get; set; } = 2;

    public int MinDurationHours { get; set; } = 1;
    public int MaxDurationHours { get; set; } = 3;
}
