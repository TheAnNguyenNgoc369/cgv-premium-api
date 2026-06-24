namespace CinemaBooking.Shared.Constants;

public static class VNPayResponseCode
{
    public const string Success = "00";
    public const string InvalidAmount = "04";
    public const string AccountLocked = "05";
    public const string TransactionFailed = "07";
    public const string InvalidCard = "09";
    public const string ExpiredCard = "10";
    public const string InvalidOTP = "11";
    public const string AccountBlockedTemp = "12";
    public const string InvalidTransactionPassword = "13";
    public const string CanceledByUser = "24";
    public const string InsufficientBalance = "51";
    public const string DailyLimitExceeded = "65";
    public const string MaintenanceInProgress = "75";
    public const string InvalidSignature = "97";
    public const string SystemError = "99";
}
