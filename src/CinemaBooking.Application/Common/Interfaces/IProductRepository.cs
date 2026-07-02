using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default);

    Task<List<Product>> GetAvailableProductsAsync(
        int cinemaId,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int itemId, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        int cinemaId,
        string itemName,
        int? excludingItemId = null,
        CancellationToken cancellationToken = default);

    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> UpdateImageAsync(
        int itemId,
        string imageUrl,
        string imagePublicId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsedInBookingsAsync(int itemId, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int itemId, CancellationToken cancellationToken = default);
}
