using CinemaBooking.API.Contracts.Products;
using CinemaBooking.API.Contracts.Images;
using CinemaBooking.Application.Products;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsAsync(cancellationToken);

        return Ok(new ProductListResponse(products.Select(ToResponse).ToList()));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableProducts(
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetAvailableProductsAsync(cancellationToken);

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
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.CreateProductAsync(
            request.ItemName,
            request.ItemType,
            request.Description,
            request.Price,
            request.ImageURL,
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
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateProduct(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _productService.UpdateProductAsync(
            id,
            request.ItemName,
            request.ItemType,
            request.Description,
            request.Price,
            request.ImageURL,
            request.IsLoyaltyEligible,
            request.Status,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Product not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToResponse(result.Product!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteProduct(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _productService.DeleteProductAsync(id, cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Product not found")
            {
                return NotFound(new { success = false, message = result.ErrorMessage });
            }

            return Conflict(new { success = false, message = result.ErrorMessage });
        }

        return NoContent();
    }

    [HttpPut("{id:int}/image")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> UpdateProductImage(
        int id,
        [FromForm] ImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null)
            return BadRequest(new { success = false, message = "Product image file is required" });

        await using var stream = request.File.OpenReadStream();
        var result = await _productService.UpdateProductImageAsync(
            id,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            cancellationToken);

        if (!result.Succeeded)
        {
            if (result.ErrorMessage == "Product not found")
                return NotFound(new { success = false, message = result.ErrorMessage });

            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(ToResponse(result.Product!));
    }

    private static ProductResponse ToResponse(Product product)
    {
        return new ProductResponse(
            product.ItemID,
            product.ItemName,
            product.ItemType,
            product.Description,
            product.Price,
            product.ImageURL,
            product.IsLoyaltyEligible,
            product.Status,
            product.UpdatedAt);
    }

}
