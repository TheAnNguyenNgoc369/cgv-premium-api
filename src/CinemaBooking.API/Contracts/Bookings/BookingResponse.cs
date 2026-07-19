namespace CinemaBooking.API.Contracts.Bookings;

public sealed record HoldSeatsResponse(
    List<int> HoldIds,
    DateTime ExpiresAt
);

public sealed record BookingResponse(
    int BookingID,
    string BookingCode,
    int? ShowtimeID,
    string MovieTitle,
    DateTime StartTime,
    string CinemaName,
    string RoomName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Status,
    DateTime BookingDate,
    List<BookingSeatResponse> Seats,
    List<BookingFnBResponse> FnbItems,
    BookingVoucherResponse? VoucherApplied,
    bool HasReviewed,
    int? ReviewId
);

public sealed record MyBookingResponse(
    int BookingID,
    string BookingCode,
    int? ShowtimeID,
    BookingMovieResponse Movie,
    DateTime StartTime,
    string CinemaName,
    string RoomName,
    decimal SubTotal,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Status,
    DateTime BookingDate,
    List<BookingSeatResponse> Seats,
    List<BookingFnBResponse> FnbItems,
    BookingVoucherResponse? VoucherApplied,
    bool HasReviewed,
    int? ReviewId
);

public sealed record BookingMovieResponse(
    string Title,
    string? PosterUrl,
    string AgeRating,
    int DurationMinutes
);

public sealed record BookingSeatResponse(
    int SeatID,
    string SeatRow,
    int SeatCol,
    decimal TicketPrice
);

public sealed record BookingFnBResponse(
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal SubTotal
);

public sealed record BookingVoucherResponse(
    string VoucherCode,
    decimal DiscountApplied
);
