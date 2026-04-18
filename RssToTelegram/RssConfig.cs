public sealed record RssConfig
{
    public long TelegramChannelId { get; init; }
    public string[] Feeds { get; init; } = [];
}