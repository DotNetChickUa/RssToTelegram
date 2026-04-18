sealed class TelegramSessionEntity
{
    internal string Token { get; set; } = string.Empty;
    internal string AppId { get; set; } = string.Empty;
    internal string AppHash { get; set; } = string.Empty;
    internal string Phone { get; set; } = string.Empty;
    internal string? OtpCode { get; set; }
    internal string? Password { get; set; }
    internal DateTimeOffset CreatedAtUtc { get; set; }
}