using System.ComponentModel.DataAnnotations;

namespace CinemaBooking.API.Contracts.CheckIns;

public sealed class FnBPickupHistoryRequest
{
    public int? StaffId { get; set; }
    public int? CinemaId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "page must be greater than or equal to 1.")]
    public int Page { get; set; } = 1;
    [Range(1, 100, ErrorMessage = "pageSize must be between 1 and 100.")]
    public int PageSize { get; set; } = 20;
}