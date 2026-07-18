using System.Text.Json.Serialization;

namespace CinemaBooking.API.Contracts.Uploads;

public sealed record PersonPhotoUploadResponse(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("publicId")] string PublicId);
