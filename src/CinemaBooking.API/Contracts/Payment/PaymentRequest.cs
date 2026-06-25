using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Payment;

public sealed class InitiatePaymentRequest
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = null!;
}

public sealed class ConfirmCashPaymentRequest
{
    [Required]
    public int PaymentId { get; set; }
}
