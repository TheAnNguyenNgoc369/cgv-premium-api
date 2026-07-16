namespace CinemaBooking.Application.Vouchers.RuleEngine.Metadata;

/// <summary>
/// Frontend-facing description of a single voucher rule type. The rule engine
/// itself keeps working from <see cref="Domain.Entities.VoucherRule"/>; this
/// record only tells the admin UI how to render the rule's editor.
/// </summary>
/// <param name="RuleType">Rule identifier (matches a <c>VoucherRuleTypes</c> constant).</param>
/// <param name="DisplayName">Human-readable label for the admin UI.</param>
/// <param name="InputType">One of <see cref="VoucherRuleInputTypes"/>.</param>
/// <param name="DataSource">
///   Absolute API path the UI can query to populate a dropdown, or <c>null</c>
///   when <see cref="Options"/> supplies the choices instead.
/// </param>
/// <param name="Options">
///   Static list of allowed values, or <c>null</c> when the UI should fetch
///   choices from <see cref="DataSource"/> or accept free-form input.
/// </param>
public sealed record VoucherRuleTypeMetadata(
    string RuleType,
    string DisplayName,
    string InputType,
    string? DataSource = null,
    IReadOnlyList<string>? Options = null);
