namespace LetsTalk.Server.Configuration.Models;

public class AuthCookieSettings
{
    public string Name { get; set; } = "auth-token";

    public string Domain { get; set; } = string.Empty;

    public bool Secure { get; set; } = true;

    public string SameSite { get; set; } = "Strict";

    public string RefreshName { get; set; } = "auth-refresh";

    public string RefreshPath { get; set; } = "/api/Authentication";
}
