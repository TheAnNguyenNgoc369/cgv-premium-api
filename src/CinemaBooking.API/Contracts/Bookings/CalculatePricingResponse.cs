namespace CinemaBooking.API.Contracts.Bookings;

public sealed class CalculatePricingResponse
{
    public decimal SeatsSubTotal { get; set; }
    public decimal FnBSubTotal { get; set; }
    public decimal TotalBeforeDiscount { get; set; }
    public decimal MembershipDiscount { get; set; }
    public decimal VoucherDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }

    public List<SeatPricingResponse> SeatDetails { get; set; } = [];
    public List<FnBPricingResponse> FnBDetails { get; set; } = [];
    public VoucherPricingResponse? VoucherDetails { get; set; }
}

public sealed class SeatPricingResponse
{
    public int SeatId { get; set; }
    public string SeatRow { get; set; } = null!;
    public int SeatCol { get; set; }
    public string SeatTypeName { get; set; } = null!;
    public decimal Price { get; set; }
}

public sealed class FnBPricingResponse
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public sealed class VoucherPricingResponse
{
    public string VoucherCode { get; set; } = null!;
    public string DiscountType { get; set; } = null!;
    public decimal DiscountApplied { get; set; }
}
