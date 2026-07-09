using System.ComponentModel.DataAnnotations;
using CinemaBooking.Shared.Time;

namespace CinemaBooking.API.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class VietnamDateTimeOffsetAttribute : ValidationAttribute
{
    public const string ValidationMessage =
        "StartTime must use ISO 8601 Vietnam time with the +07:00 offset";

    public VietnamDateTimeOffsetAttribute()
        : base(ValidationMessage)
    {
    }

    public override bool IsValid(object? value) =>
        value is DateTimeOffset dateTimeOffset
        && dateTimeOffset.Offset == VietnamTime.UtcOffset;
}
