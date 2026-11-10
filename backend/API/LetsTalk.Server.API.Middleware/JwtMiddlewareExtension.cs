using Microsoft.AspNetCore.Builder;

namespace LetsTalk.Server.API.Middleware;

public static class JwtMiddlewareExtension
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder app)
    {
        // The origin check needs the credential source that JwtMiddleware sets, so it always runs right after it
        return app
            .UseMiddleware<JwtMiddleware>()
            .UseMiddleware<OriginCheckMiddleware>();
    }
}