using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Application.Common.Security;

namespace CinemaBooking.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    private static readonly string[] ValidItemTypes = ["combo", "snack", "beverage", "dessert"];
    private static readonly string[] ValidStatuses = ["in_stock", "low_stock", "out_of_stock", "inactive"];

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<List<Product>> GetProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return _productRepository.GetProductsAsync(cinemaId, cancellationToken);
    }

    public Task<List<Product>> GetAvailableProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default)
    {
        return _productRepository.GetAvailableProductsAsync(cinemaId, cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _productRepository.GetByIdAsync(itemId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Product? Product)> CreateProductAsync(
        int cinemaId,
        string itemName,
        string itemType,
        string? description,
        decimal price,
        int stockQuantity,
        string? imageUrl,
        bool isOnMenu,
        bool isLoyaltyEligible,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeName(itemName);
        if (normalizedName is null)
        {
            return (false, "ItemName is required", null);
        }

        var normalizedType = NormalizeItemType(itemType);
        if (normalizedType is null)
        {
            return (false, "ItemType is required", null);
        }

        if (!ValidItemTypes.Contains(normalizedType))
        {
            return (false, $"ItemType must be one of: {string.Join(", ", ValidItemTypes)}", null);
        }

        if (price < 0)
        {
            return (false, "Price must be greater than or equal to 0", null);
        }

        if (stockQuantity < 0)
        {
            return (false, "StockQuantity must be greater than or equal to 0", null);
        }

        if (await _productRepository.NameExistsAsync(
                cinemaId, normalizedName, cancellationToken: cancellationToken))
        {
            return (false, "ItemName must be unique", null);
        }

        var product = new Product
        {
            CinemaID = cinemaId,
            ItemName = normalizedName,
            ItemType = normalizedType,
            Description = description?.Trim(),
            Price = price,
            StockQuantity = stockQuantity,
            ImageURL = imageUrl?.Trim(),
            IsOnMenu = isOnMenu,
            IsLoyaltyEligible = isLoyaltyEligible,
            Status = "in_stock"
        };

        var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

        return (true, null, createdProduct);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductAsync(
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
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _productRepository.GetByIdAsync(itemId, cancellationToken);
        if (existingProduct is null)
        {
            return (false, "Product not found", null);
        }
        if (existingProduct.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied, null);

        var normalizedName = NormalizeName(itemName);
        if (normalizedName is null)
        {
            return (false, "ItemName is required", null);
        }

        var normalizedType = NormalizeItemType(itemType);
        if (normalizedType is null)
        {
            return (false, "ItemType is required", null);
        }

        if (!ValidItemTypes.Contains(normalizedType))
        {
            return (false, $"ItemType must be one of: {string.Join(", ", ValidItemTypes)}", null);
        }

        if (price < 0)
        {
            return (false, "Price must be greater than or equal to 0", null);
        }

        if (stockQuantity < 0)
        {
            return (false, "StockQuantity must be greater than or equal to 0", null);
        }

        var normalizedStatus = NormalizeStatus(status);
        if (normalizedStatus is null)
        {
            return (false, "Status is required", null);
        }

        if (!ValidStatuses.Contains(normalizedStatus))
        {
            return (false, $"Status must be one of: {string.Join(", ", ValidStatuses)}", null);
        }

        if (await _productRepository.NameExistsAsync(
                managerCinemaId,
                normalizedName,
                itemId,
                cancellationToken))
        {
            return (false, "ItemName must be unique", null);
        }

        var productToUpdate = new Product
        {
            ItemID = itemId,
            CinemaID = managerCinemaId,
            ItemName = normalizedName,
            ItemType = normalizedType,
            Description = description?.Trim(),
            Price = price,
            StockQuantity = stockQuantity,
            ImageURL = imageUrl?.Trim(),
            IsOnMenu = isOnMenu,
            IsLoyaltyEligible = isLoyaltyEligible,
            Status = normalizedStatus
        };

        var updatedProduct = await _productRepository.UpdateAsync(productToUpdate, cancellationToken);

        return updatedProduct is null
            ? (false, "Product not found", null)
            : (true, null, updatedProduct);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteProductAsync(
        int itemId,
        int managerCinemaId,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _productRepository.GetByIdAsync(itemId, cancellationToken);
        if (existingProduct is null)
        {
            return (false, "Product not found");
        }
        if (existingProduct.CinemaID != managerCinemaId)
            return (false, CinemaScopeMessages.AccessDenied);

        if (await _productRepository.IsUsedInBookingsAsync(itemId, cancellationToken))
        {
            return (false, "Product is used in existing bookings");
        }

        var deleted = await _productRepository.DeleteAsync(itemId, cancellationToken);

        return deleted
            ? (true, null)
            : (false, "Product not found");
    }

    private static string? NormalizeName(string itemName)
    {
        return string.IsNullOrWhiteSpace(itemName)
            ? null
            : itemName.Trim();
    }

    private static string? NormalizeItemType(string itemType)
    {
        return string.IsNullOrWhiteSpace(itemType)
            ? null
            : itemType.Trim().ToLowerInvariant();
    }

    private static string? NormalizeStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? null
            : status.Trim().ToLowerInvariant();
    }
}
