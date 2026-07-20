using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Application.Vouchers;
using CinemaBooking.Domain.Entities;
using CinemaBooking.Shared.Constants;
using CinemaBooking.Shared.Time;

namespace CinemaBooking.API.Contracts.Vouchers;

// Shared projection for endpoints that return a customer's owned loyalty vouchers
// as one aggregated card per VoucherID (with Quantity = usable copies).
// Consumed by /api/vouchers/my-vouchers and /api/users/lookup so both endpoints
// have identical grouping, rule mapping, and batched name-lookup behavior.
public sealed class UserVoucherProjection
{
    private readonly IMovieRepository _movieRepository;
    private readonly ICinemaRepository _cinemaRepository;
    private readonly ILoyaltyRepository _loyaltyRepository;

    public UserVoucherProjection(
        IMovieRepository movieRepository,
        ICinemaRepository cinemaRepository,
        ILoyaltyRepository loyaltyRepository)
    {
        _movieRepository = movieRepository;
        _cinemaRepository = cinemaRepository;
        _loyaltyRepository = loyaltyRepository;
    }

    // Group by VoucherID, keeping only USABLE copies (Available and not past ExpiredAt).
    // Groups with zero usable copies are dropped entirely. Representative row is the
    // newest usable copy by RedeemedAt.
    public async Task<List<UserVoucherResponse>> ProjectAsync(
        IEnumerable<UserVoucher> userVouchers,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var grouped = userVouchers
            .Where(uv => uv.Status == UserVoucherStatus.Available && uv.ExpiredAt >= now)
            .GroupBy(uv => uv.VoucherID)
            .Select(g =>
            {
                var representative = g.OrderByDescending(uv => uv.RedeemedAt).First();
                return (representative, quantity: g.Count());
            })
            .OrderByDescending(x => x.representative.RedeemedAt)
            .ToList();

        var (movieNames, cinemaNames, tierNames) = await BuildRuleNameLookupsAsync(
            grouped.Select(x => x.representative.Voucher), ct);

        return grouped
            .Select(x => MapUserVoucher(x.representative, x.quantity, movieNames, cinemaNames, tierNames))
            .ToList();
    }

    // Build a redeemable-voucher card list from the vouchers themselves (no ownership
    // info). Used by /api/vouchers/redeemable.
    public async Task<List<RedeemableVoucherResponse>> ProjectRedeemableAsync(
        IEnumerable<Voucher> vouchers,
        CancellationToken ct)
    {
        var (movieNames, cinemaNames, tierNames) = await BuildRuleNameLookupsAsync(vouchers, ct);
        return vouchers.Select(v => MapRedeemable(v, movieNames, cinemaNames, tierNames)).ToList();
    }

    private async Task<(Dictionary<int, string> movieNames, Dictionary<int, string> cinemaNames, Dictionary<int, string> tierNames)>
        BuildRuleNameLookupsAsync(IEnumerable<Voucher> vouchers, CancellationToken ct)
    {
        var movieIds = new HashSet<int>();
        var cinemaIds = new HashSet<int>();
        var tierIds = new HashSet<int>();

        foreach (var voucher in vouchers)
        {
            foreach (var rule in voucher.VoucherRules ?? [])
            {
                if (rule.RuleType == "Movie" && int.TryParse(rule.RuleValue, out var movieId))
                    movieIds.Add(movieId);
                else if (rule.RuleType == "Cinema" && int.TryParse(rule.RuleValue, out var cinemaId))
                    cinemaIds.Add(cinemaId);
                else if (rule.RuleType == "Membership" && int.TryParse(rule.RuleValue, out var tierId))
                    tierIds.Add(tierId);
            }
        }

        var movieNames = movieIds.Any()
            ? (await _movieRepository.GetMoviesByIdsAsync(movieIds.ToList(), ct))
                .ToDictionary(m => m.MovieID, m => m.Title)
            : new Dictionary<int, string>();

        var cinemaNames = cinemaIds.Any()
            ? (await _cinemaRepository.GetCinemasByIdsAsync(cinemaIds.ToList(), ct))
                .ToDictionary(c => c.CinemaID, c => c.CinemaName)
            : new Dictionary<int, string>();

        var tierNames = tierIds.Any()
            ? (await _loyaltyRepository.GetTiersByIdsAsync(tierIds.ToList(), ct))
                .ToDictionary(t => t.TierID, t => t.TierName)
            : new Dictionary<int, string>();

        return (movieNames, cinemaNames, tierNames);
    }

    private static List<RedeemableVoucherRuleResponse> MapVoucherRules(
        Voucher v,
        Dictionary<int, string> movieNames,
        Dictionary<int, string> cinemaNames,
        Dictionary<int, string> tierNames) =>
        (v.VoucherRules ?? [])
            .Select(r => new RedeemableVoucherRuleResponse(
                r.RuleType,
                GetOperatorForRuleType(r.RuleType),
                r.RuleValue,
                RedeemableVoucherRuleDisplayTextGenerator.GenerateDisplayText(
                    r.RuleType,
                    r.RuleValue,
                    movieNames,
                    cinemaNames,
                    null,
                    tierNames)))
            .ToList();

    private static UserVoucherResponse MapUserVoucher(
        UserVoucher uv,
        int quantity,
        Dictionary<int, string> movieNames,
        Dictionary<int, string> cinemaNames,
        Dictionary<int, string> tierNames) => new(
        // Voucher information
        uv.VoucherID,
        uv.Voucher.VoucherCode,
        uv.Voucher.DiscountType,
        uv.Voucher.DiscountValue,

        // Display information
        uv.Voucher.ImageURL,
        MapVoucherRules(uv.Voucher, movieNames, cinemaNames, tierNames),

        // Ownership information
        quantity,
        uv.Status,

        // Lifecycle
        VietnamTime.FromUtc(uv.RedeemedAt),
        VietnamTime.FromUtc(uv.ExpiredAt),
        uv.UsedAt.HasValue ? VietnamTime.FromUtc(uv.UsedAt.Value) : null);

    private static RedeemableVoucherResponse MapRedeemable(
        Voucher v,
        Dictionary<int, string> movieNames,
        Dictionary<int, string> cinemaNames,
        Dictionary<int, string> tierNames) => new(
        v.VoucherID,
        v.VoucherCode,
        v.DiscountType,
        v.DiscountValue,
        v.RequiredPoints!.Value,
        v.ExchangeLimit,
        VietnamTime.FromUtc(v.ValidFrom),
        VietnamTime.FromUtc(v.ValidUntil),
        v.ImageURL,
        v.Description,
        MapVoucherRules(v, movieNames, cinemaNames, tierNames));

    private static string GetOperatorForRuleType(string ruleType) => ruleType switch
    {
        "MinimumSpend" => ">=",
        "MaximumSpend" => "<=",
        "TicketQuantity" => ">=",
        "Movie" => "=",
        "Cinema" => "=",
        "SeatType" => "=",
        "Room" => "=",
        "Membership" => "=",
        "PaymentMethod" => "=",
        "DayOfWeek" => "=",
        "Product" => "=",
        "FoodCategory" => "=",
        "FoodAndDrink" => "=",
        "ApplyScope" => "=",
        _ => "="
    };
}
