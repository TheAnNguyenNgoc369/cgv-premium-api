using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface IConversationStore
{
    ConversationSession GetOrCreate(string? sessionId);
    void Save(ConversationSession session);
}
