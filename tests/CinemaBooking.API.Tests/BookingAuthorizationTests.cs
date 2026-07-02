using System.Security.Claims;
using CinemaBooking.API.Contracts.Bookings;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.Bookings;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class BookingAuthorizationTests
{
    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    public async Task CreateBooking_AdminOrManager_ReturnsForbidden(string role)
    {
        var service = new StubBookingService();
        var controller = CreateController(service, role);

        var result = await controller.CreateBooking(CreateRequest(), CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Equal(0, service.CreateBookingCallCount);
        Assert.Equal(
            "You do not have permission to create a booking.",
            forbidden.Value?.GetType().GetProperty("message")?.GetValue(forbidden.Value));
    }

    [Theory]
    [InlineData(Roles.Customer)]
    [InlineData(Roles.Staff)]
    public async Task CreateBooking_CustomerOrStaff_ReachesBookingService(string role)
    {
        var service = new StubBookingService();
        var controller = CreateController(service, role);

        var result = await controller.CreateBooking(CreateRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(1, service.CreateBookingCallCount);
    }

    private static BookingController CreateController(
        StubBookingService service,
        string role)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("userId", "1"),
            new Claim(ClaimTypes.Role, role)
        ], "Test");

        return new BookingController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            }
        };
    }

    private static CreateBookingRequest CreateRequest()
    {
        return new CreateBookingRequest
        {
            ShowtimeId = 1,
            SeatIds = [1]
        };
    }

    private sealed class StubBookingService : IBookingService
    {
        public int CreateBookingCallCount { get; private set; }

        public Task<(bool Succeeded, string? ErrorMessage, List<int>? HoldIds, DateTime? ExpiresAt, SeatValidationErrors? SeatErrors)> HoldSeatsAsync(
            int userId,
            int showtimeId,
            List<int> seatIds,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<(bool Succeeded, string? ErrorMessage)> ReleaseSeatHoldsAsync(
            int userId,
            int showtimeId,
            List<int> seatIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(bool Succeeded, string? ErrorMessage, Booking? Booking, SeatValidationErrors? SeatErrors)> CreateBookingAsync(
            int actorUserId,
            int? customerId,
            bool isStaff,
            int showtimeId,
            List<int> seatIds,
            List<BookingFnBItemDto> fnbItems,
            string? voucherCode,
            CancellationToken cancellationToken = default)
        {
            CreateBookingCallCount++;
            return Task.FromResult<(bool, string?, Booking?, SeatValidationErrors?)>(
                (false, "Booking validation stopped the request.", null, null));
        }

        public Task<Booking?> GetBookingByIdAsync(
            int bookingId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<List<Booking>> GetMyBookingsAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
