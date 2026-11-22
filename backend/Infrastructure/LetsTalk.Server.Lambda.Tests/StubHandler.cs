using System.Net;
using Microsoft.Extensions.Time.Testing;

namespace LetsTalk.Server.Lambda.Tests;

/// <summary>Answers with the given statuses in turn (the last one repeats) and counts the calls.</summary>
public sealed class StubHandler(params HttpStatusCode[] answers) : HttpMessageHandler
{
    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var status = answers[Math.Min(Calls, answers.Length - 1)];
        Calls++;
        return Task.FromResult(new HttpResponseMessage(status));
    }

    /// <summary>Moves the fake clock until <paramref name="task"/> ends, so no test really waits.</summary>
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
