using CinemaBooking.Domain.Entities;
using CinemaBooking.Infrastructure.Reports;

namespace CinemaBooking.API.Tests;

public sealed class ReportTicketCapacityTests
{
    [Fact]
    public void CountTicketsSold_UsesSeatTypeCapacity()
    {
        var booking = new Booking
        {
            BookingSeats =
            [
                new BookingSeat { Seat = new Seat { SeatType = new SeatType { Capacity = 1 } } },
                new BookingSeat { Seat = new Seat { SeatType = new SeatType { Capacity = 2 } } }
            ]
        };

        Assert.Equal(3, ReportService.CountTicketsSold([booking]));
    }
}
