namespace TutorMamu.Infrastructure.Settings;

public class SmsSettings
{
    public string BaseUrl { get; set; } =
        "https://smsplus.sslwireless.com/api/v3";

    public string ApiToken { get; set; } = string.Empty;

    public string SenderId { get; set; } = string.Empty;
}