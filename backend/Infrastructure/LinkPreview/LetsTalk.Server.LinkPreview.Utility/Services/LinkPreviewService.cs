using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using LetsTalk.Server.LinkPreview.Utility.Abstractions.Models;
using System.Web;

namespace LetsTalk.Server.LinkPreview.Utility.Services;

public class LinkPreviewService(
    IDownloadService downloadService,
    IRegexService regexService,
    ILinkPreviewService fallbackLinkPreviewService,
    ILinkPreviewLogger logger) : ILinkPreviewService
{
    private readonly IDownloadService _downloadService = downloadService;
    private readonly IRegexService _regexService = regexService;
    private readonly ILinkPreviewService _fallbackLinkPreviewService = fallbackLinkPreviewService;
    private readonly ILinkPreviewLogger _logger = logger;

    public async Task<OpenGraphModel> GenerateLinkPreviewAsync(
        LinkPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Generating link preview for URL: {request.Url}");
        try
        {
            _logger.LogInformation("Downloading page content...");
            var pageString = await _downloadService.DownloadAsStringAsync(request.Url!, cancellationToken);

            _logger.LogInformation("Parsing Open Graph data...");
            var model = _regexService.GetOpenGraphModel(pageString);

            if (model == null)
            {
                return null!;
            }

            return new OpenGraphModel
            {
                Title = HttpUtility.HtmlDecode(model.Title),
                ImageUrl = model.ImageUrl
            };
        }
        catch (HttpRequestException e)
        when (e.StatusCode == System.Net.HttpStatusCode.Forbidden && !string.IsNullOrWhiteSpace(request.SecretKey))
        {
            _logger.LogError(e, "Forbidden error occurred. Trying fallback service.");
            return await _fallbackLinkPreviewService.GenerateLinkPreviewAsync(request, cancellationToken);
        }
    }
}
