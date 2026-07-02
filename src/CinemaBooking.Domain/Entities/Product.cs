namespace CinemaBooking.Domain.Entities;

public class Product
{
    public int ItemID { get; set; }
    public int CinemaID { get; set; }
    public string ItemName { get; set; } = null!;
    public string ItemType { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string? ImageURL { get; set; }
    public string? ImagePublicId { get; set; }
    public bool IsOnMenu { get; set; } = true;
    public bool IsLoyaltyEligible { get; set; }
    public string Status { get; set; } = "in_stock";
    public DateTime UpdatedAt { get; set; }

    public Cinema Cinema { get; set; } = null!;
    public ICollection<BookingFnB> BookingFnBs { get; set; } = [];
}
