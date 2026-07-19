using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface ISafetyFilter
{
    SafetyCheckResult Check(string message);
}
