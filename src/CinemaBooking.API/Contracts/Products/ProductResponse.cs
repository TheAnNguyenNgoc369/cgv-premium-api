namespace CinemaBooking.API.Contracts.Products;

public sealed record ProductListResponse(
    List<ProductResponse> Products
);

public sealed record ProductResponse(
    int ItemID,
    string ItemName,
    string ItemType,
    string? Description,
    decimal Price,
    string? ImageURL,
    bool IsLoyaltyEligible
);
