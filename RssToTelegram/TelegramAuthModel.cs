public sealed record TelegramAuthModel
{
    public required string Token { get; init; }
    public required string Code { get; init; }
}