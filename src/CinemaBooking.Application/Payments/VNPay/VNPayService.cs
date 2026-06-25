using System.Security.Cryptography;
using System.Text;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Application.Payments.VNPay;

public sealed class VNPayService : IVNPayService
{
    private readonly VNPaySettings _settings;

    public VNPayService(IOptions<VNPaySettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<string> CreatePaymentUrlAsync(
        Payment payment,
        Booking booking,
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var vnpParams = new Dictionary<string, string>
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = _settings.Command,
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = ((long)(payment.Amount * 100)).ToString(),
            ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["vnp_CurrCode"] = _settings.CurrCode,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_OrderInfo"] = $"Cinema ticket payment - {booking.BookingCode}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_TxnRef"] = $"{booking.BookingCode}_{payment.PaymentID}_{DateTime.Now.Ticks}"
        };

        var queryString = BuildQueryString(vnpParams);
        var signature = CalculateHmacSha512(_settings.HashSecret, queryString);

        vnpParams["vnp_SecureHash"] = signature;

        var paymentUrl = $"{_settings.BaseUrl}?{BuildQueryString(vnpParams)}";
        return Task.FromResult(paymentUrl);
    }

    public Task<(bool IsValid, string ResponseCode, string TransactionNo)> ProcessCallbackAsync(
        Dictionary<string, string> vnpayData,
        CancellationToken cancellationToken = default)
    {
        if (!vnpayData.TryGetValue("vnp_SecureHash", out var receivedSignature))
            return Task.FromResult((false, string.Empty, string.Empty));

        var dataForSignature = new Dictionary<string, string>(vnpayData);
        dataForSignature.Remove("vnp_SecureHash");
        dataForSignature.Remove("vnp_SecureHashType");

        var isValid = VerifySignature(dataForSignature, receivedSignature);

        var responseCode = vnpayData.GetValueOrDefault("vnp_ResponseCode", string.Empty);
        var transactionNo = vnpayData.GetValueOrDefault("vnp_TransactionNo", string.Empty);

        return Task.FromResult((isValid, responseCode, transactionNo));
    }

    public bool VerifySignature(
        Dictionary<string, string> vnpayData,
        string secureHash)
    {
        var queryString = BuildQueryString(vnpayData);
        var calculatedSignature = CalculateHmacSha512(_settings.HashSecret, queryString);

        return string.Equals(
            calculatedSignature,
            secureHash,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildQueryString(Dictionary<string, string> data)
    {
        var sortedData = data
            .OrderBy(kvp => kvp.Key)
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value));

        return string.Join("&", sortedData.Select(kvp =>
            $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
    }

    private static string CalculateHmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return BitConverter.ToString(hashBytes)
            .Replace("-", string.Empty)
            .ToUpperInvariant();
    }
}
