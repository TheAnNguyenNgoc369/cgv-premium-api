namespace CinemaBooking.Application.Payments.PayOS;

public sealed record PayOSWebhookData(
    long OrderCode,
    int Amount,
    string Description,
    string AccountNumber,
    string Reference,
    string TransactionDateTime,
    string Currency,
    string PaymentLinkId,
    string Code,
    string DescriptionDetail,
    string? CounterAccountBankId,
    string? CounterAccountBankName,
    string? CounterAccountName,
    string? CounterAccountNumber,
    string? VirtualAccountName,
    string? VirtualAccountNumber);
