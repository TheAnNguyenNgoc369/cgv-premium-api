using Microsoft.AspNetCore.Http;

namespace CinemaBooking.API.Contracts.Images;

public sealed class ImageUploadRequest
{
    public IFormFile? File { get; set; }
}
