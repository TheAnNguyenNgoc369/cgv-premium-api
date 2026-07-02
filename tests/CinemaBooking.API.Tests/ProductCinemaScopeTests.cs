using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Common.Security;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Products;

namespace CinemaBooking.API.Tests;

public sealed class ProductCinemaScopeTests
{
    [Fact]
    public void Product_HasRequiredCinemaId()
    {
        var property = typeof(Product).GetProperty("CinemaID");

        Assert.NotNull(property);
        Assert.Equal(typeof(int), property!.PropertyType);
    }

    [Fact]
    public void AvailableProducts_RequiresCinemaId()
    {
        var method = typeof(ProductController).GetMethod(
            nameof(ProductController.GetAvailableProducts))!;

        Assert.Contains(method.GetParameters(), parameter =>
            parameter.Name == "cinemaId" && parameter.ParameterType == typeof(int));
    }

    [Fact]
    public void ProductController_UsesManagerCinemaScopeService()
    {
        var constructor = Assert.Single(typeof(ProductController).GetConstructors());

        Assert.Contains(constructor.GetParameters(), parameter =>
            parameter.ParameterType == typeof(IManagerCinemaScopeService));
    }

    [Fact]
    public async Task CreateProduct_AssignsManagerCinema()
    {
        var repository = new StubProductRepository();
        var service = new ProductService(repository, new StubImageStorageService());

        var result = await service.CreateProductAsync(
            2, "Popcorn", "snack", null, 50_000, 10, null, true, false);

        Assert.True(result.Succeeded);
        Assert.Equal(2, result.Product!.CinemaID);
        Assert.Equal(2, repository.NameCheckCinemaId);
    }

    [Fact]
    public async Task UpdateProduct_FromAnotherCinema_ReturnsForbidden()
    {
        var repository = new StubProductRepository
        {
            ExistingProduct = new Product { ItemID = 1, CinemaID = 1, ItemName = "Popcorn" }
        };
        var service = new ProductService(repository, new StubImageStorageService());

        var result = await service.UpdateProductAsync(
            1, 2, "Popcorn", "snack", null, 50_000, 10, null, true, false, "in_stock");

        Assert.False(result.Succeeded);
        Assert.Equal(CinemaScopeMessages.AccessDenied, result.ErrorMessage);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateProductImage_ValidImage_StoresCloudinaryDetails()
    {
        var repository = new StubProductRepository
        {
            ExistingProduct = new Product { ItemID = 1, CinemaID = 2, ItemName = "Popcorn" }
        };
        var imageStorage = new StubImageStorageService();
        var service = new ProductService(repository, imageStorage);

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await service.UpdateProductImageAsync(
            1, 2, stream, "popcorn.png", "image/png", stream.Length);

        Assert.True(result.Succeeded);
        Assert.Equal("https://example.com/products/popcorn.png", result.Product!.ImageURL);
        Assert.Equal("products/popcorn", result.Product.ImagePublicId);
        Assert.Equal("products", imageStorage.UploadFolder);
    }

    [Fact]
    public async Task UpdateProductImage_FromAnotherCinema_ReturnsForbidden()
    {
        var repository = new StubProductRepository
        {
            ExistingProduct = new Product { ItemID = 1, CinemaID = 1, ItemName = "Popcorn" }
        };
        var imageStorage = new StubImageStorageService();
        var service = new ProductService(repository, imageStorage);

        await using var stream = new MemoryStream([1]);
        var result = await service.UpdateProductImageAsync(
            1, 2, stream, "popcorn.png", "image/png", stream.Length);

        Assert.False(result.Succeeded);
        Assert.Equal(CinemaScopeMessages.AccessDenied, result.ErrorMessage);
        Assert.Null(imageStorage.UploadFolder);
    }

    private sealed class StubProductRepository : IProductRepository
    {
        public Product? ExistingProduct { get; init; }
        public int? NameCheckCinemaId { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<List<Product>> GetProductsAsync(
            int cinemaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Product>());
        public Task<List<Product>> GetAvailableProductsAsync(
            int cinemaId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Product>());
        public Task<Product?> GetByIdAsync(
            int itemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExistingProduct);
        public Task<bool> NameExistsAsync(
            int cinemaId, string itemName, int? excludingItemId = null,
            CancellationToken cancellationToken = default)
        {
            NameCheckCinemaId = cinemaId;
            return Task.FromResult(false);
        }
        public Task<Product> AddAsync(
            Product product, CancellationToken cancellationToken = default) =>
            Task.FromResult(product);
        public Task<Product?> UpdateAsync(
            Product product, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            return Task.FromResult<Product?>(product);
        }
        public Task<Product?> UpdateImageAsync(
            int itemId, string imageUrl, string imagePublicId,
            CancellationToken cancellationToken = default)
        {
            if (ExistingProduct is null || ExistingProduct.ItemID != itemId)
                return Task.FromResult<Product?>(null);

            ExistingProduct.ImageURL = imageUrl;
            ExistingProduct.ImagePublicId = imagePublicId;
            return Task.FromResult<Product?>(ExistingProduct);
        }
        public Task<bool> IsUsedInBookingsAsync(
            int itemId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> DeleteAsync(
            int itemId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private sealed class StubImageStorageService : IImageStorageService
    {
        public string? UploadFolder { get; private set; }

        public Task<StoredImageResult> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string folder,
            CancellationToken cancellationToken = default)
        {
            UploadFolder = folder;
            return Task.FromResult(new StoredImageResult(
                "https://example.com/products/popcorn.png",
                "products/popcorn"));
        }

        public Task DeleteImageAsync(
            string publicId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
