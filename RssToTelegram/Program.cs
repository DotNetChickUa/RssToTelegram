using Microsoft.Extensions.Options;
using SimpleFeedReader;
using WTelegram;

string? _password = null;
string? _otp = null;

var builder = WebApplication.CreateBuilder(args);
var lastPublished = DateTimeOffset.MinValue;
builder.Services.AddHttpClient("RssReader", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36 Edg/146.0.0.0");
});
builder.Services.AddSingleton<Client>(sp =>
{
    var telegram = builder.Configuration.GetSection("Telegram").Get<TelegramSettings>();

    string ConfigProvider(string what)
    {
        switch (what)
        {
            case "api_id":
                return telegram.AppId;
            case "api_hash":
                return telegram.AppHash;
            case "phone_number":
                return telegram.Phone;
            case "session_pathname":
                return Path.Combine(AppContext.BaseDirectory, "telegram.session");
            case "verification_code":
                while (_otp == null)
                    Thread.Sleep(1000);

                return _otp;
            case "password":
                while (_password == null)
                    Thread.Sleep(1000);

                return _password;
            default:
                return null;
        }
    }

    var client = new Client(ConfigProvider);
    return client;
});

builder.Services.Configure<RssTelegramConfiguration>(builder.Configuration.GetSection("RssTelegramConfiguration"));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", async (IHttpClientFactory httpFactory, Client client, IOptions<RssTelegramConfiguration> options) =>
{
    var reader = new FeedReader(httpFactory.CreateClient("RssReader"));
    foreach (var config in options.Value.Configs)
    {
        var feeds = new List<FeedItemExtended>();
        foreach (var feed in config.Feeds)
        {
            var feedItems = await reader.RetrieveFeedAsync(feed);
            foreach (var feedItem in feedItems)
            {
                feeds.Add(new FeedItemExtended(feed, feedItem));
            }
        }

        var itemsToPublish = feeds.Where(x => x.Item.PublishDate > lastPublished).OrderBy(x => x.Item.PublishDate);

        await client.LoginUserIfNeeded();
        var chats = await client.Messages_GetAllChats();
        var chat = chats.chats[config.TelegramChannelId];
        foreach (var item in itemsToPublish)
        {
            await client.SendMessageAsync(chat, item.Item.Title + Environment.NewLine + item.Item.Summary + Environment.NewLine + item.Item.Uri);
        }
    }

    lastPublished = DateTimeOffset.UtcNow;
});

app.MapPost("/telegram/otp", (TelegramAuthModel code) =>
{
    _otp = code.Code;
});

app.MapPost("/telegram/password", (TelegramAuthModel code) =>
{
    _password = code.Code;
});

app.Run();

public record FeedItemExtended(string Feed, FeedItem Item);

public class RssConfig
{
    public long TelegramChannelId { get; set; }
    public string[] Feeds { get; set; } = [];
}

public class RssTelegramConfiguration
{
    public List<RssConfig> Configs { get; set; } = [];
}
public class TelegramSettings
{
    public required string AppId { get; set; }
    public required string AppHash { get; set; }
    public required string Phone { get; set; }
}


public class TelegramAuthModel
{
    public required string Code { get; set; }
}