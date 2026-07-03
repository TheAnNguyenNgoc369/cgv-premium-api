using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Products;

public sealed class ProductService : IProductService
{
    private const string ImageFolder = "products";
    private const string ImageUploadFailedMessage = "Unable to upload product image. Please try again later.";
    private const string ImageDeleteFailedMessage = "Unable to replace the existing product image. Please try again later.";

    private readonly IProductRepository _productRepository;
    private readonly IImageStorageService _imageStorageService;

    private static readonly string[] ValidItemTypes = ["combo", "snack", "beverage", "dessert"];
    private static readonly string[] ValidStatuses = ["active", "inactive"];

    public ProductService(
        IProductRepository productRepository,
        IImageStorageService imageStorageService)
    {
        _productRepository = productRepository;
        _imageStorageService = imageStorageService;
    }

    public Task<List<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        return _productRepository.GetProductsAsync(cancellationToken);
    }

    public Task<List<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
    {
        return _productRepository.GetAvailableProductsAsync(cancellationToken);
    }

    public Task<Product?> GetProductByIdAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        return _productRepository.GetByIdAsync(itemId, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Product? Product)> CreateProductAsync(
        string itemName,
        string itemType,
        string? description,
        decimal price,
        string? imageUrl,
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

        if (await _productRepository.NameExistsAsync(
                normalizedName, cancellationToken: cancellationToken))
        {
            return (false, "ItemName must be unique", null);
        }

        var product = new Product
        {
            ItemName = normalizedName,
            ItemType = normalizedType,
            Description = description?.Trim(),
            Price = price,
            ImageURL = imageUrl?.Trim(),
            IsLoyaltyEligible = isLoyaltyEligible,
            Status = "active"
        };

        var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

        return (true, null, createdProduct);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductAsync(
        int itemId,
        string itemName,
        string itemType,
        string? description,
        decimal price,
        string? imageUrl,
        bool isLoyaltyEligible,
        string status,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _productRepository.GetByIdAsync(itemId, cancellationToken);
        if (existingProduct is null)
        {
            return (false, "Product not found", null);
        }
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
                normalizedName,
                itemId,
                cancellationToken))
        {
            return (false, "ItemName must be unique", null);
        }

        var productToUpdate = new Product
        {
            ItemID = itemId,
            ItemName = normalizedName,
            ItemType = normalizedType,
            Description = description?.Trim(),
            Price = price,
            ImageURL = imageUrl?.Trim(),
            IsLoyaltyEligible = isLoyaltyEligible,
            Status = normalizedStatus
        };

        var updatedProduct = await _productRepository.UpdateAsync(productToUpdate, cancellationToken);

        return updatedProduct is null
            ? (false, "Product not found", null)
            : (true, null, updatedProduct);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Product? Product)> UpdateProductImageAsync(
        int itemId,
        Stream imageStream,
        string fileName,
        string? contentType,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _productRepository.GetByIdAsync(itemId, cancellationToken);
        if (existingProduct is null)
            return (false, "Product not found", null);

        var validationError = ImageFileValidator.Validate(fileName, contentType, fileSize);
        if (validationError is not null)
            return (false, validationError, null);

        StoredImageResult newImage;
        try
        {
            newImage = await _imageStorageService.UploadImageAsync(
                imageStream, fileName, ImageFolder, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return (false, ImageUploadFailedMessage, null);
        }

        if (!string.IsNullOrWhiteSpace(existingProduct.ImagePublicId))
        {
            try
            {
                await _imageStorageService.DeleteImageAsync(
                    existingProduct.ImagePublicId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await TryDeleteImageAsync(newImage.PublicId);
                return (false, ImageDeleteFailedMessage, null);
            }
        }

        try
        {
            var updatedProduct = await _productRepository.UpdateImageAsync(
                itemId,
                newImage.SecureUrl,
                newImage.PublicId,
                cancellationToken);

            return updatedProduct is null
                ? (false, "Product not found", null)
                : (true, null, updatedProduct);
        }
        catch
        {
            await TryDeleteImageAsync(newImage.PublicId);
            throw;
        }
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> DeleteProductAsync(
        int itemId,
        CancellationToken cancellationToken = default)
    {
        var existingProduct = await _productRepository.GetByIdAsync(itemId, cancellationToken);
        if (existingProduct is null)
        {
            return (false, "Product not found");
        }
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

    private async Task TryDeleteImageAsync(string publicId)
    {
        try
        {
            await _imageStorageService.DeleteImageAsync(publicId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
        }
    }
}
