using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using Microsoft.Extensions.Logging;

namespace LetsTalk.Server.LinkPreview;

public partial class LinkPreviewLogger(ILogger<LinkPreviewLogger> logger) : ILinkPreviewLogger
{
    private readonly ILogger<LinkPreviewLogger> _logger = logger;

    public void LogError(Exception exception, string message)
    {
        WriteError(_logger, exception, message);
    }

    public void LogInformation(string message)
    {
        WriteInformation(_logger, message);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "{Message}")]
    private static partial void WriteError(ILogger logger, Exception exception, string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Message}")]
    private static partial void WriteInformation(ILogger logger, string message);
}
