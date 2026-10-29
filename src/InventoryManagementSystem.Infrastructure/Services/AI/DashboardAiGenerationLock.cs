using System.Diagnostics;
using InventoryManagementSystem.Application.Dashboard.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

// One renewable lease per provider/environment, shared by all API instances.
// This is coordination on one Redis service, not a consensus/failover guarantee.
public sealed class DashboardAiGenerationLock
{
    private static readonly TimeSpan LeaseLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromSeconds(10);
    private readonly IDatabase _redis;
    private readonly RedisKey _key;
    private readonly ILogger<DashboardAiGenerationLock> _logger;

    public DashboardAiGenerationLock(IConnectionMultiplexer connection,
        IHostEnvironment environment, ILogger<DashboardAiGenerationLock> logger)
    {
        _redis = connection.GetDatabase();
        _key = $"inventory:{environment.EnvironmentName}:groq:generation-lock:v1";
        _logger = logger;
    }

    public async Task<Lease?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var owner = Guid.NewGuid().ToString("N");
        try
        {
            var acquired = await _redis.StringSetAsync(_key, owner,
                    new Expiration(LeaseLifetime), ValueCondition.NotExists)
                .WaitAsync(cancellationToken);
            return acquired ? new Lease(_redis, _key, owner, _logger) : null;
        }
        catch (RedisException exception)
        {
            throw new DashboardCoordinationException("Could not acquire the shared AI generation lease.", exception);
        }
    }

    public sealed class Lease : IAsyncDisposable
    {
        private const string RenewScript = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('PEXPIRE', KEYS[1], ARGV[2])
            end
            return 0
            """;
        private const string ReleaseScript = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                return redis.call('DEL', KEYS[1])
            end
            return 0
            """;
        private const string PublishScript = """
            if redis.call('GET', KEYS[1]) == ARGV[1] then
                redis.call('SET', KEYS[2], ARGV[2], 'PX', ARGV[3])
                return 1
            end
            return 0
            """;

        private readonly IDatabase _redis;
        private readonly RedisKey _key;
        private readonly string _owner;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _stop = new();
        private readonly CancellationTokenSource _lost = new();
        private readonly Task _renewal;
        private long _lastConfirmed = Stopwatch.GetTimestamp();
        private int _disposed;

        internal Lease(IDatabase redis, RedisKey key, string owner, ILogger logger)
        {
            _redis = redis;
            _key = key;
            _owner = owner;
            _logger = logger;
            _renewal = RenewLoopAsync();
        }

        public CancellationToken LostToken => _lost.Token;

        public async Task PublishAsync(RedisKey cacheKey, string json, TimeSpan lifetime,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_lost.IsCancellationRequested || Volatile.Read(ref _disposed) != 0)
                throw new DashboardCoordinationException("The AI generation lease was lost before publication.");

            // Owner check and publication are atomic on the configured standalone Redis.
            var result = await _redis.ScriptEvaluateAsync(PublishScript,
                    [_key, cacheKey], [_owner, json, (long)lifetime.TotalMilliseconds])
                .WaitAsync(cancellationToken);
            if ((long)result != 1)
            {
                _lost.Cancel();
                throw new DashboardCoordinationException("The AI generation lease was lost before publication.");
            }
        }

        public async Task EnsureOwnershipAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_lost.IsCancellationRequested || Volatile.Read(ref _disposed) != 0
                || Stopwatch.GetElapsedTime(Interlocked.Read(ref _lastConfirmed)) >= LeaseLifetime)
            {
                _lost.Cancel();
                throw new DashboardCoordinationException("The AI generation lease was lost.");
            }

            var started = Stopwatch.GetTimestamp();
            try
            {
                var result = await _redis.ScriptEvaluateAsync(RenewScript,
                        [_key], [_owner, (long)LeaseLifetime.TotalMilliseconds])
                    .WaitAsync(cancellationToken);
                if ((long)result != 1)
                {
                    _lost.Cancel();
                    throw new DashboardCoordinationException("The AI generation lease was lost.");
                }
                Interlocked.Exchange(ref _lastConfirmed, started);
            }
            catch (RedisException exception)
            {
                _lost.Cancel();
                throw new DashboardCoordinationException("Could not renew the AI generation lease.", exception);
            }
        }

        private async Task RenewLoopAsync()
        {
            try
            {
                while (true)
                {
                    await Task.Delay(RenewalInterval, _stop.Token);
                    await EnsureOwnershipAsync(_stop.Token);
                }
            }
            catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
            catch (DashboardCoordinationException exception)
            {
                _logger.LogWarning(exception, "Shared AI generation lease lost; cancelling generation.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
            _stop.Cancel();
            await _renewal;
            try
            {
                // Cancellation of the HTTP request must not skip owner-safe cleanup.
                await _redis.ScriptEvaluateAsync(ReleaseScript, [_key], [_owner])
                    .WaitAsync(TimeSpan.FromSeconds(3));
            }
            catch (Exception exception) when (exception is RedisException or TimeoutException)
            {
                _logger.LogWarning(exception, "Could not release the AI lease; its TTL will expire.");
            }
            finally
            {
                _stop.Dispose();
                _lost.Dispose();
            }
        }
    }
}
