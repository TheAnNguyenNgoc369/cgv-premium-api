using CinemaBooking.API.Configuration;
using CinemaBooking.Application.Features.AI;
using CinemaBooking.Application.Features.AI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaBooking.API.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(AiRateLimitPolicyNames.Chat)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _chatService.ChatAsync(request, User, cancellationToken);
        return Ok(new
        {
            success = true,
            reply = response.Reply,
            sessionId = response.SessionId,
            followUpQuestions = response.FollowUpQuestions,
        });
    }
}
