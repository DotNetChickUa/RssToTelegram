# RssToTelegram

The app allows to publish messages from RSS feeds to Telegram channels. It is built using .NET 10 and C# 14.

> You need to have a telegram account to use the app!

Existing channels:
- https://t.me/+vQkCEAIIGZllZTNi
- https://t.me/+YWk9ZSV2PKozMDVi

## Requests

- Sign in:

    POST https://localhost:7091/telegram/signin
    ```json
    {
        "AppId": "YOUR_APP_ID",
        "AppHash": "YOUR_APP_HASH",
        "Phone": "YOUR_PHONE_NUMBER"
    }
    ```

- Set otp:
    
    POST https://localhost:7091/telegram/otp
    ```json
    {
        "code":"YOUR_CODE_FROM_TELEGRAM",
        "token":"YOUR_TOKEN_FROM_SIGNIN_RESPONSE"
    }
    ```

- Set password:
    
    POST https://localhost:7091/telegram/password
    ```json
    {
        "code":"YOUR_TELEGTAM_PASSWORD",
        "token":"YOUR_TOKEN_FROM_SIGNIN_RESPONSE"
    }
    ```

- Setup RSS to Telegram:
     
     POST https://localhost:7091/telegram/config
     ```json
     {
        "Token": "YOUR_TOKEN_FROM_SIGNIN_RESPONSE",
        "Configs": [
          {
            "Feeds": [ "https://blogs.microsoft.com/feed/", "https://blogs.windows.com/feed/" ],
            "TelegramChannelId": "Telegram channel Id 1"
          },
          {
            "Feeds": [ "https://news.microsoft.com/feed/" ],
            "TelegramChannelId": "Telegram channel Id 2"
          }
        ]
    }
     ```

- Publish RSS to Telegram:
     
     POST https://localhost:7091/
     ```json
     {
        "Token": "YOUR_TOKEN_FROM_SIGNIN_RESPONSE"
    }
     ```
