using System.ComponentModel.DataAnnotations;
using CinemaBooking.API.Validation;

namespace CinemaBooking.API.Contracts.Products;

public sealed record CreateProductRequest(
    [Required] [MaxLength(150)] string ItemName,
    [Required] [MaxLength(30)] [ValidItemType] string ItemType,
    [MaxLength(500)] string? Description,
    [Required] [Range(0, double.MaxValue)] decimal Price,
    [MaxLength(500)] string? ImageURL,
    bool IsLoyaltyEligible
);

public sealed record UpdateProductRequest(
    [Required] [MaxLength(150)] string ItemName,
    [Required] [MaxLength(30)] [ValidItemType] string ItemType,
    [MaxLength(500)] string? Description,
    [Required] [Range(0, double.MaxValue)] decimal Price,
    [MaxLength(500)] string? ImageURL,
    bool IsLoyaltyEligible,
    [Required] [MaxLength(20)] [ValidProductStatus] string Status
);
