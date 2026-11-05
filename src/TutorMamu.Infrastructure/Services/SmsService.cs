using System.Text.Json;
using System.Net.Http;
using TutorMamu.Application.Common;
using TutorMamu.Application.Interfaces;
using TutorMamu.Infrastructure.Settings;
using Microsoft.Extensions.Options;

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
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            throw new AppException("Alpha SMS API key is missing.");

        if (string.IsNullOrWhiteSpace(message))
            throw new AppException("SMS message cannot be empty.");

        var mobile = NormalizePhoneNumber(phoneNumber);

        var parameters = new Dictionary<string, string>
        {
            ["api_key"] = _settings.ApiKey,
            ["msg"] = message,
            ["to"] = mobile
        };

        // Sender ID is optional. Send it only when configured.
        if (!string.IsNullOrWhiteSpace(_settings.SenderId))
        {
            parameters["sender_id"] = _settings.SenderId;
        }

        using var content = new FormUrlEncodedContent(parameters);

        using var response = await _httpClient.PostAsync(
            $"{_settings.BaseUrl.TrimEnd('/')}/sendsms",
            content,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(
                $"Alpha SMS HTTP error: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);

            var root = document.RootElement;

            if (!root.TryGetProperty("error", out var errorElement))
            {
                throw new AppException(
                    "Alpha SMS returned an invalid response.");
            }

            var errorCode = errorElement.ValueKind switch
            {
                JsonValueKind.Number => errorElement.GetInt32(),
                JsonValueKind.String when int.TryParse(
                    errorElement.GetString(), out var value) => value,
                _ => -1
            };

            if (errorCode != 0)
            {
                var gatewayMessage =
                    root.TryGetProperty("msg", out var msgElement)
                        ? msgElement.ToString()
                        : "Unknown SMS gateway error.";

                throw new AppException(
                    $"Alpha SMS rejected the request. " +
                    $"Error: {errorCode}. Reason: {gatewayMessage}");
            }
        }
        catch (JsonException)
        {
            throw new AppException(
                "Alpha SMS returned an invalid JSON response.");
        }
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