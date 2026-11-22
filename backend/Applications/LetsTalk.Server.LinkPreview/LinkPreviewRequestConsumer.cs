using LetsTalk.Server.Configuration.Models;
using LetsTalk.Server.Models.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LetsTalk.Server.LinkPreview.Utility.Abstractions;
using LetsTalk.Server.API.Client;
using MassTransit;

namespace LetsTalk.Server.LinkPreview;

public partial class LinkPreviewRequestConsumer(
    ILinkPreviewService linkPreviewService,
    ILogger<LinkPreviewRequestConsumer> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<ApplicationUrlSettings> applicationSettingsOptions,
    IOptions<LinkPreviewSettings> linkPreviewSettingsOptions) : IConsumer<LinkPreviewRequest>
{
    /// <summary>The client that reports to the API. It is not the client that downloads the page (spec 006, FR-006).</summary>
    public const string ApiClientName = "LinkPreviewApiClient";

    private readonly ILinkPreviewService _linkPreviewService = linkPreviewService;
    private readonly ILogger<LinkPreviewRequestConsumer> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ApplicationUrlSettings _applicationUrlSettings = applicationSettingsOptions.Value;
    private readonly LinkPreviewSettings _linkPreviewSettings = linkPreviewSettingsOptions.Value;

    public async Task Consume(ConsumeContext<LinkPreviewRequest> context)
    {
        if (string.IsNullOrWhiteSpace(context.Message.Url))
        {
            return;
        }

        var model = await _linkPreviewService.GenerateLinkPreviewAsync(new Utility.Abstractions.Models.LinkPreviewRequest
        {
            Url = context.Message.Url,
            SecretKey = _linkPreviewSettings.SecretKey,
            ChatId = context.Message.ChatId,
            MessageId = context.Message.MessageId,
            Token = context.Message.Token,
            ApiUrl = _applicationUrlSettings.Api
        });

        if (model == null || string.IsNullOrWhiteSpace(model.Title))
        {
            LogTitleEmpty(_logger, context.Message.Url);
            return;
        }

        var payload = new SetLinkPreviewRequest
        {
            MessageId = context.Message.MessageId,
            ChatId = context.Message.ChatId,
            Url = context.Message.Url,
            Title = model.Title,
            ImageUrl = model.ImageUrl
        };
        using var client = _httpClientFactory.CreateClient(ApiClientName);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {context.Message.Token}");
        var apiClient = new ApiClient(_applicationUrlSettings.Api, client);
        await apiClient.SetLinkPreviewAsync(payload, context.CancellationToken);
        LogSuccess(_logger, context.Message.Url);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Title is empty: {Url}")]
    private static partial void LogTitleEmpty(ILogger logger, string url);

    [LoggerMessage(Level = LogLevel.Information, Message = "New LinkPreview added: {Url}")]
    private static partial void LogSuccess(ILogger logger, string url);
}
