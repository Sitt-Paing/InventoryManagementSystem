using System.Net;

namespace InventoryManagementSystem.Application.Common.Exceptions;

public sealed class AiRateLimitException(int retryAfterSeconds)
    : HttpRequestException("AI provider usage limit reached.", null, HttpStatusCode.TooManyRequests)
{
    public int RetryAfterSeconds { get; } = Math.Max(1, retryAfterSeconds);
}
