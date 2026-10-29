using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryManagementSystem.Application.Common.Exceptions;
using InventoryManagementSystem.Application.Dashboard.DTOs;
using InventoryManagementSystem.Application.Dashboard.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

public sealed class DashboardAiCache : IDisposable
{
    private static readonly TimeSpan CacheLifetime =
        TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockWaitLimit = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan GenerationLimit = TimeSpan.FromMinutes(4);

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling =
                JsonUnmappedMemberHandling.Disallow
        };

    private readonly MemoryCache _cache =
        new(new MemoryCacheOptions { SizeLimit = 128 });

    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly IDatabase _redis;
    private readonly ILogger<DashboardAiCache> _logger;
    private readonly string _keyPrefix;
    private readonly DashboardAiCooldown _cooldown;
    private readonly DashboardAiGenerationLock _generationLock;

    private DateTimeOffset _retryAt;

    public DashboardAiCache(
        IConnectionMultiplexer connection,
        IHostEnvironment environment,
        ILogger<DashboardAiCache> logger,
        DashboardAiCooldown cooldown,
        DashboardAiGenerationLock generationLock)
    {
        _redis = connection.GetDatabase();
        _logger = logger;
        _cooldown = cooldown;
        _generationLock = generationLock;

        _keyPrefix =
            $"inventory:{environment.EnvironmentName}" +
            ":dashboard:definition:v1:";
    }

    public async Task<DashboardDefinitionDto> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<DashboardDefinitionDto>> generate,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var cached = await ReadAsync(key, cancellationToken);

        if (cached is not null)
            return cached;

        if (!await _gate.WaitAsync(LockWaitLimit, cancellationToken))
            throw new DashboardCoordinationException("Timed out waiting for local AI generation.");

        try
        {
            var waiting = Stopwatch.StartNew();
            while (true)
            {
                // Another instance may finish this definition while we wait.
                cached = await ReadAsync(key, cancellationToken);
                if (cached is not null) return cached;

                await CheckCooldownAsync(cancellationToken);
                if (waiting.Elapsed >= LockWaitLimit)
                    throw new DashboardCoordinationException("Timed out waiting for shared AI generation.");

                await using var lease = await _generationLock.TryAcquireAsync(cancellationToken);
                if (lease is null)
                {
                    await Task.Delay(250, cancellationToken);
                    continue;
                }

                cached = await ReadAsync(key, cancellationToken);
                if (cached is not null) return cached;
                await CheckCooldownAsync(cancellationToken);
                await lease.EnsureOwnershipAsync(cancellationToken);

                using var generation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, lease.LostToken);
                generation.CancelAfter(GenerationLimit);
                try
                {
                    var definition = await generate(generation.Token);
                    await lease.EnsureOwnershipAsync(generation.Token);
                    await WriteAsync(key, definition, lease, generation.Token);
                    return definition;
                }
                catch (AiRateLimitException exception)
                {
                    var retryAt = DateTimeOffset.UtcNow.AddSeconds(exception.RetryAfterSeconds);
                    if (retryAt > _retryAt) _retryAt = retryAt;
                    // Publish a provider limit even if the browser disconnected.
                    using var publish = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    try
                    {
                        await _cooldown.ExtendAsync(exception.RetryAfterSeconds, publish.Token);
                    }
                    catch (Exception failure) when (failure is DashboardCoordinationException or OperationCanceledException)
                    {
                        _logger.LogWarning(failure, "Could not publish the provider cooldown; local cooldown remains active.");
                    }
                    throw;
                }
                catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new DashboardCoordinationException(
                        "AI generation lost its lease or exceeded its time budget.", exception);
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task CheckCooldownAsync(CancellationToken cancellationToken)
    {
        var localWait = _retryAt - DateTimeOffset.UtcNow;
        if (localWait > TimeSpan.Zero)
            throw new AiRateLimitException((int)Math.Ceiling(localWait.TotalSeconds));

        var sharedWait = await _cooldown.GetRemainingSecondsAsync(cancellationToken);
        if (sharedWait > 0) throw new AiRateLimitException(sharedWait);
    }

    private async Task<DashboardDefinitionDto?> ReadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(
                key,
                out DashboardDefinitionDto? local)
            && local is not null)
        {
            return local;
        }

        try
        {
            var payload = await _redis
                .StringGetAsync(_keyPrefix + key)
                .WaitAsync(cancellationToken);

            if (payload.IsNullOrEmpty)
                return null;

            var definition =
                JsonSerializer.Deserialize<DashboardDefinitionDto>(
                    payload.ToString(),
                    JsonOptions);

            if (definition is not null)
            {
                _logger.LogInformation(
                    "Dashboard definition cache hit in Redis.");
            }

            return definition;
        }
        catch (Exception exception)
            when (exception is RedisException or JsonException)
        {
            _logger.LogWarning(
                exception,
                "Redis dashboard cache read failed; treating it as a cache miss.");

            return null;
        }
    }

    private async Task WriteAsync(
        string key,
        DashboardDefinitionDto definition,
        DashboardAiGenerationLock.Lease lease,
        CancellationToken cancellationToken)
    {
        try
        {
            await lease.PublishAsync(_keyPrefix + key,
                JsonSerializer.Serialize(definition, JsonOptions), CacheLifetime, cancellationToken);
            _logger.LogInformation("Dashboard definition stored in Redis for five minutes.");
        }
        catch (RedisException exception)
        {
            // A failed cache write may fall back locally only while ownership can
            // still be confirmed. Disconnected cold misses never bypass coordination.
            await lease.EnsureOwnershipAsync(cancellationToken);
            _logger.LogWarning(exception, "Redis dashboard cache write failed; keeping a local memory copy.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        _cache.Set(key, definition, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheLifetime,
            Size = 1
        });
    }

    public void Dispose()
    {
        _cache.Dispose();
        _gate.Dispose();
    }
}
