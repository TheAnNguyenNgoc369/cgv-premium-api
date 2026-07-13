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
        long fileSize)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "Image file is required";
        }

        if (fileSize <= 0)
        {
            return "Image file is empty";
        }

        if (fileSize > MaxFileSizeBytes)
        {
            return "Image file must not exceed 5 MB";
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
