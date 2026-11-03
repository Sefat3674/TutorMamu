namespace TutorMamu.Application.Common;

/// <summary>Business-rule failure. The API maps StatusCode to the HTTP response.</summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
