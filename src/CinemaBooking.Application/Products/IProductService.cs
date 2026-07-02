using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Products;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetAvailableProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(int itemId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> CreateProductAsync(
        int cinemaId,
        string itemName,
        string itemType,
        string? description,
        decimal price,
        int stockQuantity,
        string? imageUrl,
        bool isOnMenu,
        bool isLoyaltyEligible,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductAsync(
        int itemId,
        int managerCinemaId,
        string itemName,
        string itemType,
        string? description,
        decimal price,
        int stockQuantity,
        string? imageUrl,
        bool isOnMenu,
        bool isLoyaltyEligible,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductImageAsync(
        int itemId,
        int managerCinemaId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteProductAsync(
        int itemId,
        int managerCinemaId,
        CancellationToken cancellationToken = default);
}
