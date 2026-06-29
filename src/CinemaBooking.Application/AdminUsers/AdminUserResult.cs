namespace CinemaBooking.Application.AdminUsers;

public sealed record AdminUserResult<T>(
    bool Succeeded,
    AdminUserErrorType ErrorType,
    string? ErrorMessage,
    T? Value)
{
    public static AdminUserResult<T> Success(T value) =>
        new(true, AdminUserErrorType.None, null, value);

    public static AdminUserResult<T> Failure(AdminUserErrorType errorType, string message) =>
        new(false, errorType, message, default);
}
