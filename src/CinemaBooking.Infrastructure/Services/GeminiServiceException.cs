namespace CinemaBooking.Infrastructure.Services;

public sealed class GeminiServiceException : Exception
{
    public string ErrorCode { get; }

    public GeminiServiceException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public GeminiServiceException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
