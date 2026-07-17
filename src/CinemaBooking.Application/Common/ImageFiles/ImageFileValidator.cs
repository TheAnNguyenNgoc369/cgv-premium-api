namespace CinemaBooking.Application.Common.ImageFiles;

public static class ImageFileValidator
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public static string? Validate(
        string fileName,
        string? contentType,
        long fileSize,
        long? maxFileSizeBytes = null)
    {
        var effectiveMaxSize = maxFileSizeBytes ?? MaxFileSizeBytes;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Image file is required";
        }

        if (fileSize <= 0)
        {
            return "Image file is empty";
        }

        if (fileSize > effectiveMaxSize)
        {
            var maxMb = effectiveMaxSize / (1024 * 1024);
            return $"Image file must not exceed {maxMb} MB";
        }

        var extension = Path.GetExtension(fileName);

        if (!AllowedExtensions.Contains(extension))
        {
            return "Image file type must be jpg, jpeg, png, or webp";
        }

        if (!string.IsNullOrWhiteSpace(contentType)
            && !AllowedContentTypes.Contains(contentType))
        {
            return "Image content type must be image/jpeg, image/png, or image/webp";
        }

        return null;
    }
}
