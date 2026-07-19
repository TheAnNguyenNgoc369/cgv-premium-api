using System.Security.Claims;
using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface IChatService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, ClaimsPrincipal? user, CancellationToken cancellationToken = default);
}
