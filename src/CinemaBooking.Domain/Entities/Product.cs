namespace CinemaBooking.Domain.Entities;

public class Product
{
    public int ItemID { get; set; }
    public string ItemName { get; set; } = null!;
    public string ItemType { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageURL { get; set; }
    public bool IsOnMenu { get; set; } = true;
    public bool IsLoyaltyEligible { get; set; }
    public string Status { get; set; } = "in_stock";
    public DateTime UpdatedAt { get; set; }

    public ICollection<BookingFnB> BookingFnBs { get; set; } = [];
}
