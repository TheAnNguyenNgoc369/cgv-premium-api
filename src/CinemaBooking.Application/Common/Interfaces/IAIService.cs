namespace CinemaBooking.Application.Common.Interfaces;

public interface IAIService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
}
