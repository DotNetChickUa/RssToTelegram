public sealed record TelegramConfigRequest
{
    public required string Token { get; init; }
    public List<RssConfig> Configs { get; init; } = [];
}
