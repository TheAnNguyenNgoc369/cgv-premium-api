using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Domain.Entities;

namespace CinemaBooking.Application.Bookings;

public sealed class BookingService : IBookingService
{
    private const int HoldDurationMinutes = 10;

    private readonly IBookingRepository _bookingRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public BookingService(
        IBookingRepository bookingRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _bookingRepository = bookingRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<(bool Succeeded, string? ErrorMessage, List<int>? HoldIds, DateTime? ExpiresAt, SeatValidationErrors? SeatErrors)> HoldSeatsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        if (seatIds.Count == 0)
            return (false, "Please select at least one seat.", null, null, null);

        var showtime = await _bookingRepository.GetShowtimeAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return (false, "Showtime not found.", null, null, null);

        if (showtime.Status != "scheduled")
            return (false, "This showtime is not scheduled and is unavailable for seat holds.", null, null, null);

        if (showtime.StartTime <= DateTime.UtcNow.AddMinutes(15))
            return (false, "Seat holds close 15 minutes before the showtime starts.", null, null, null);

        if (showtime.Room.Status != "active")
            return (false, "The showtime room is inactive.", null, null, null);

        if (showtime.Room.Cinema.Status != "active")
            return (false, "The showtime cinema is inactive.", null, null, null);

        seatIds = seatIds.Distinct().ToList();

        var selectedSeats = await _bookingRepository.GetSeatsByIdsAsync(seatIds, cancellationToken);
        var seatErrors = GetSeatValidationErrors(seatIds, selectedSeats, showtime.RoomID);
        if (seatErrors is not null)
            return (false, "Some selected seats are invalid.", null, null, seatErrors);

        var unavailableSeatIds = await _bookingRepository.GetUnavailableSeatIdsAsync(
            showtimeId, seatIds, userId, cancellationToken);

        if (unavailableSeatIds.Count > 0)
            return (false, $"Seats with IDs {string.Join(", ", unavailableSeatIds)} are booked or held by another user.", null, null, null);

        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(HoldDurationMinutes);

        var holds = seatIds.Select(seatId => new SeatHold
        {
            SeatID = seatId,
            ShowtimeID = showtimeId,
            UserID = userId,
            HeldAt = now,
            ExpiresAt = expiresAt,
            Status = "holding"
        }).ToList();

        if (!await _bookingRepository.TryAddSeatHoldsAsync(holds, cancellationToken))
            return (false, "One or more seats are already booked or being held.", null, null, null);

        return (true, null, holds.Select(h => h.HoldID).ToList(), expiresAt, null);
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> ReleaseSeatHoldsAsync(
        int userId,
        int showtimeId,
        List<int> seatIds,
        CancellationToken cancellationToken = default)
    {
        var requestedSeatIds = seatIds.Distinct().ToHashSet();
        if (requestedSeatIds.Count == 0)
            return (false, "Please select at least one seat.");

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var activeHolds = await _bookingRepository.GetMyActiveHoldsForUpdateAsync(
                userId, showtimeId, DateTime.UtcNow, cancellationToken);
            var requestedHolds = activeHolds
                .Where(hold => requestedSeatIds.Contains(hold.SeatID))
                .ToList();

            if (requestedHolds.Count != requestedSeatIds.Count)
                return (false, "One or more seats are not actively held by the current user.");

            await _bookingRepository.ReleaseSeatHoldsAsync(requestedHolds, cancellationToken);
            return (true, (string?)null);
        }, cancellationToken);
    }

    public async Task<(bool Succeeded, string? ErrorMessage, Booking? Booking, SeatValidationErrors? SeatErrors)> CreateBookingAsync(
        int actorUserId,
        int? customerId,
        bool isStaff,
        int showtimeId,
        List<int> seatIds,
        List<BookingFnBItemDto> fnbItems,
        string? voucherCode,
        CancellationToken cancellationToken = default)
    {
        if (seatIds.Count == 0)
            return (false, "Please select at least one seat.", null, null);

        var showtime = await _bookingRepository.GetShowtimeAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return (false, "Showtime not found.", null, null);

        if (showtime.Status != "scheduled")
            return (false, "This showtime is not scheduled and is unavailable for booking.", null, null);

        if (showtime.StartTime <= DateTime.UtcNow)
            return (false, "This showtime has already started.", null, null);

        if (showtime.Room.Status != "active")
            return (false, "The showtime room is inactive.", null, null);

        if (showtime.Room.Cinema.Status != "active")
            return (false, "The showtime cinema is inactive.", null, null);

        seatIds = seatIds.Distinct().ToList();

        var myHolds = await _bookingRepository.GetMyActiveHoldsAsync(
            actorUserId, showtimeId, seatIds, cancellationToken);

        if (myHolds.Count != seatIds.Count)
            return (false, "Some seats are not held or the holds have expired. Please select them again.", null, null);

        var seats = await _bookingRepository.GetSeatsByIdsAsync(seatIds, cancellationToken);

        var seatErrors = GetSeatValidationErrors(seatIds, seats, showtime.RoomID);
        if (seatErrors is not null)
            return (false, "Some selected seats are invalid.", null, seatErrors);

        var bookingSeats = seats.Select(seat => new BookingSeat
        {
            SeatID = seat.SeatID,
            TicketPrice = showtime.BasePrice + seat.SeatType.ExtraPrice
        }).ToList();

        var seatsSubTotal = bookingSeats.Sum(bs => bs.TicketPrice);

        var bookingFnBs = new List<BookingFnB>();
        var fnbSubTotal = 0m;
        var productQuantities = new Dictionary<int, int>();

        var normalizedFnbItems = fnbItems
            .GroupBy(item => item.ItemId)
            .Select(group => new BookingFnBItemDto(group.Key, group.Sum(item => item.Quantity)))
            .ToList();

        if (normalizedFnbItems.Any())
        {
            var productIds = normalizedFnbItems.Select(f => f.ItemId).ToList();
            var products = await _bookingRepository.GetProductsByIdsAsync(productIds, cancellationToken);

            if (products.Count != productIds.Count)
            {
                var missingIds = productIds.Except(products.Select(p => p.ItemID)).ToList();
                return (false, $"Products with IDs {string.Join(", ", missingIds)} were not found.", null, null);
            }

            foreach (var fnbItem in normalizedFnbItems)
            {
                var product = products.First(p => p.ItemID == fnbItem.ItemId);

                if (product.CinemaID != showtime.Room.CinemaID)
                    return (false, $"Product '{product.ItemName}' is not available at this cinema.", null, null);

                if (!product.IsOnMenu)
                    return (false, $"Product '{product.ItemName}' is no longer available.", null, null);

                if (product.Status != "in_stock" && product.Status != "low_stock")
                    return (false, $"Product '{product.ItemName}' is out of stock.", null, null);

                if (product.StockQuantity < fnbItem.Quantity)
                    return (false, $"Only {product.StockQuantity} units of '{product.ItemName}' are available.", null, null);

                var itemSubTotal = product.Price * fnbItem.Quantity;
                fnbSubTotal += itemSubTotal;

                bookingFnBs.Add(new BookingFnB
                {
                    ItemID = product.ItemID,
                    Quantity = fnbItem.Quantity,
                    UnitPrice = product.Price,
                    SubTotal = itemSubTotal
                });

                productQuantities[product.ItemID] = fnbItem.Quantity;
            }
        }

        var totalBeforeDiscount = seatsSubTotal + fnbSubTotal;

        User? user = null;
        if (customerId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(customerId.Value, cancellationToken);
            if (user is null || user.Role != "customer")
                return (false, "Customer account was not found.", null, null);
        }

        if (!isStaff && customerId != actorUserId)
            return (false, "Customers can only create bookings for their own account.", null, null);

        var membershipDiscount = 0m;
        if (user?.LoyaltyTier is not null)
        {
            membershipDiscount = Math.Round(totalBeforeDiscount * user.LoyaltyTier.DiscountRate, 0);
        }

        var discountAmount = membershipDiscount;
        BookingVoucher? bookingVoucher = null;

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            if (!customerId.HasValue)
                return (false, "Guest bookings cannot use vouchers.", null, null);

            var voucher = await _bookingRepository.GetVoucherByCodeAsync(voucherCode.Trim(), cancellationToken);

            if (voucher is null)
                return (false, "Voucher code does not exist.", null, null);

            if (!voucher.IsActive)
                return (false, "Voucher is not available.", null, null);

            var now = DateTime.UtcNow;
            if (now < voucher.ValidFrom)
                return (false, "Voucher is not active yet.", null, null);

            if (now > voucher.ValidUntil)
                return (false, "Voucher has expired.", null, null);

            if (voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value)
                return (false, "Voucher usage limit has been reached.", null, null);

            if (voucher.MinOrderValue.HasValue && totalBeforeDiscount < voucher.MinOrderValue.Value)
                return (false, $"A minimum order value of {voucher.MinOrderValue.Value:N0} VND is required to use this voucher.", null, null);

            var voucherDiscount = voucher.DiscountType == "percent"
                ? Math.Round(totalBeforeDiscount * voucher.DiscountValue / 100, 0)
                : voucher.DiscountValue;

            discountAmount += voucherDiscount;

            if (discountAmount > totalBeforeDiscount)
                discountAmount = totalBeforeDiscount;

            bookingVoucher = new BookingVoucher
            {
                VoucherID = voucher.VoucherID,
                DiscountApplied = voucherDiscount,
                UsedAt = DateTime.UtcNow
            };
        }

        var finalAmount = totalBeforeDiscount - discountAmount;

        var booking = new Booking
        {
            BookingCode = GenerateBookingCode(),
            UserID = customerId,
            CreatedByStaffID = isStaff ? actorUserId : null,
            ShowtimeID = showtimeId,
            SubTotal = totalBeforeDiscount,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            Status = "pending",
            BookingDate = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BookingSeats = bookingSeats,
            BookingFnBs = bookingFnBs
        };

        if (bookingVoucher is not null)
        {
            booking.BookingVoucher = bookingVoucher;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (productQuantities.Count > 0)
            {
                var lockedProducts = await _bookingRepository.GetProductsByIdsWithLockAsync(
                    productQuantities.Keys.ToList(), cancellationToken);

                foreach (var item in productQuantities)
                {
                    var product = lockedProducts.FirstOrDefault(p => p.ItemID == item.Key);
                    if (product is null)
                        throw new InvalidOperationException($"Product with ID {item.Key} not found.");

                    if (product.CinemaID != showtime.Room.CinemaID)
                        throw new InvalidOperationException(
                            $"Product '{product.ItemName}' is not available at this cinema.");

                    if (!product.IsOnMenu)
                        throw new InvalidOperationException($"Product '{product.ItemName}' is no longer available.");

                    if (product.Status != "in_stock" && product.Status != "low_stock")
                        throw new InvalidOperationException($"Product '{product.ItemName}' is out of stock.");

                    if (product.StockQuantity < item.Value)
                        throw new InvalidOperationException(
                            $"Insufficient stock for '{product.ItemName}'. Available: {product.StockQuantity}, Requested: {item.Value}");
                }
            }

            if (bookingVoucher is not null)
            {
                var lockedVoucher = await _bookingRepository.GetVoucherByCodeWithLockAsync(
                    voucherCode!.Trim(), cancellationToken);

                if (lockedVoucher is null)
                    throw new InvalidOperationException("Voucher no longer exists.");

                if (!lockedVoucher.IsActive)
                    throw new InvalidOperationException("Voucher is no longer active.");

                var now = DateTime.UtcNow;
                if (now < lockedVoucher.ValidFrom)
                    throw new InvalidOperationException("Voucher is not active yet.");

                if (now > lockedVoucher.ValidUntil)
                    throw new InvalidOperationException("Voucher has expired.");

                if (lockedVoucher.MaxUses.HasValue && lockedVoucher.UsedCount >= lockedVoucher.MaxUses.Value)
                    throw new InvalidOperationException("Voucher usage limit has been reached.");
            }

            await _bookingRepository.AddBookingAsync(booking, cancellationToken);
            await _bookingRepository.MarkHoldsAsConfirmedAsync(myHolds, booking.BookingID, cancellationToken);

            if (bookingVoucher is not null)
                await _bookingRepository.IncrementVoucherUsageAsync(bookingVoucher.VoucherID, cancellationToken);

            if (productQuantities.Count > 0)
                await _bookingRepository.DeductProductStockAsync(productQuantities, cancellationToken);

            return true;
        }, cancellationToken);

        var savedBooking = await _bookingRepository.GetBookingByIdAsync(booking.BookingID, cancellationToken);

        return (true, null, savedBooking, null);
    }

    public async Task<Booking?> GetBookingByIdAsync(
        int bookingId,
        CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.GetBookingByIdAsync(bookingId, cancellationToken);
    }

    public async Task<List<Booking>> GetMyBookingsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _bookingRepository.GetBookingsByUserAsync(userId, cancellationToken);
    }

    private static string GenerateBookingCode()
    {
        return $"BK{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
    }

    private static SeatValidationErrors? GetSeatValidationErrors(
        IReadOnlyCollection<int> requestedSeatIds,
        IReadOnlyCollection<Seat> seats,
        int showtimeRoomId)
    {
        var foundSeatIds = seats.Select(seat => seat.SeatID).ToHashSet();
        var notFoundSeatIds = requestedSeatIds.Where(id => !foundSeatIds.Contains(id)).Order().ToList();
        var wrongRoomSeatIds = seats.Where(seat => seat.RoomID != showtimeRoomId)
            .Select(seat => seat.SeatID).Order().ToList();
        var inactiveSeatIds = seats.Where(seat => seat.RoomID == showtimeRoomId && seat.Status != "active")
            .Select(seat => seat.SeatID).Order().ToList();

        return notFoundSeatIds.Count == 0 && wrongRoomSeatIds.Count == 0 && inactiveSeatIds.Count == 0
            ? null
            : new SeatValidationErrors(notFoundSeatIds, wrongRoomSeatIds, inactiveSeatIds);
    }
}
