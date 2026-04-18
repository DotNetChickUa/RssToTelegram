using WTelegram;

sealed class TelegramSignInSession
{
    private readonly TelegramSessionSettings _settings;
    private readonly string _token;
    private readonly TelegramSessionStore _sessionStore;
    private readonly Lazy<Client> _client;

    internal TelegramSignInSession(string token, TelegramSessionSettings settings, TelegramSessionStore sessionStore)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or whitespace.", nameof(token));
        }

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(sessionStore);

        _token = token;
        _settings = settings;
        _sessionStore = sessionStore;
        _client = new Lazy<Client>(() => new Client(ConfigProvider));
    }

    internal Client GetClient()
    {
        return _client.Value;
    }

    private string? ConfigProvider(string what)
    {
        return what switch
        {
            "api_id" => _settings.AppId,
            "api_hash" => _settings.AppHash,
            "phone_number" => _settings.Phone,
            "session_pathname" => Path.Combine(AppContext.BaseDirectory, "telegram-sessions", _token),
            "verification_code" => WaitFor(() => _sessionStore.GetOtp(_token)),
            "password" => WaitFor(() => _sessionStore.GetPassword(_token)),
            _ => null
        };
    }

    private static string WaitFor(Func<string?> valueFactory)
    {
        while (true)
        {
            var value = valueFactory();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Thread.Sleep(250);
        }
    }
}