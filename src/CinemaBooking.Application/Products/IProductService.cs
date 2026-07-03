using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Products;

public interface IProductService
{
    Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default);

    Task<List<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default);

    Task<Product?> GetProductByIdAsync(int itemId, CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> CreateProductAsync(
        string itemName,
        string itemType,
        string? description,
        decimal price,
        string? imageUrl,
        bool isLoyaltyEligible,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductAsync(
        int itemId,
        string itemName,
        string itemType,
        string? description,
        decimal price,
        string? imageUrl,
        bool isLoyaltyEligible,
        string status,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductImageAsync(
        int itemId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> DeleteProductAsync(
        int itemId,
        CancellationToken cancellationToken = default);
}
