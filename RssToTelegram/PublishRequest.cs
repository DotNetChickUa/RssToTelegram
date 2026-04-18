public sealed record PublishRequest
{
    public required string Token { get; init; }
    public List<RssConfig> Configs { get; init; } = [];
}