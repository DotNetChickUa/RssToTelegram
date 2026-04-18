public sealed record TelegramSigninRequest
{
    public required string AppId { get; init; }
    public required string AppHash { get; init; }
    public required string Phone { get; init; }
}