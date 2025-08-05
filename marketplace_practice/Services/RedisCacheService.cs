using StackExchange.Redis;
using System.Text.Json;

namespace marketplace_practice.Services
{
    public class RedisCacheService
    {
        private readonly IDatabase _database;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<T> Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var value = await _database.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return default(T);

            return JsonSerializer.Deserialize<T>(value!);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiry);
        }

        public async Task<bool> Delete(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            return await _database.KeyDeleteAsync(key);
        }

        public async Task<bool> CheckIsExists(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            return await _database.KeyExistsAsync(key);
        }

        public async Task GetExpire(string key, TimeSpan expiry)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            await _database.KeyExpireAsync(key, expiry);
        }
    }
}
