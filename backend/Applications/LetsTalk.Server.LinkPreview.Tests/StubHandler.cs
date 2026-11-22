using System.Net;

namespace LetsTalk.Server.LinkPreview.Tests;

/// <summary>Answers with the given statuses in turn (the last one repeats) and counts the calls.</summary>
public sealed class StubHandler(params HttpStatusCode[] answers) : HttpMessageHandler
{
    public int Calls { get; private set; }

    public List<string> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var status = answers[Math.Min(Calls, answers.Length - 1)];
        Calls++;
        Requests.Add($"{request.Method} {request.RequestUri}");
        return Task.FromResult(new HttpResponseMessage(status));
    }
}
