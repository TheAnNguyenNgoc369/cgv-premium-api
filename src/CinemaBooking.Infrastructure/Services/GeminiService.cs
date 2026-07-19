using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CinemaBooking.Application.Common.Interfaces;
using CinemaBooking.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CinemaBooking.Infrastructure.Services;

public sealed class GeminiService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    private const int MaxRetryAttempts = 3;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiService(
        IHttpClientFactory httpClientFactory,
        IOptions<GeminiSettings> settings,
        ILogger<GeminiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var providers = _settings.Providers;

        if (providers is { Count: > 0 })
        {
            return await CallWithProvidersAsync(prompt, providers, cancellationToken);
        }

        return await CallSingleAsync(prompt, _settings.ApiKey, _settings.Model, cancellationToken);
    }

    private async Task<string> CallWithProvidersAsync(
        string prompt,
        List<GeminiProviderSettings> providers,
        CancellationToken ct)
    {
        var lastException = (Exception?)null;

        foreach (var provider in providers)
        {
            foreach (var model in provider.Models)
            {
                for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Gemini request: provider={Provider}, model={Model}, attempt={Attempt}/{MaxAttempts}",
                            provider.Name, model, attempt, MaxRetryAttempts);

                        var result = await CallAsync(prompt, provider.ApiKey, model, ct);

                        _logger.LogInformation(
                            "Gemini success: provider={Provider}, model={Model}, attempt={Attempt}",
                            provider.Name, model, attempt);

                        return result;
                    }
                    catch (HttpRequestException ex) when (IsTransient(ex) && attempt < MaxRetryAttempts)
                    {
                        lastException = ex;
                        var delay = CalculateRetryDelay(ex, attempt);
                        _logger.LogWarning(
                            "Transient error on attempt {Attempt}: {StatusCode}. Retrying in {Delay}ms",
                            attempt, ex.StatusCode, delay.TotalMilliseconds);
                        await Task.Delay(delay, ct);
                    }
                    catch (HttpRequestException ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(
                            "Non-transient or final attempt error: provider={Provider}, model={Model}, attempt={Attempt}, status={StatusCode}",
                            provider.Name, model, attempt, ex.StatusCode);
                        break;
                    }
                }
            }

            _logger.LogWarning("All models exhausted for provider={Provider}. Moving to next provider.", provider.Name);
        }

        throw new GeminiServiceException(
            "all_providers_failed",
            $"All {providers.Count} provider(s) exhausted. Last error: {lastException?.Message}",
            lastException!);
    }

    private async Task<string> CallSingleAsync(
        string prompt,
        string apiKey,
        string model,
        CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxRetryAttempts; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "Gemini request: model={Model}, attempt={Attempt}/{MaxAttempts}",
                    model, attempt, MaxRetryAttempts);

                var result = await CallAsync(prompt, apiKey, model, ct);

                _logger.LogInformation("Gemini success: model={Model}, attempt={Attempt}", model, attempt);
                return result;
            }
            catch (HttpRequestException ex) when (IsTransient(ex) && attempt < MaxRetryAttempts)
            {
                var delay = CalculateRetryDelay(ex, attempt);
                _logger.LogWarning(
                    "Transient error on attempt {Attempt}: {StatusCode}. Retrying in {Delay}ms",
                    attempt, ex.StatusCode, delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError("Gemini error: model={Model}, attempt={Attempt}, status={StatusCode}",
                    model, attempt, ex.StatusCode);
                throw new GeminiServiceException(
                    "gemini_api_error",
                    $"Gemini API error ({ex.StatusCode}): {ex.Message}", ex);
            }
        }

        throw new GeminiServiceException(
            "max_retries_exceeded",
            $"Failed after {MaxRetryAttempts} attempts for model={model}");
    }

    private async Task<string> CallAsync(string prompt, string apiKey, string model, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = JsonContent.Create(requestBody, options: JsonOptions);

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);

            int? retryAfterSeconds = null;
            if (response.Headers.RetryAfter?.Delta.HasValue == true)
            {
                retryAfterSeconds = (int)response.Headers.RetryAfter.Delta.Value.TotalSeconds;
            }

            throw new HttpRequestException(
                $"Gemini API returned {(int)response.StatusCode} {response.StatusCode}: {errorBody}. Retry-After={retryAfterSeconds}s");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(json, JsonOptions);

        var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new HttpRequestException("Gemini returned empty response (no text in candidates)");
        }

        return text;
    }

    private static bool IsTransient(HttpRequestException ex)
    {
        if (ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout or HttpStatusCode.RequestTimeout)
        {
            return true;
        }

        return ex.StatusCode is null && ex.InnerException is System.Net.Sockets.SocketException;
    }

    private static TimeSpan CalculateRetryDelay(HttpRequestException ex, int attempt)
    {
        var baseDelay = InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.NextDouble() * baseDelay * 0.5;
        return TimeSpan.FromMilliseconds(baseDelay + jitter);
    }

    private sealed class GeminiApiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
