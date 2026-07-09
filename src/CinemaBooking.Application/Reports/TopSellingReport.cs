namespace CinemaBooking.Application.Reports;

public sealed record TopSellingReport(
    IReadOnlyList<TopSellingMovie> Movies,
    IReadOnlyList<TopSellingFnbProduct> FnbProducts,
    IReadOnlyList<TopCinema> Cinemas);
