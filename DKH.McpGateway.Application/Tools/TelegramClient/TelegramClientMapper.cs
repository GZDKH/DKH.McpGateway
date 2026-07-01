using DKH.TelegramClientService.Contracts.TelegramClient.Api.ChatMonitoring.V1;
using DKH.TelegramClientService.Contracts.TelegramClient.Api.MessageCollection.V1;
using DKH.TelegramClientService.Contracts.TelegramClient.Models.ChatMessage.V1;
using DKH.TelegramClientService.Contracts.TelegramClient.Models.MediaAttachment.V1;
using DKH.TelegramClientService.Contracts.TelegramClient.Models.MonitoredChat.V1;
using DKH.TelegramClientService.Contracts.TelegramClient.Models.Session.V1;
using Google.Protobuf.WellKnownTypes;

namespace DKH.McpGateway.Application.Tools.TelegramClient;

internal static class TelegramClientMapper
{
    internal static object MapMessage(ChatMessageModel message) => new
    {
        messageId = message.MessageId?.Value,
        monitoredChatId = message.MonitoredChatId?.Value,
        telegramMessageId = message.TelegramMessageId,
        senderName = NullIfEmpty(message.SenderName),
        messageType = message.MessageType,
        textContent = NullIfEmpty(message.TextContent),
        views = message.Views,
        forwards = message.Forwards,
        sentAt = Iso(message.SentAt),
        isFromHistory = message.IsFromHistory,
        replyToMessageId = message.ReplyToMessageId,
    };

    internal static object MapMedia(MediaAttachmentModel attachment) => new
    {
        mediaId = attachment.MediaId?.Value,
        messageId = attachment.MessageId?.Value,
        mediaType = attachment.MediaType,
        fileName = NullIfEmpty(attachment.FileName),
        mimeType = NullIfEmpty(attachment.MimeType),
        fileSize = attachment.FileSize,
        width = attachment.Width,
        height = attachment.Height,
        durationSeconds = attachment.DurationSeconds,
        downloadStatus = attachment.DownloadStatus,
        storagePath = NullIfEmpty(attachment.StoragePath),
    };

    internal static object MapChat(MonitoredChatModel chat) => new
    {
        chatId = chat.ChatId?.Value,
        sessionId = chat.SessionId?.Value,
        telegramChatId = chat.TelegramChatId,
        title = chat.Title,
        username = NullIfEmpty(chat.Username),
        chatType = chat.ChatType,
        monitoringStatus = chat.MonitoringStatus,
        membersCount = chat.MembersCount,
        historyBackfillCompleted = chat.HistoryBackfillCompleted,
        totalMessagesCollected = chat.TotalMessagesCollected,
    };

    internal static object MapStatus(MonitoringStatusResponse status) => new
    {
        chatId = status.ChatId?.Value,
        monitoringStatus = status.MonitoringStatus,
        historyBackfillCompleted = status.HistoryBackfillCompleted,
        oldestMessageIdFetched = status.OldestMessageIdFetched,
        totalMessagesCollected = status.TotalMessagesCollected,
    };

    internal static object MapSession(SessionModel session) => new
    {
        sessionId = session.SessionId?.Value,
        displayName = session.DisplayName,
        telegramUserId = session.TelegramUserId,
        username = NullIfEmpty(session.Username),
        status = session.Status,
        lastConnectedAt = Iso(session.LastConnectedAt),
    };

    internal static object MapExport(ExportMessagesResponse response) => new
    {
        fileName = response.FileName,
        contentType = response.ContentType,
        contentLength = response.Content.Length,
        contentBase64 = response.Content.ToBase64(),
    };

    internal static object MapPage<T>(IEnumerable<T> items, int totalCount, int page, int pageSize, Func<T, object> map)
        => new { items = items.Select(map), totalCount, page, pageSize };

    private static string? Iso(Timestamp? timestamp)
        => timestamp?.ToDateTimeOffset().ToString("O");

    private static string? NullIfEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;
}
