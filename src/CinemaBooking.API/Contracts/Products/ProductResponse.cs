namespace CinemaBooking.API.Contracts.Products;

public sealed record ProductListResponse(
    List<ProductResponse> Products
);

public sealed record ProductResponse(
    int ItemID,
    int CinemaID,
    string ItemName,
    string ItemType,
    string? Description,
    decimal Price,
    int StockQuantity,
    string? ImageURL,
    bool IsOnMenu,
    bool IsLoyaltyEligible,
    string Status,
    DateTime UpdatedAt
);
