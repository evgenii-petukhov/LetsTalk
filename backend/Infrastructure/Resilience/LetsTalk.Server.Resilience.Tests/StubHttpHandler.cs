using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace LetsTalk.Server.Resilience.Tests;

/// <summary>
/// Answers from a queue (the last entry repeats), records every request it receives, and lets a test move a
/// <see cref="FakeTimeProvider"/> forward until a call has finished.
/// </summary>
public sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>> _answers = new();
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _last;

    public int Calls { get; private set; }

    public List<string> Bodies { get; } = [];

    public List<string?> Authorizations { get; } = [];

    public StubHttpHandler Then(HttpStatusCode status, Action<HttpResponseMessage>? configure = null) =>
        Then((_, _) =>
        {
            var response = new HttpResponseMessage(status);
            configure?.Invoke(response);
            return Task.FromResult(response);
        });

    public StubHttpHandler ThenThrow(Exception exception) => Then((_, _) => throw exception);

    public StubHttpHandler ThenHang() => Then(async (_, token) =>
    {
        await Task.Delay(Timeout.Infinite, token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    });

    public StubHttpHandler Then(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> answer)
    {
        _answers.Enqueue(answer);
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        Authorizations.Add(request.Headers.Authorization?.ToString());

        if (_answers.Count > 0)
        {
            _last = _answers.Dequeue();
        }

        return await _last!(request, cancellationToken);
    }

    /// <summary>Moves the fake clock in small steps until <paramref name="task"/> ends, so no test really waits.</summary>
    public static async Task<T> RunAsync<T>(Task<T> task, FakeTimeProvider time)
    {
        for (var i = 0; i < 20000 && !task.IsCompleted; i++)
        {
            time.Advance(TimeSpan.FromSeconds(1));
            await Task.Delay(1);
        }

        return await task;
    }
}
