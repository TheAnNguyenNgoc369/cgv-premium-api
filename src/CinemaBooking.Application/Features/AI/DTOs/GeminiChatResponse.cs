using System.Text.Json.Serialization;

namespace CinemaBooking.Application.Features.AI.DTOs;

public sealed class GeminiChatResponse
{
    [JsonPropertyName("reply")]
    public string Reply { get; set; } = string.Empty;

    [JsonPropertyName("followUpQuestions")]
    public List<string> FollowUpQuestions { get; set; } = [];
}
