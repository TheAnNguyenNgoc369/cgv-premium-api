using CinemaBooking.API.Contracts.Images;
using CinemaBooking.API.Contracts.Uploads;
using CinemaBooking.Application.Common.ImageFiles;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Controllers;

[ApiController, Route("api/uploads")]
public sealed class UploadController : ControllerBase
{
    private const string VoucherImageFolder = "cgvp/vouchers";
    private readonly IImageStorageService _imageStorage;
    public UploadController(IImageStorageService imageStorage) => _imageStorage = imageStorage;

    /// <summary>
    /// Upload a voucher image and get back the URL + public id to attach to a voucher.
    /// </summary>
    [HttpPost("vouchers/image"), Authorize(Roles = Roles.Admin), Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVoucherImage([FromForm] ImageUploadRequest request, CancellationToken ct)
    {
        if (request.File is null)
            return BadRequest(new { success = false, message = "Image file is required" });

        var error = ImageFileValidator.Validate(request.File.FileName, request.File.ContentType, request.File.Length);
        if (error is not null) return BadRequest(new { success = false, message = error });

        await using var stream = request.File.OpenReadStream();
        var uploaded = await _imageStorage.UploadImageAsync(stream, request.File.FileName, VoucherImageFolder, ct);
        return Ok(new ImageUploadResponse(uploaded.SecureUrl, uploaded.PublicId));
    }
}
