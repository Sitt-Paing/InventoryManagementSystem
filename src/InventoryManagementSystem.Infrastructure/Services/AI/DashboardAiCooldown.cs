using InventoryManagementSystem.Application.Dashboard.Exceptions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace InventoryManagementSystem.Infrastructure.Services.AI;

// Provider cooldown is shared across companies/languages in this environment.
public sealed class DashboardAiCooldown
{
    private readonly IDatabase _redis;
    private readonly RedisKey _key;

    private const string ReadScript = "return redis.call('PTTL', KEYS[1])";
    private const string ExtendScript = """
        local requested = tonumber(ARGV[1])
        local current = redis.call('PTTL', KEYS[1])
        if current < requested then
            redis.call('SET', KEYS[1], '1', 'PX', requested)
        end
        return redis.call('PTTL', KEYS[1])
        """;

    public DashboardAiCooldown(IConnectionMultiplexer connection, IHostEnvironment environment)
    {
        _redis = connection.GetDatabase();
        _key = $"inventory:{environment.EnvironmentName}:groq:cooldown:v1";
    }

    public async Task<int> GetRemainingSecondsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await _redis.ScriptEvaluateAsync(ReadScript, [_key])
                .WaitAsync(cancellationToken);
            var milliseconds = (long)result;
            if (milliseconds == -1)
                throw new DashboardCoordinationException("The AI cooldown key has no expiration.");
            return ToSeconds(milliseconds);
        }
        catch (RedisException exception)
        {
            throw new DashboardCoordinationException("Could not read the shared AI cooldown.", exception);
        }
    }

    public async Task<int> ExtendAsync(int retryAfterSeconds, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(retryAfterSeconds);
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = await _redis.ScriptEvaluateAsync(ExtendScript,
                    [_key], [retryAfterSeconds * 1000L])
                .WaitAsync(cancellationToken);
            return ToSeconds((long)result);
        }
        catch (RedisException exception)
        {
            throw new DashboardCoordinationException("Could not extend the shared AI cooldown.", exception);
        }
    }

    private static int ToSeconds(long milliseconds) =>
        (int)Math.Clamp(Math.Ceiling(milliseconds / 1000d), 0, int.MaxValue);
}
