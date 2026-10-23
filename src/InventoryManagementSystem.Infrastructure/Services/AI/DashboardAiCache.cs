using InventoryManagementSystem.Application.Common.Exceptions;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

// The provider quota is shared. Serialize cache misses and honor its cooldown,
// while allowing already-validated dashboards to be served during that cooldown.
public sealed class DashboardAiCache : IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 128 });
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _retryAt;

    public async Task<DashboardDefinitionDto> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<DashboardDefinitionDto>> generate,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out DashboardDefinitionDto? cached) && cached is not null)
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached) && cached is not null)
                return cached;

            var wait = _retryAt - DateTimeOffset.UtcNow;
            if (wait > TimeSpan.Zero)
                throw new AiRateLimitException((int)Math.Ceiling(wait.TotalSeconds));

            try
            {
                var definition = await generate(cancellationToken);
                _cache.Set(key, definition, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                    Size = 1
                });
                return definition;
            }
            catch (AiRateLimitException exception)
            {
                _retryAt = DateTimeOffset.UtcNow.AddSeconds(exception.RetryAfterSeconds);
                throw;
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _cache.Dispose();
        _gate.Dispose();
    }
}
