using CinemaBooking.API.Contracts.Products;
using CinemaBooking.Application.Products;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IManagerCinemaScopeService _managerCinemaScopeService;

    public ProductController(
        IProductService productService,
        IManagerCinemaScopeService managerCinemaScopeService)
    {
        _productService = productService;
        _managerCinemaScopeService = managerCinemaScopeService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var cinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!cinemaId.HasValue) return CinemaScopeForbidden();
        var products = await _productService.GetProductsAsync(cinemaId.Value, cancellationToken);

        return Ok(new ProductListResponse(products.Select(ToResponse).ToList()));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableProducts(
        [FromQuery] int cinemaId,
        CancellationToken cancellationToken)
    {
        if (cinemaId <= 0)
            return BadRequest(new { success = false, message = "CinemaId must be greater than 0" });
        var products = await _productService.GetAvailableProductsAsync(cinemaId, cancellationToken);

        return Ok(new ProductListResponse(products.Select(ToResponse).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProductById(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);

        if (product is null)
        {
            return NotFound(new { success = false, message = "Product not found" });
        }

        return Ok(ToResponse(product));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var cinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!cinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _productService.CreateProductAsync(
            cinemaId.Value,
            request.ItemName,
            request.ItemType,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.ImageURL,
            request.IsOnMenu,
            request.IsLoyaltyEligible,
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        var response = ToResponse(result.Product!);

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = response.ItemID },
            response);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var cinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!cinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _productService.UpdateProductAsync(
            id,
            cinemaId.Value,
            request.ItemName,
            request.ItemType,
            request.Description,
            request.Price,
            request.StockQuantity,
            request.ImageURL,
            request.IsOnMenu,
            request.IsLoyaltyEligible,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Product not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            if (result.ErrorMessage == CinemaScopeMessages.AccessDenied)
                return CinemaScopeForbidden();

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToResponse(result.Product!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Manager)]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var cinemaId = await GetManagerCinemaIdAsync(cancellationToken);
        if (!cinemaId.HasValue) return CinemaScopeForbidden();
        var result = await _productService.DeleteProductAsync(
            id, cinemaId.Value, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Product not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            if (result.ErrorMessage == CinemaScopeMessages.AccessDenied)
                return CinemaScopeForbidden();

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        return NoContent();
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.ItemID,
            product.CinemaID,
            product.ItemName,
            product.ItemType,
            product.Description,
            product.Price,
            product.StockQuantity,
            product.ImageURL,
            product.IsOnMenu,
            product.IsLoyaltyEligible,
            product.Status,
            product.UpdatedAt);
    }

    private async Task<int?> GetManagerCinemaIdAsync(CancellationToken cancellationToken) =>
        int.TryParse(User.FindFirst("userId")?.Value, out var userId)
            ? await _managerCinemaScopeService.GetAssignedCinemaIdAsync(userId, cancellationToken)
            : null;

    private ObjectResult CinemaScopeForbidden() => StatusCode(
        StatusCodes.Status403Forbidden,
        new { success = false, message = CinemaScopeMessages.AccessDenied });
}
