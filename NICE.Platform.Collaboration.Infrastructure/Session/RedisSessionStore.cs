namespace NICE.Platform.Collaboration.Infrastructure.Session;
using NICE.Platform.Collaboration.Application.Interfaces.Services;
using StackExchange.Redis;
public class RedisSessionStore(IConnectionMultiplexer redis) : ISessionStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    public async Task SetAsync(string key, string value, TimeSpan? expiry, CancellationToken ct)
        => await _db.StringSetAsync(key, value, expiry.HasValue ? (Expiration)expiry.Value : Expiration.Default);
    public async Task<string?> GetAsync(string key, CancellationToken ct)
        => await _db.StringGetAsync(key);
    public async Task RemoveAsync(string key, CancellationToken ct)
        => await _db.KeyDeleteAsync(key);
    public async Task<bool> ExistsAsync(string key, CancellationToken ct)
        => await _db.KeyExistsAsync(key);
}
