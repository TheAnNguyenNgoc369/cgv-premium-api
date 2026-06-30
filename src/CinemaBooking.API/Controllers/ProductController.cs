using CinemaBooking.API.Contracts.Products;
using CinemaBooking.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api")]
public sealed class ProductController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;

    public ProductController(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetAvailableProducts(CancellationToken cancellationToken)
    {
        var products = await _bookingRepository.GetAvailableProductsAsync(cancellationToken);

        var productResponses = products.Select(p => new ProductResponse(
            p.ItemID,
            p.ItemName,
            p.ItemType,
            p.Description,
            p.Price,
            p.ImageURL,
            p.IsLoyaltyEligible
        )).ToList();

        return Ok(new ProductListResponse(productResponses));
    }
}
