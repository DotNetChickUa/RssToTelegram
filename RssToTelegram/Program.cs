using Microsoft.Extensions.Options;
using SimpleFeedReader;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("RssReader", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36 Edg/146.0.0.0");
});

builder.Services.Configure<RssTelegramConfiguration>(builder.Configuration.GetSection("RssTelegramConfiguration"));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", async (IHttpClientFactory httpFactory, IOptions<RssTelegramConfiguration> options) =>
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

        var itemsToPublish = feeds.OrderBy(x => x.Item.PublishDate);

        var bot = new TelegramBotClient("");
        bot.OnMessage += OnMessage;

        Task OnMessage(Message message, UpdateType type)
        {
            message.Chat
            foreach (var item in itemsToPublish)
            {
                await bot.SendMessage(config.TelegramChannelId, item.Item.Title);
            }
        }

    }
});

app.Run();

public record FeedItemExtended(string Feed, FeedItem Item);
public class RssConfig
{
    public string TelegramChannelId { get; set; }
    public string[] Feeds { get; set; } = [];
}

public class RssTelegramConfiguration
{
    public List<RssConfig> Configs { get; set; } = [];
}