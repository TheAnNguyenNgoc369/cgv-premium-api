using System.Collections.Concurrent;
using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public sealed class ConversationStore : IConversationStore
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions = new();
    private readonly Timer _cleanupTimer;
    private static readonly TimeSpan SessionExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(5);

    public ConversationStore()
    {
        _cleanupTimer = new Timer(CleanupExpiredSessions, null, CleanupInterval, CleanupInterval);
    }

    public ConversationSession GetOrCreate(string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return CreateNewSession();
        }

        return _sessions.GetOrAdd(sessionId, _ => CreateNewSession(sessionId));
    }

    public void Save(ConversationSession session)
    {
        session.LastActivity = DateTime.UtcNow;
        _sessions.AddOrUpdate(session.SessionId, session, (_, _) => session);
    }

    private static ConversationSession CreateNewSession(string? sessionId = null)
    {
        return new ConversationSession
        {
            SessionId = sessionId ?? Guid.NewGuid().ToString("N"),
            LastActivity = DateTime.UtcNow,
        };
    }

    private void CleanupExpiredSessions(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _sessions
            .Where(kvp => (now - kvp.Value.LastActivity) > SessionExpiration)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _sessions.TryRemove(key, out _);
        }
    }
}
