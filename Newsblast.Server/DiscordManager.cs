using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.WebSocket;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    public class DiscordManager : IDisposable
    {
        const int TimeoutInSeconds = 30;

        ILogger Logger;
        DbContextOptions<NewsblastContext> ContextOptions;
        string Token;

        bool AwaitingReconnect;

        DiscordSocketClient Client;
        CancellationTokenSource CancellationToken;

        public DiscordManager(ILogger logger, DbContextOptions<NewsblastContext> contextOptions, string token)
        {
            Logger = logger;
            ContextOptions = contextOptions;

            Token = token;
            CreateClient();

            CancellationToken = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public async Task ConnectAsync()
        {
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();
        }

        public async Task DisconnectAsync()
        {
            await Client.LogoutAsync();
        }

        public async Task SendMessageAsync(ulong channelId, string content = "", Embed embed = null)
        {
            var channel = Client.GetChannel(channelId) as SocketTextChannel;
            await channel.SendMessageAsync(content, false, embed);

            Logger.LogInformation($"Discord message sent: {channel.Guild.Name} ({channel.Guild.Id.ToString()}) -> {channel.Name} ({channel.Id.ToString()})");
        }

        void CreateClient()
        {
            Client = new DiscordSocketClient();

            Client.Connected += Connected;
            Client.Disconnected += Disconnected;
            Client.JoinedGuild += JoinedGuild;
            Client.GuildAvailable += GuildAvailable;
            Client.LeftGuild += LeftGuild;
            Client.GuildUnavailable += GuildUnavailable;
        }

        async Task ResetClientAsync()
        {
            if (Client.ConnectionState != ConnectionState.Connected)
            {
                Client.Dispose();

                Logger.LogWarning("Resetting Discord client.");

                CreateClient();
                await ConnectAsync();
            }
        }

        Task Connected()
        {
            Logger.LogInformation("Discord client connected.");

            AwaitingReconnect = false;
            CancellationToken.Cancel();
            CancellationToken.Dispose();
            CancellationToken = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        Task Disconnected(Exception ex)
        {
            if (!AwaitingReconnect)
            {
                AwaitingReconnect = true;

                Logger.LogWarning(ex, $"Discord client disconnected. Waiting for {TimeoutInSeconds} seconds to allow Discord time to reconnect.");

                Task.Delay(TimeoutInSeconds * 1000, CancellationToken.Token).ContinueWith(async _ =>
                {
                    while (AwaitingReconnect)
                    {
                        await ResetClientAsync();

                        await Task.Delay(TimeoutInSeconds * 1000);
                    }
                    
                });
            }
            else
            {
                Logger.LogWarning(ex, "Discord client disconnected.");
            }

            return Task.CompletedTask;
        }

        Task JoinedGuild(SocketGuild guild)
        {
            Logger.LogInformation($"Discord guild joined: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task GuildAvailable(SocketGuild guild)
        {
            Logger.LogInformation($"Discord guild available: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        async Task LeftGuild(SocketGuild guild)
        {
            Logger.LogInformation($"Discord guild left: {guild.Name} ({guild.Id.ToString()})");

            try
            {
                using (var context = new NewsblastContext(ContextOptions))
                {
                    var channelIds = guild.Channels.Select(c => c.Id);
                    var subscriptions = context.Subscriptions.Where(e => channelIds.Contains(e.ChannelId));

                    foreach (var subscription in subscriptions)
                    {
                        context.Subscriptions.Remove(subscription);
                    }

                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to remove subscriptions.");
            }
        }

        Task GuildUnavailable(SocketGuild guild)
        {
            Logger.LogWarning($"Discord guild unavailable: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task MessageReceived(SocketMessage message)
        {
            if (message.MentionedUsers.Contains(Client.CurrentUser))
            {
                Logger.LogInformation($"Discord message received - bot mentioned: {message.Channel.Id.ToString()}");
                // SendMessageAsync(message.Channel, $"{message.Author.Mention} Hello!");
            }

            return Task.CompletedTask;
        }
    }
}
