using System.Net;
using System.Net.Http.Headers;
using Google.Protobuf;
using Grpc.Core;

namespace LetsTalk.Server.Authentication.Client.Tests;

/// <summary>
/// A fake Authentication service: the first <c>failures</c> calls answer with the given status, the later ones succeed with
/// <c>reply</c>. It records the path of every call, so a test can tell which method was repeated.
/// </summary>
public sealed class GrpcStubHandler(int failures, IMessage? reply = null, StatusCode failure = StatusCode.Unavailable) : HttpMessageHandler
{
    public int Calls { get; private set; }

    public List<string> Paths { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        Paths.Add(request.RequestUri!.AbsolutePath);

        // The gRPC client buffers the request to be able to send it again, so it has to be read to the end
        if (request.Content is not null)
        {
            await request.Content.ReadAsByteArrayAsync(cancellationToken);
        }

        return Calls <= failures ? Failed(failure) : Succeeded(reply ?? new Google.Protobuf.WellKnownTypes.Empty());
    }

    // A "trailers only" answer: the status is in the headers and there is no message
    private static HttpResponseMessage Failed(StatusCode status)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK) { Version = new Version(2, 0), Content = new ByteArrayContent([]) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        response.Headers.Add("grpc-status", ((int)status).ToString());
        return response;
    }

    private static HttpResponseMessage Succeeded(IMessage message)
    {
        var body = message.ToByteArray();
        var frame = new byte[5 + body.Length];
        frame[1] = (byte)(body.Length >> 24);
        frame[2] = (byte)(body.Length >> 16);
        frame[3] = (byte)(body.Length >> 8);
        frame[4] = (byte)body.Length;
        body.CopyTo(frame, 5);

        var response = new HttpResponseMessage(HttpStatusCode.OK) { Version = new Version(2, 0), Content = new ByteArrayContent(frame) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        response.TrailingHeaders.Add("grpc-status", "0");
        return response;
    }
}
