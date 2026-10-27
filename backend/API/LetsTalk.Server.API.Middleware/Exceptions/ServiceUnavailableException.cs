namespace LetsTalk.Server.API.Middleware.Exceptions;

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException()
    {
    }

    public ServiceUnavailableException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
