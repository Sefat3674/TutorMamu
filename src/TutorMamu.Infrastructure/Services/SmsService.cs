
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TutorMamu.Application.Common;
using TutorMamu.Application.Interfaces;
using TutorMamu.Infrastructure.Settings;

namespace TutorMamu.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsSettings _settings;

    public SmsService(
        HttpClient httpClient,
        IOptions<SmsSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiToken))
            throw new AppException("SSL Wireless API token is missing.");

        if (string.IsNullOrWhiteSpace(_settings.SenderId))
            throw new AppException("SSL Wireless Sender ID is missing.");

        var mobile = NormalizePhoneNumber(phoneNumber);

        var csmsId = Guid.NewGuid().ToString("N")[..20];

        var payload = new
        {
            api_token = _settings.ApiToken,
            sid = _settings.SenderId,
            msisdn = mobile,
            sms = message,
            csms_id = csmsId
        };

        using var response = await _httpClient.PostAsJsonAsync(
            $"{_settings.BaseUrl.TrimEnd('/')}/send-sms",
            payload,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        // Check HTTP response
        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(
                $"SSL Wireless HTTP error: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        // Parse gateway response
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(responseBody);
        }
        catch (JsonException)
        {
            throw new AppException(
                $"Invalid SSL Wireless response: {responseBody}");
        }

        using (document)
        {
            var root = document.RootElement;

            var status = GetJsonValue(root, "status");

            if (!string.Equals(
                    status,
                    "SUCCESS",
                    StringComparison.OrdinalIgnoreCase))
            {
                var statusCode = GetJsonValue(root, "status_code")
                                 ?? GetJsonValue(root, "code")
                                 ?? "Unknown";

                var errorMessage = GetJsonValue(root, "error_message")
                                   ?? GetJsonValue(root, "message")
                                   ?? "Unknown SMS gateway error";

                throw new AppException(
                    $"SSL Wireless rejected SMS. " +
                    $"Code: {statusCode}. Reason: {errorMessage}");
            }
        }
    }

    private static string? GetJsonValue(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
            return null;

        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.Null => null,
            _ => property.ToString()
        };
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var number = phoneNumber.Trim()
            .Replace("+", "")
            .Replace(" ", "")
            .Replace("-", "");

        if (number.StartsWith("01") && number.Length == 11)
            return "88" + number;

        if (number.StartsWith("8801") && number.Length == 13)
            return number;

        throw new AppException(
            "Invalid Bangladesh mobile number.");
    }
}
