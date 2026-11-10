using LetsTalk.Server.API.Core.Commands;
using LetsTalk.Server.Authentication.Abstractions;

namespace LetsTalk.Server.API.Core.Features.Authentication;

public static class AuthSessionExtensions
{
    public static SessionResult ToResult(this AuthSession session)
    {
        return new SessionResult(
            session.AccessToken,
            session.AccessExpiresAt,
            session.RefreshToken,
            session.RefreshExpiresAt);
    }
}
