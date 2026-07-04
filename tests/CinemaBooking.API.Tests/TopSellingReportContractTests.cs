using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class TopSellingReportContractTests
{
    [Fact]
    public void TopSellingEndpoint_UsesDedicatedRouteWithoutChangingExistingReports()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.TopSelling));
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>());

        Assert.Equal("top-selling", route.Template);
        Assert.DoesNotContain(method.GetParameters(), parameter => parameter.Name == "top");
    }

    [Fact]
    public void TopSellingResponse_ContainsAllThreeRankings()
    {
        var response = new TopSellingReport(
            [new TopSellingMovie(1, "Movie", 20)],
            [new TopSellingFnbProduct(2, "Popcorn", 10)],
            [new TopCinema(3, "Cinema", 5, 20)]);

        Assert.Single(response.Movies);
        Assert.Single(response.FnbProducts);
        Assert.Single(response.Cinemas);
        Assert.Equal(20, response.Cinemas[0].TicketsSold);
    }
}
