using CinemaBooking.Application.Features.AI.DTOs;

namespace CinemaBooking.Application.Features.AI;

public interface IPromptBuilder
{
    string BuildMoviePrompt(
        IntentResult intent,
        IReadOnlyList<MovieRecommendation> recommendations,
        ConversationSession session,
        string userMessage,
        bool isAuthenticated,
        string? userProfileContext);

    string BuildFnBPrompt(
        IntentResult intent,
        IReadOnlyList<FnBRecommendation> recommendations,
        ConversationSession session,
        string userMessage,
        bool isAuthenticated,
        string? userProfileContext);

    string BuildPromotionPrompt(
        string voucherData,
        ConversationSession session,
        string userMessage,
        bool isAuthenticated,
        string? userProfileContext);

    string BuildSupportPrompt(
        string supportData,
        ConversationSession session,
        string userMessage,
        bool isAuthenticated,
        string? userProfileContext);

    string BuildGeneralPrompt(
        string generalData,
        ConversationSession session,
        string userMessage,
        bool isAuthenticated,
        string? userProfileContext);
}
