using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.Refunds;

public sealed record CreateRefundRequest(
    [Required] int BookingId,
    [Required][StringLength(500)] string Reason
);
