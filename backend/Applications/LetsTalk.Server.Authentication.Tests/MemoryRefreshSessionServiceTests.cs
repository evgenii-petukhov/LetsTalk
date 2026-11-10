using LetsTalk.Server.Authentication.Abstractions;
using LetsTalk.Server.Authentication.Services.Session;
using LetsTalk.Server.Configuration.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LetsTalk.Server.Authentication.Tests;

[TestFixture]
public class MemoryRefreshSessionServiceTests : RefreshSessionServiceTestsBase
{
    private RecordingMemoryCache _memoryCache = null!;

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    protected override IRefreshSessionService CreateService(
        IJwtCacheService jwtCacheService,
        IOptions<JwtSettings> jwtSettings,
        TimeProvider timeProvider,
        ListLogger logger)
    {
        _memoryCache = new RecordingMemoryCache();
        return new MemoryRefreshSessionService(
            jwtCacheService,
            jwtSettings,
            timeProvider,
            new TypedLogger(logger),
            _memoryCache);
    }

    protected override IEnumerable<string> GetStoredText()
    {
        return _memoryCache.Entries.SelectMany(x => new[] { x.Key.ToString()!, JsonSerializer.Serialize(x.Value) });
    }

    // Records every entry written to the cache so the test can inspect keys and values
    private sealed class RecordingMemoryCache : IMemoryCache
    {
        private readonly MemoryCache _inner = new(new MemoryCacheOptions());

        public List<ICacheEntry> Entries { get; } = [];

        public ICacheEntry CreateEntry(object key)
        {
            var entry = _inner.CreateEntry(key);
            Entries.Add(entry);
            return entry;
        }

        public void Remove(object key) => _inner.Remove(key);

        public bool TryGetValue(object key, out object? value) => _inner.TryGetValue(key, out value);

        public void Dispose() => _inner.Dispose();
    }

    private sealed class TypedLogger(ILogger inner) : ILogger<MemoryRefreshSessionService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => inner.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => inner.Log(logLevel, eventId, state, exception, formatter);
    }
}
