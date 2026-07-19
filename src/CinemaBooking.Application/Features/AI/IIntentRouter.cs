using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface IIntentRouter
{
    Task<IntentResult> ClassifyIntentAsync(string message, CancellationToken cancellationToken = default);
}
