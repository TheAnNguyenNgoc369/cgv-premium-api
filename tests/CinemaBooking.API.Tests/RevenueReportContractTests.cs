using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class RevenueReportContractTests
{
    [Fact]
    public void RevenueEndpoint_UsesExpectedRouteAndQueryParameters()
    {
        var method = typeof(ReportsController).GetMethod(nameof(ReportsController.Revenue));
        var route = Assert.Single(method!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>());

        Assert.Equal("revenue", route.Template);
        Assert.Equal(["startDate", "endDate", "groupBy", "cinemaId", "ct"],
            method.GetParameters().Select(parameter => parameter.Name));
    }

    [Fact]
    public void RevenueResponses_ExposeGroupSpecificFields()
    {
        var day = new DailyRevenue(new DateOnly(2026, 6, 1), 1_280_000, 980_000, 300_000, 35, 172);
        var week = new WeeklyRevenue("2026-W22", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 7),
            8_900_000, 6_500_000, 2_400_000, 210, 1_100);
        var month = new MonthlyRevenue("2026-06", 45_200_000, 32_000_000, 13_200_000, 980, 5_100);

        Assert.Equal(new DateOnly(2026, 6, 1), day.Date);
        Assert.Equal("2026-W22", week.Week);
        Assert.Equal("2026-06", month.Month);
        Assert.Equal(day.Revenue, day.TicketRevenue + day.FnbRevenue);
    }
}
