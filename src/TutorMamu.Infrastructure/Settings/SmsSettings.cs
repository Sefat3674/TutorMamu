namespace TutorMamu.Infrastructure.Settings;

public class SmsSettings
{
    public string BaseUrl { get; set; } = "https://api.sms.net.bd";
    public string ApiKey { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
}