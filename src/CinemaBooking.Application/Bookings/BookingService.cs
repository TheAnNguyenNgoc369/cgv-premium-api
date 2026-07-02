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

        var pricingResult = await CalculatePricingAsync(
            customerId, showtimeId, seatIds, fnbItems, voucherCode, cancellationToken);

        if (!pricingResult.Succeeded)
            return (false, pricingResult.ErrorMessage, null, null);

        if (!isStaff && customerId != actorUserId)
            return (false, "Customers can only create bookings for their own account.", null, null);

        var bookingSeats = pricingResult.Result!.SeatDetails
            .Select(s => new BookingSeat
            {
                SeatID = s.SeatId,
                TicketPrice = s.Price
            }).ToList();

        var bookingFnBs = pricingResult.Result.FnBDetails
            .Select(f => new BookingFnB
            {
                ItemID = f.ItemId,
                Quantity = f.Quantity,
                UnitPrice = f.UnitPrice,
                SubTotal = f.SubTotal
            }).ToList();

        var productQuantities = pricingResult.Result.FnBDetails
            .ToDictionary(f => f.ItemId, f => f.Quantity);

        BookingVoucher? bookingVoucher = null;
        if (pricingResult.Result.VoucherDetails is not null)
        {
            var voucher = await _bookingRepository.GetVoucherByCodeAsync(
                pricingResult.Result.VoucherDetails.VoucherCode, cancellationToken);

            if (voucher is null)
                return (false, "Voucher code does not exist.", null, null);

            bookingVoucher = new BookingVoucher
            {
                VoucherID = voucher.VoucherID,
                DiscountApplied = pricingResult.Result.VoucherDetails.DiscountApplied,
                UsedAt = DateTime.UtcNow
            };
        }

        var booking = new Booking
        {
            BookingCode = GenerateBookingCode(),
            UserID = customerId,
            CreatedByStaffID = isStaff ? actorUserId : null,
            ShowtimeID = showtimeId,
            SubTotal = pricingResult.Result!.TotalBeforeDiscount,
            DiscountAmount = pricingResult.Result.TotalDiscount,
            FinalAmount = pricingResult.Result.FinalAmount,
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

    public async Task<(bool Succeeded, string? ErrorMessage, PricingCalculationResult? Result)> CalculatePricingAsync(
        int? userId,
        int showtimeId,
        List<int> seatIds,
        List<BookingFnBItemDto> fnbItems,
        string? voucherCode,
        CancellationToken cancellationToken = default)
    {
        if (seatIds.Count == 0)
            return (false, "Please select at least one seat.", null);

        var showtime = await _bookingRepository.GetShowtimeAsync(showtimeId, cancellationToken);
        if (showtime is null)
            return (false, "Showtime not found.", null);

        if (showtime.Status != "scheduled")
            return (false, "Cannot calculate pricing for showtimes that are not scheduled.", null);

        if (showtime.StartTime <= DateTime.UtcNow)
            return (false, "Cannot calculate pricing for showtimes that have already started.", null);

        if (showtime.Room.Cinema.Status != "active")
            return (false, "Cinema is not active.", null);

        seatIds = seatIds.Distinct().ToList();

        var seats = await _bookingRepository.GetSeatsByIdsAsync(seatIds, cancellationToken);

        if (seats.Count != seatIds.Count || seats.Any(seat =>
                seat.RoomID != showtime.RoomID || seat.Status != "active"))
            return (false, "One or more seats do not belong to the showtime room or are inactive.", null);

        var seatDetails = seats.Select(seat => new SeatPricingDetail
        {
            SeatId = seat.SeatID,
            SeatRow = seat.SeatRow,
            SeatCol = seat.SeatCol,
            SeatTypeName = seat.SeatType.TypeName,
            Price = showtime.BasePrice + seat.SeatType.ExtraPrice
        }).ToList();

        var seatsSubTotal = seatDetails.Sum(s => s.Price);

        var fnbDetails = new List<FnBPricingDetail>();
        var fnbSubTotal = 0m;

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
                return (false, $"Products with IDs {string.Join(", ", missingIds)} were not found.", null);
            }

            foreach (var fnbItem in normalizedFnbItems)
            {
                var product = products.First(p => p.ItemID == fnbItem.ItemId);

                if (product.CinemaID != showtime.Room.CinemaID)
                    return (false, $"Product '{product.ItemName}' is not available at this cinema.", null);

                if (!product.IsOnMenu)
                    return (false, $"Product '{product.ItemName}' is no longer available.", null);

                if (product.Status != "in_stock" && product.Status != "low_stock")
                    return (false, $"Product '{product.ItemName}' is out of stock.", null);

                if (product.StockQuantity < fnbItem.Quantity)
                    return (false, $"Only {product.StockQuantity} units of '{product.ItemName}' are available.", null);

                var itemSubTotal = product.Price * fnbItem.Quantity;
                fnbSubTotal += itemSubTotal;

                fnbDetails.Add(new FnBPricingDetail
                {
                    ItemId = product.ItemID,
                    ItemName = product.ItemName,
                    Quantity = fnbItem.Quantity,
                    UnitPrice = product.Price,
                    SubTotal = itemSubTotal
                });
            }
        }

        var totalBeforeDiscount = seatsSubTotal + fnbSubTotal;

        User? user = null;
        if (userId.HasValue)
        {
            user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
            if (user is null || user.Role != "customer")
                return (false, "Customer account was not found.", null);
        }

        var membershipDiscount = 0m;
        if (user?.LoyaltyTier is not null)
        {
            membershipDiscount = Math.Round(totalBeforeDiscount * user.LoyaltyTier.DiscountRate, 0);
        }

        var voucherDiscount = 0m;
        VoucherPricingDetail? voucherDetails = null;

        if (!string.IsNullOrWhiteSpace(voucherCode))
        {
            if (!userId.HasValue)
                return (false, "Guest bookings cannot use vouchers.", null);

            var voucher = await _bookingRepository.GetVoucherByCodeAsync(voucherCode.Trim(), cancellationToken);

            if (voucher is null)
                return (false, "Voucher code does not exist.", null);

            if (!voucher.IsActive)
                return (false, "Voucher is not available.", null);

            var now = DateTime.UtcNow;
            if (now < voucher.ValidFrom)
                return (false, "Voucher is not active yet.", null);

            if (now > voucher.ValidUntil)
                return (false, "Voucher has expired.", null);

            if (voucher.MaxUses.HasValue && voucher.UsedCount >= voucher.MaxUses.Value)
                return (false, "Voucher usage limit has been reached.", null);

            if (voucher.MinOrderValue.HasValue && totalBeforeDiscount < voucher.MinOrderValue.Value)
                return (false, $"A minimum order value of {voucher.MinOrderValue.Value:N0} VND is required to use this voucher.", null);

            voucherDiscount = voucher.DiscountType == "percent"
                ? Math.Round(totalBeforeDiscount * voucher.DiscountValue / 100, 0)
                : voucher.DiscountValue;

            voucherDetails = new VoucherPricingDetail
            {
                VoucherCode = voucher.VoucherCode,
                DiscountType = voucher.DiscountType,
                DiscountApplied = voucherDiscount
            };
        }

        var totalDiscount = membershipDiscount + voucherDiscount;
        if (totalDiscount > totalBeforeDiscount)
            totalDiscount = totalBeforeDiscount;

        var finalAmount = totalBeforeDiscount - totalDiscount;

        var result = new PricingCalculationResult
        {
            SeatsSubTotal = seatsSubTotal,
            FnBSubTotal = fnbSubTotal,
            TotalBeforeDiscount = totalBeforeDiscount,
            MembershipDiscount = membershipDiscount,
            VoucherDiscount = voucherDiscount,
            TotalDiscount = totalDiscount,
            FinalAmount = finalAmount,
            SeatDetails = seatDetails,
            FnBDetails = fnbDetails,
            VoucherDetails = voucherDetails
        };

        return (true, null, result);
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
