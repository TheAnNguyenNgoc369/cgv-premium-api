namespace CinemaBooking.Application.Reports;

public sealed record TopSellingFnbProduct(
    int ProductId,
    string ProductName,
    int QuantitySold);
