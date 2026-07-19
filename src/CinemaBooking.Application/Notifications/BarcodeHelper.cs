using BarcodeStandard;
using SkiaSharp;

namespace CinemaBooking.Application.Notifications;

internal static class BarcodeHelper
{
    public static byte[] GenerateCode128(string data, int width = 560, int height = 160)
    {
        var barcode = new Barcode
        {
            IncludeLabel = false,
            Alignment = AlignmentPositions.Center,
            Width = width,
            Height = height,
            ForeColor = SKColors.Black,
            BackColor = SKColors.White
        };

        barcode.Encode(BarcodeStandard.Type.Code128, data);
        return barcode.EncodedImageBytes;
    }
}