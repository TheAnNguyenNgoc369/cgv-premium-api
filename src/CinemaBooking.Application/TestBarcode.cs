using System.Reflection;
using QRCoder;

namespace CinemaBooking.Application;

public static class TestBarcode
{
    public static void Test()
    {
        var assembly = Assembly.Load("QRCoder");
        foreach (var type in assembly.GetTypes())
        {
            if (type.Name.Contains("Code128", StringComparison.OrdinalIgnoreCase) || 
                type.Name.Contains("Barcode", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Found: {type.FullName}");
            }
        }
    }
}