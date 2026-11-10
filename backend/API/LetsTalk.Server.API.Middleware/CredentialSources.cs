namespace LetsTalk.Server.API.Middleware;

// Where the credential of the current request came from, stored in HttpContext.Items by JwtMiddleware
public static class CredentialSources
{
    public const string ItemKey = "CredentialSource";

    // The browser sent the session cookie on its own
    public const string Cookie = "Cookie";

    // The caller put the token into the Authorization header explicitly
    public const string Header = "Header";
}
