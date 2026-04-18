using Microsoft.EntityFrameworkCore;
using SimpleFeedReader;

var builder = WebApplication.CreateBuilder(args);
Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "telegram-sessions"));

var databasePath = Path.Combine(AppContext.BaseDirectory, "telegram-sessions.db");

var lastPublished = DateTimeOffset.MinValue;
builder.Services.AddHttpClient("RssReader", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36 Edg/146.0.0.0");
});

builder.Services.AddDbContextFactory<TelegramSessionDbContext>(options =>
{
    options.UseSqlite($"Data Source={databasePath}");
});
builder.Services.AddSingleton<TelegramSessionStore>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TelegramSessionDbContext>>();
    using var dbContext = dbFactory.CreateDbContext();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.MapPost("/", async (PublishRequest request, IHttpClientFactory httpFactory, TelegramSessionStore sessionStore) =>
{
    if (!sessionStore.TryGetSession(request.Token, out var session))
    {
        return Results.Unauthorized();
    }

    if (request.Configs.Count == 0)
    {
        return Results.BadRequest("At least one RSS config is required.");
    }

    var client = session.GetClient();

    var reader = new FeedReader(httpFactory.CreateClient("RssReader"));
    foreach (var config in request.Configs)
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
    return Results.Ok();
});

app.MapPost("/telegram/signin", (TelegramSessionStore sessionStore, TelegramSigninRequest request) =>
{
    var token = sessionStore.CreateSession(new TelegramSessionSettings(request.AppId, request.AppHash, request.Phone));
    return Results.Ok(new TelegramSigninResponse { Token = token });
});

app.MapPost("/telegram/signin/{token}", async (string token, TelegramSessionStore sessionStore) =>
{
    if (!sessionStore.TryGetSession(token, out var session))
    {
        return Results.Unauthorized();
    }

    var client = session.GetClient();
    await client.LoginUserIfNeeded();
    return Results.Ok();
});

app.MapPost("/telegram/otp", (TelegramSessionStore sessionStore, TelegramAuthModel authModel) =>
{
    if (!sessionStore.TrySetOtp(authModel.Token, authModel.Code))
    {
        return Results.Unauthorized();
    }

    return Results.Accepted();
});

app.MapPost("/telegram/password", (TelegramSessionStore sessionStore, TelegramAuthModel authModel) =>
{
    if (!sessionStore.TrySetPassword(authModel.Token, authModel.Code))
    {
        return Results.Unauthorized();
    }

    return Results.Accepted();
});

app.Run();