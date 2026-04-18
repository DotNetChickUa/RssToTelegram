using System.Text.Json;
using Microsoft.EntityFrameworkCore;

sealed class TelegramSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDbContextFactory<TelegramSessionDbContext> _dbContextFactory;

    public TelegramSessionStore(IDbContextFactory<TelegramSessionDbContext> dbContextFactory)
    {
        ArgumentNullException.ThrowIfNull(dbContextFactory);
        _dbContextFactory = dbContextFactory;
    }

    internal string CreateSession(TelegramSessionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.AppId))
        {
            throw new ArgumentException("AppId cannot be empty.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.AppHash))
        {
            throw new ArgumentException("AppHash cannot be empty.", nameof(settings));
        }

        if (string.IsNullOrWhiteSpace(settings.Phone))
        {
            throw new ArgumentException("Phone cannot be empty.", nameof(settings));
        }

        var token = Guid.NewGuid().ToString("N");

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Sessions.Add(new TelegramSessionEntity
        {
            Token = token,
            AppId = settings.AppId,
            AppHash = settings.AppHash,
            Phone = settings.Phone,
            RssConfigsJson = JsonSerializer.Serialize(new List<RssConfig>(), JsonOptions),
            LastPublishedAtUtc = DateTimeOffset.MinValue,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        dbContext.SaveChanges();

        return token;
    }

    internal bool TryGetSession(string token, out TelegramSignInSession session)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            session = null!;
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var entity = dbContext.Sessions.AsNoTracking().SingleOrDefault(x => x.Token == token);
        if (entity is null)
        {
            session = null!;
            return false;
        }

        session = new TelegramSignInSession(
            token,
            new TelegramSessionSettings(entity.AppId, entity.AppHash, entity.Phone),
            this);
        return true;
    }

    internal bool TrySetOtp(string token, string code)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var entity = dbContext.Sessions.AsTracking().SingleOrDefault(x => x.Token == token);
        if (entity is null)
        {
            return false;
        }

        entity.OtpCode = code;
        dbContext.SaveChanges();
        return true;
    }

    internal bool TrySetPassword(string token, string password)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var entity = dbContext.Sessions.AsTracking().SingleOrDefault(x => x.Token == token);
        if (entity is null)
        {
            return false;
        }

        entity.Password = password;
        dbContext.SaveChanges();
        return true;
    }

    internal bool TrySetRssConfigs(string token, IReadOnlyList<RssConfig> configs)
    {
        ArgumentNullException.ThrowIfNull(configs);

        if (string.IsNullOrWhiteSpace(token) || configs.Count == 0)
        {
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var entity = dbContext.Sessions.AsTracking().SingleOrDefault(x => x.Token == token);
        if (entity is null)
        {
            return false;
        }

        entity.RssConfigsJson = JsonSerializer.Serialize(configs, JsonOptions);
        dbContext.SaveChanges();
        return true;
    }

    internal bool TryGetRssConfigs(string token, out IReadOnlyList<RssConfig> configs)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            configs = [];
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var rssConfigsJson = dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => x.RssConfigsJson)
            .SingleOrDefault();

        if (string.IsNullOrWhiteSpace(rssConfigsJson))
        {
            configs = [];
            return false;
        }

        var deserialized = JsonSerializer.Deserialize<List<RssConfig>>(rssConfigsJson, JsonOptions);
        if (deserialized is null || deserialized.Count == 0)
        {
            configs = [];
            return false;
        }

        configs = deserialized;
        return true;
    }

    internal DateTimeOffset? GetLastPublishedAtUtc(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => x.LastPublishedAtUtc)
            .SingleOrDefault();
    }

    internal bool TrySetLastPublishedAtUtc(string token, DateTimeOffset value)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        var entity = dbContext.Sessions.AsTracking().SingleOrDefault(x => x.Token == token);
        if (entity is null)
        {
            return false;
        }

        entity.LastPublishedAtUtc = value;
        dbContext.SaveChanges();
        return true;
    }

    internal string? GetOtp(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => x.OtpCode)
            .SingleOrDefault();
    }

    internal string? GetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        using var dbContext = _dbContextFactory.CreateDbContext();
        return dbContext.Sessions
            .AsNoTracking()
            .Where(x => x.Token == token)
            .Select(x => x.Password)
            .SingleOrDefault();
    }
}