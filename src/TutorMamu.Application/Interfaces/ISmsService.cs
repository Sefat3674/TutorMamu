namespace TutorMamu.Application.Interfaces;

public interface ISmsService
{
    Task SendSmsAsync(
        string phoneNumber,
        string message,
        CancellationToken cancellationToken = default);
}