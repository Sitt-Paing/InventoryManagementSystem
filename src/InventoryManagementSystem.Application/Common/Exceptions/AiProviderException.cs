using System.Net;

namespace InventoryManagementSystem.Application.Common.Exceptions;

public sealed class AiProviderException(HttpStatusCode statusCode, string stage, string? errorCode)
    : HttpRequestException($"Groq rejected the {stage} request with HTTP {(int)statusCode}.", null, statusCode)
{
    public string Stage { get; } = stage;
    public string? ErrorCode { get; } = errorCode;
}
