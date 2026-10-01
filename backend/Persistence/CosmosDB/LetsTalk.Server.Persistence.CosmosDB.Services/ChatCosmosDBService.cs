using LetsTalk.Server.Persistence.AgnosticServices.Abstractions;
using LetsTalk.Server.Persistence.AgnosticServices.Models;
using LetsTalk.Server.Persistence.CosmosDB.Repository;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions;
using LetsTalk.Server.Persistence.CosmosDB.Repository.Abstractions.Models;

namespace LetsTalk.Server.Persistence.CosmosDB.Services;

public class ChatCosmosDBService(
    IChatRepository chatRepository,
    IAccountRepository accountRepository,
    IMessageRepository messageRepository,
    ChatMessageStatusRepository chatMessageStatusRepository) : IChatAgnosticService
{
    private readonly IChatRepository _chatRepository = chatRepository;
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IMessageRepository _messageRepository = messageRepository;
    private readonly ChatMessageStatusRepository _chatMessageStatusRepository = chatMessageStatusRepository;

    public async Task<string> CreateIndividualChatAsync(IEnumerable<string> accountIds, CancellationToken cancellationToken = default)
    {
        var chat = await _chatRepository.GetIndividualChatByAccountIdsAsync(accountIds, cancellationToken);

        chat ??= await _chatRepository.CreateIndividualChatAsync(accountIds, cancellationToken);

        return chat.Id!;
    }

    public Task<List<string>> GetAccountIdsInIndividualChatsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        return _chatRepository.GetAccountIdsInIndividualChatsAsync(accountId, cancellationToken);
    }

    public Task<List<string>> GetChatMemberAccountIdsAsync(string chatId, CancellationToken cancellationToken = default)
    {
        return _chatRepository.GetChatMemberAccountIdsAsync(chatId, cancellationToken);
    }

    public async Task<List<ChatServiceModel>> GetChatsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var chats = await _chatRepository.GetChatsByAccountIdAsync(accountId, cancellationToken);

        var accountsTask = _accountRepository.GetAccountsByChatsAsync(chats, accountId, cancellationToken);

        var metricsTask = GetChatMetricsInternal(accountId, cancellationToken);

        await Task.WhenAll(accountsTask, metricsTask);

        return [.. chats.Select(chat =>
        {
            metricsTask.Result.TryGetValue(chat.Id!, out var metrics);
            var otherAccount = accountsTask.Result.FirstOrDefault(a => chat.AccountIds!.Contains(a.Id!));

            return new ChatServiceModel
            {
                Id = chat.Id,
                ChatName = chat.IsIndividual ? $"{otherAccount?.FirstName} {otherAccount?.LastName}" : chat.Name,
                PhotoUrl = chat.IsIndividual ? otherAccount?.PhotoUrl : null,
                AccountTypeId = chat.IsIndividual ? otherAccount?.AccountTypeId : null,
                Image = chat.IsIndividual && otherAccount?.Image != null ? new ImageServiceModel
                {
                    Id = otherAccount.Image.Id,
                    FileStorageTypeId = otherAccount.Image.FileStorageTypeId
                } : null,
                LastMessageDate = metrics?.LastMessageDate,
                LastMessageId = metrics?.LastMessageId,
                UnreadCount = metrics?.UnreadCount ?? 0,
                IsIndividual = chat.IsIndividual,
                AccountIds = [.. chat.AccountIds!.Where(x => x != accountId)]
            };
        })];
    }

    public Task<bool> IsAccountChatMemberAsync(string chatId, string accountId, CancellationToken cancellationToken = default)
    {
        return _chatRepository.IsAccountChatMemberAsync(chatId, accountId, cancellationToken);
    }

    public Task<bool> IsChatIdValidAsync(string id, CancellationToken cancellationToken = default)
    {
        return _chatRepository.IsChatIdValidAsync(id, cancellationToken);
    }

    private async Task<Dictionary<string, ChatMetric>> GetChatMetricsInternal(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var chats = await _chatRepository.GetChatsByAccountIdAsync(accountId, cancellationToken);

        var chatIds = chats
            .Select(x => x.Id!)
            .ToList();

        var messagesTask = _messageRepository.GetMessagesByChatIdsAsync(chatIds, cancellationToken);
        var statusesTask = _chatMessageStatusRepository.GetStatusesByAccountIdAndChatIdsAsync(accountId, chatIds, cancellationToken);

        await Task.WhenAll(messagesTask, statusesTask);

        var messagesByChatId = messagesTask.Result
            .GroupBy(m => m.ChatId!)
            .ToDictionary(g => g.Key, g => g.ToList());

        var statusesByMessageId = statusesTask.Result
            .ToDictionary(s => s.MessageId!, StringComparer.Ordinal);

        return chats.ToDictionary(x => x.Id!, x =>
        {
            if (!messagesByChatId.TryGetValue(x.Id!, out var msgs) || msgs.Count == 0)
                return new ChatMetric();

            var lastMessageDate = msgs.Max(m => m.DateCreatedUnix);
            var lastMessageId = msgs.First(m => m.DateCreatedUnix == lastMessageDate).Id;
            var lastReadDate = msgs
                .Where(m => statusesByMessageId.TryGetValue(m.Id!, out _))
                .Select(m => statusesByMessageId[m.Id!].DateReadUnix)
                .DefaultIfEmpty(null)
                .Max();
            var unreadCount = msgs.Count(m => m.DateCreatedUnix > lastReadDate && m.SenderId != accountId);

            return new ChatMetric
            {
                ChatId = x.Id,
                LastMessageDate = lastMessageDate,
                LastMessageId = lastMessageId,
                UnreadCount = unreadCount
            };
        });
    }
}
