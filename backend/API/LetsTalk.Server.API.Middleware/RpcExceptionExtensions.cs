using Grpc.Core;
using LetsTalk.Server.API.Middleware.Exceptions;

namespace LetsTalk.Server.API.Middleware;

public static class RpcExceptionExtensions
{
    // A service that cannot answer is different from one that rejects the credential: callers must not sign the user out
    public static bool IsUnavailable(this RpcException exception) =>
        exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

    public static ServiceUnavailableException ToServiceUnavailableException(this RpcException exception) =>
        new("The Authentication service is not available", exception);
}
