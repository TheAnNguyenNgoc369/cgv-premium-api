namespace CinemaBooking.Application.Common.Interfaces;

public interface IImageStorageService
{
    Task<StoredImageResult> UploadImageAsync(
        Stream imageStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default);
}
