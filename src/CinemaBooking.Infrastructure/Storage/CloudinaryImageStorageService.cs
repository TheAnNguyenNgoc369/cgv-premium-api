using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Infrastructure.Configuration;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Infrastructure.Storage;

public sealed class CloudinaryImageStorageService : IImageStorageService
{
    private readonly CloudinarySettings _settings;

    public CloudinaryImageStorageService(IOptions<CloudinarySettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<StoredImageResult> UploadImageAsync(
        Stream imageStream,
        string fileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var cloudinary = CreateClient();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, imageStream),
            Folder = folder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }

        if (string.IsNullOrWhiteSpace(result.SecureUrl?.ToString())
            || string.IsNullOrWhiteSpace(result.PublicId))
        {
            throw new InvalidOperationException("Cloudinary did not return image upload details.");
        }

        return new StoredImageResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteImageAsync(
        string publicId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return;
        }

        var cloudinary = CreateClient();
        var deletionParams = new DeletionParams(publicId);
        var result = await cloudinary.DestroyAsync(deletionParams);

        if (result.Error is not null)
        {
            throw new InvalidOperationException(result.Error.Message);
        }
    }

    private Cloudinary CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_settings.CloudName)
            || string.IsNullOrWhiteSpace(_settings.ApiKey)
            || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary configuration is incomplete.");
        }

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret);

        return new Cloudinary(account)
        {
            Api =
            {
                Secure = true
            }
        };
    }
}
