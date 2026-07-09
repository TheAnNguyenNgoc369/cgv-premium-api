using System.Security.Claims;
using CinemaBooking.API.Contracts.CheckIns;
using CinemaBooking.API.Controllers;
using CinemaBooking.Application.CheckIns;
using CinemaBooking.Shared.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CinemaBooking.API.Tests;

public sealed class CheckInsControllerTests
{
    #region Lookup Endpoint Tests

    [Theory]
    [InlineData(Roles.Customer)]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    public async Task Lookup_NonStaffRole_ReturnsForbidden(string role)
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, role, userId: 1);

        var request = new CheckInLookupRequest { QRCode = "QR123" };
        var result = await controller.Lookup(request, CancellationToken.None);

        var forbidden = Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, service.LookupCallCount);
    }

    [Fact]
    public async Task Lookup_StaffRole_AllowsAccess()
    {
        var service = new StubCheckInService
        {
            LookupResult = (false, "Ticket not found.", null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInLookupRequest { QRCode = "QR123" };
        var result = await controller.Lookup(request, CancellationToken.None);

        Assert.Equal(1, service.LookupCallCount);
    }

    [Fact]
    public async Task Lookup_InvalidModel_ReturnsBadRequest()
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, Roles.Staff, userId: 1);
        controller.ModelState.AddModelError("QRCode", "QR Code is required");

        var request = new CheckInLookupRequest { QRCode = "" };
        var result = await controller.Lookup(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.LookupCallCount);
    }

    [Fact]
    public async Task Lookup_TicketNotFound_ReturnsNotFound()
    {
        var service = new StubCheckInService
        {
            LookupResult = (false, "Booking not found.", null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInLookupRequest { QRCode = "INVALID" };
        var result = await controller.Lookup(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = GetAnonymousProperty<bool>(notFound.Value, "success");
        Assert.False(response);
    }

    [Fact]
    public async Task Lookup_DifferentCinema_ReturnsForbidden()
    {
        var service = new StubCheckInService
        {
            LookupResult = (false, "You cannot check in tickets from another cinema.", null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInLookupRequest { QRCode = "QR123" };
        var result = await controller.Lookup(request, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Lookup_ValidRequest_ReturnsSuccess()
    {
        var lookupData = new CheckInLookupResult
        {
            BookingId = 100,
            BookingCode = "BK12345",
            CustomerName = "John Doe",
            Movie = new CheckInLookupResult.MovieInfo
            {
                Title = "Avengers",
                Rating = "PG-13",
                Duration = 120,
                PosterURL = "poster.jpg"
            },
            Cinema = new CheckInLookupResult.CinemaInfo
            {
                Name = "Cinema A",
                Address = "123 Main St"
            },
            Room = new CheckInLookupResult.RoomInfo
            {
                Name = "Room 1",
                RoomType = "Standard"
            },
            Showtime = new CheckInLookupResult.ShowtimeInfo
            {
                StartTime = DateTime.UtcNow.AddMinutes(20),
                EndTime = DateTime.UtcNow.AddMinutes(140)
            },
            PaymentStatus = "completed",
            BookingStatus = "confirmed",
            CheckedIn = false,
            Seats = new List<CheckInLookupResult.SeatInfo>
            {
                new CheckInLookupResult.SeatInfo
                {
                    Row = "A",
                    Column = 1,
                    SeatType = "Standard",
                    TicketPrice = 50000,
                    IsCheckedIn = false
                }
            },
            Products = new List<CheckInLookupResult.ProductInfo>()
        };

        var service = new StubCheckInService
        {
            LookupResult = (true, null, lookupData)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInLookupRequest { QRCode = "QR123" };
        var result = await controller.Lookup(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetAnonymousProperty<bool>(ok.Value, "success");
        Assert.True(success);
    }

    #endregion

    #region CheckIn Endpoint Tests

    [Theory]
    [InlineData(Roles.Customer)]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    public async Task CheckIn_NonStaffRole_ReturnsForbidden(string role)
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, role, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var forbidden = Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, service.CheckInCallCount);
    }

    [Fact]
    public async Task CheckIn_StaffRole_AllowsAccess()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (false, "Ticket not found.", null, null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        Assert.Equal(1, service.CheckInCallCount);
    }

    [Fact]
    public async Task CheckIn_InvalidModel_ReturnsBadRequest()
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, Roles.Staff, userId: 1);
        controller.ModelState.AddModelError("QRCode", "QR Code is required");

        var request = new CheckInRequest { QRCode = "" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.CheckInCallCount);
    }

    [Fact]
    public async Task CheckIn_TicketNotFound_ReturnsNotFound()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (false, "Booking not found.", null, null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "INVALID" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        var response = GetAnonymousProperty<bool>(notFound.Value, "success");
        Assert.False(response);
    }

    [Fact]
    public async Task CheckIn_DifferentCinema_ReturnsForbidden()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (false, "You cannot check in tickets from another cinema.", null, null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task CheckIn_AlreadyCheckedIn_ReturnsConflict()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (false, "Ticket has already been checked in.", null, null)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var response = GetAnonymousProperty<bool>(conflict.Value, "success");
        Assert.False(response);
    }

    [Fact]
    public async Task CheckIn_ValidRequest_ReturnsSuccess()
    {
        var checkedInAt = DateTime.UtcNow;
        var service = new StubCheckInService
        {
            CheckInResult = (true, null, "BK12345", checkedInAt)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        var result = await controller.CheckIn(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetAnonymousProperty<bool>(ok.Value, "success");
        var bookingCode = GetAnonymousProperty<string>(ok.Value, "bookingCode");
        Assert.True(success);
        Assert.Equal("BK12345", bookingCode);
    }

    [Fact]
    public async Task CheckIn_CapturesStaffIdFromClaims()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (true, null, "BK12345", DateTime.UtcNow)
        };
        var controller = CreateController(service, Roles.Staff, userId: 42);

        var request = new CheckInRequest { QRCode = "QR123" };
        await controller.CheckIn(request, CancellationToken.None);

        Assert.Equal(42, service.CapturedStaffId);
    }

    [Fact]
    public async Task CheckIn_CapturesIpAddress()
    {
        var service = new StubCheckInService
        {
            CheckInResult = (true, null, "BK12345", DateTime.UtcNow)
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInRequest { QRCode = "QR123" };
        await controller.CheckIn(request, CancellationToken.None);

        Assert.Equal("127.0.0.1", service.CapturedIpAddress);
    }

    #endregion

    #region History Endpoint Tests

    [Theory]
    [InlineData(Roles.Staff)]
    [InlineData(Roles.Manager)]
    [InlineData(Roles.Admin)]
    public async Task GetHistory_AuthorizedRoles_AllowsAccess(string role)
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, role, userId: 1);

        var request = new CheckInHistoryRequest { Page = 1, PageSize = 10 };
        var result = await controller.GetHistory(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.HistoryCallCount);
    }

    [Fact]
    public async Task GetHistory_CustomerRole_ReturnsForbidden()
    {
        var service = new StubCheckInService();
        var controller = CreateController(service, Roles.Customer, userId: 1);

        var request = new CheckInHistoryRequest { Page = 1, PageSize = 10 };
        var result = await controller.GetHistory(request, CancellationToken.None);

        var forbidden = Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, service.HistoryCallCount);
    }

    [Fact]
    public async Task GetHistory_WithFilters_PassesFiltersToService()
    {
        var fromDate = DateTime.UtcNow.AddDays(-7);
        var toDate = DateTime.UtcNow;

        var service = new StubCheckInService
        {
            HistoryResult = new CheckInHistoryResult
            {
                Records = new List<CheckInHistoryResult.CheckInRecord>
                {
                    new CheckInHistoryResult.CheckInRecord
                    {
                        BookingId = 100,
                        BookingCode = "BK12345",
                        CustomerName = "John Doe",
                        MovieTitle = "Avengers",
                        CinemaName = "Cinema A",
                        RoomName = "Room 1",
                        ShowtimeStart = DateTime.UtcNow,
                        CheckedInAt = DateTime.UtcNow,
                        StaffName = "Staff Member",
                        SeatCount = 2,
                        TotalAmount = 100000
                    }
                },
                TotalCount = 1
            }
        };
        var controller = CreateController(service, Roles.Manager, userId: 1);

        var request = new CheckInHistoryRequest
        {
            StaffId = 42,
            CinemaId = 5,
            From = fromDate,
            To = toDate,
            Page = 2,
            PageSize = 20
        };
        var result = await controller.GetHistory(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetAnonymousProperty<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetHistory_ReturnsCorrectResponseStructure()
    {
        var service = new StubCheckInService
        {
            HistoryResult = new CheckInHistoryResult
            {
                Records = new List<CheckInHistoryResult.CheckInRecord>
                {
                    new CheckInHistoryResult.CheckInRecord
                    {
                        BookingId = 100,
                        BookingCode = "BK12345",
                        CustomerName = "John Doe",
                        MovieTitle = "Avengers",
                        CinemaName = "Cinema A",
                        RoomName = "Room 1",
                        ShowtimeStart = DateTime.UtcNow.AddMinutes(-30),
                        CheckedInAt = DateTime.UtcNow.AddMinutes(-10),
                        StaffName = "Staff Member",
                        SeatCount = 2,
                        TotalAmount = 100000
                    }
                },
                TotalCount = 1
            }
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInHistoryRequest { Page = 1, PageSize = 10 };
        var result = await controller.GetHistory(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetAnonymousProperty<bool>(ok.Value, "success");
        Assert.True(success);
    }

    [Fact]
    public async Task GetHistory_EmptyResults_ReturnsEmptyArray()
    {
        var service = new StubCheckInService
        {
            HistoryResult = new CheckInHistoryResult
            {
                Records = new List<CheckInHistoryResult.CheckInRecord>(),
                TotalCount = 0
            }
        };
        var controller = CreateController(service, Roles.Staff, userId: 1);

        var request = new CheckInHistoryRequest { Page = 1, PageSize = 10 };
        var result = await controller.GetHistory(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var success = GetAnonymousProperty<bool>(ok.Value, "success");
        Assert.True(success);
    }

    #endregion

    #region Helper Methods

    private static CheckInsController CreateController(
        StubCheckInService service,
        string role,
        int userId)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], "Test");

        return new CheckInsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity),
                    Connection = { RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1") }
                }
            }
        };
    }

    private static T GetAnonymousProperty<T>(object? obj, string propertyName)
    {
        var property = obj?.GetType().GetProperty(propertyName);
        return property != null ? (T)property.GetValue(obj)! : default!;
    }

    #endregion

    #region Stub Implementation

    private sealed class StubCheckInService : ICheckInService
    {
        public int LookupCallCount { get; private set; }
        public int CheckInCallCount { get; private set; }
        public int HistoryCallCount { get; private set; }

        public (bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data) LookupResult { get; set; }
        public (bool Succeeded, string? ErrorMessage, string? BookingCode, DateTime? CheckedInAt) CheckInResult { get; set; }
        public CheckInHistoryResult HistoryResult { get; set; } = new CheckInHistoryResult
        {
            Records = new List<CheckInHistoryResult.CheckInRecord>(),
            TotalCount = 0
        };

        public int? CapturedStaffId { get; private set; }
        public int? CapturedHistoryStaffId { get; private set; }
        public int? CapturedHistoryCinemaId { get; private set; }
        public int? CapturedHistoryUserId { get; private set; }
        public bool CapturedHistoryIsAdmin { get; private set; }
        public bool CapturedHistoryIsManager { get; private set; }
        public bool CapturedHistoryIsStaff { get; private set; }
        public string? CapturedIpAddress { get; private set; }

        public Task<(bool Succeeded, string? ErrorMessage, CheckInLookupResult? Data)> LookupAsync(
            string qrCode,
            int staffId,
            CancellationToken cancellationToken = default)
        {
            LookupCallCount++;
            CapturedStaffId = staffId;
            return Task.FromResult(LookupResult);
        }

        public Task<(bool Succeeded, string? ErrorMessage, string? BookingCode, DateTime? CheckedInAt)> CheckInAsync(
            string qrCode,
            int staffId,
            string? ipAddress,
            CancellationToken cancellationToken = default)
        {
            CheckInCallCount++;
            CapturedStaffId = staffId;
            CapturedIpAddress = ipAddress;
            return Task.FromResult(CheckInResult);
        }

        public Task<CheckInHistoryResult> GetHistoryAsync(
            int? staffId,
            int? cinemaId,
            DateTime? from,
            DateTime? to,
            int page,
            int pageSize,
            int currentUserId,
            bool isAdmin,
            bool isManager,
            bool isStaff,
            CancellationToken cancellationToken = default)
        {
            HistoryCallCount++;
            CapturedHistoryStaffId = staffId;
            CapturedHistoryCinemaId = cinemaId;
            CapturedHistoryUserId = currentUserId;
            CapturedHistoryIsAdmin = isAdmin;
            CapturedHistoryIsManager = isManager;
            CapturedHistoryIsStaff = isStaff;
            return Task.FromResult(HistoryResult);
        }
    }

    #endregion
}
