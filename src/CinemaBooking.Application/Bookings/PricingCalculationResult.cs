namespace CinemaBooking.Application.Bookings;

public sealed class PricingCalculationResult
{
    public decimal SeatsSubTotal { get; set; }
    public decimal FnBSubTotal { get; set; }
    public decimal TotalBeforeDiscount { get; set; }
    public decimal MembershipDiscount { get; set; }
    public decimal VoucherDiscount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalAmount { get; set; }

    public List<SeatPricingDetail> SeatDetails { get; set; } = [];
    public List<FnBPricingDetail> FnBDetails { get; set; } = [];
    public VoucherPricingDetail? VoucherDetails { get; set; }
}

public sealed class SeatPricingDetail
{
    public int SeatId { get; set; }
    public string SeatRow { get; set; } = null!;
    public int SeatCol { get; set; }
    public string SeatTypeName { get; set; } = null!;
    public decimal Price { get; set; }
}

public sealed class FnBPricingDetail
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public sealed class VoucherPricingDetail
{
    public string VoucherCode { get; set; } = null!;
    public string DiscountType { get; set; } = null!;
    public decimal DiscountApplied { get; set; }
}
