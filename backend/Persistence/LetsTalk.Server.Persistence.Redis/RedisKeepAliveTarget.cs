using LetsTalk.Server.KeepAlive;
using StackExchange.Redis;
using System.Globalization;

namespace LetsTalk.Server.Persistence.Redis
{
    // The free Redis Cloud plan deletes a database that receives no commands for 14 days. Its support
    // articles recommend a scheduled SET with an expiry, so the signal is a SET followed by a GET of one
    // dedicated key. A bare PING is not documented as activity. The key expires on its own and no
    // application key is touched.
    public class RedisKeepAliveTarget(RedisConnection connection, TimeProvider timeProvider) : IKeepAliveTarget
    {
        internal const string KeepAliveKey = "keepalive:letstalk";

        private static readonly TimeSpan KeyLifetime = TimeSpan.FromHours(1);

        private readonly RedisConnection _connection = connection;
        private readonly TimeProvider _timeProvider = timeProvider;

        public string Name => "Redis";

        public async Task KeepAliveAsync(CancellationToken cancellationToken)
        {
            var database = _connection.Connection.GetDatabase();
            var key = new RedisKey(KeepAliveKey);
            var value = new RedisValue(_timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture));

            // The Redis client has no cancellation support, so the wait is abandoned when the token fires
            await database.StringSetAsync(key, value, KeyLifetime).WaitAsync(cancellationToken);
            var stored = await database.StringGetAsync(key).WaitAsync(cancellationToken);

            if (stored != value)
            {
                throw new InvalidOperationException("Redis did not return the keep-alive value that was just written.");
            }
        }
    }
}
