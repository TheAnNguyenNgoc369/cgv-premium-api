namespace CinemaBooking.Application.Payments;

public enum PaymentErrorType
{
    Validation,
    NotFound,
    Forbidden,
    Conflict,
    Gateway
}

public sealed record PaymentOperationResult(
    bool Succeeded,
    object? Value = null,
    PaymentErrorType? ErrorType = null,
    string? ErrorMessage = null)
{
    public static PaymentOperationResult Success(object value) => new(true, value);

    public static PaymentOperationResult Failure(PaymentErrorType type, string message) =>
        new(false, null, type, message);
}
