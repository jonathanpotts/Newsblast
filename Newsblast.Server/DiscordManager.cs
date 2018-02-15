﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Discord;
using Discord.WebSocket;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    public class DiscordManager : IDisposable
    {
        const int timeoutInSeconds = 30;

        string ConnectionString;
        string Token;

        DiscordSocketClient Client;
        CancellationTokenSource CancellationToken;

        public DiscordManager(string connectionString, string token)
        {
            ConnectionString = connectionString;

            Client = new DiscordSocketClient();
            Token = token;

            CancellationToken = new CancellationTokenSource();

            Client.Disconnected += Disconnected;
            Client.JoinedGuild += JoinedGuild;
            Client.GuildAvailable += GuildAvailable;
            Client.LeftGuild += LeftGuild;
            Client.GuildUnavailable += GuildUnavailable;
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

            Console.WriteLine($"{DateTime.Now.ToString()} - Discord message sent: {channel.Guild.Name} ({channel.Guild.Id.ToString()}) -> {channel.Name} ({channel.Id.ToString()})");
        }

        Task Connected()
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord connected.");

            CancellationToken.Cancel();
            CancellationToken.Dispose();
            CancellationToken = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        Task Disconnected(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord disconnected: {ex.Message}");
            Console.WriteLine($"{DateTime.Now.ToString()} - Waiting for {timeoutInSeconds} seconds to allow Discord time to reconnect.");
            Console.ResetColor();

            Task.Delay(timeoutInSeconds * 1000, CancellationToken.Token).ContinueWith(async _ =>
            {
                if (Client.ConnectionState != ConnectionState.Connected)
                {
                    Client.Dispose();

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{DateTime.Now.ToString()} - Discord failed to reconnect. Resetting client...");
                    Console.ResetColor();

                    Client = new DiscordSocketClient();
                    await ConnectAsync();
                }
            });

            return Task.CompletedTask;
        }

        Task JoinedGuild(SocketGuild guild)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild joined: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task GuildAvailable(SocketGuild guild)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild available: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        async Task LeftGuild(SocketGuild guild)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild left: {guild.Name} ({guild.Id.ToString()})");

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<NewsblastContext>()
                            .UseSqlServer(ConnectionString);

                using (var context = new NewsblastContext(optionsBuilder.Options))
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Failed to remove subscriptions...");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        Task GuildUnavailable(SocketGuild guild)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild unavailable: {guild.Name} ({guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task MessageReceived(SocketMessage message)
        {
            if (message.MentionedUsers.Contains(Client.CurrentUser))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} - Discord message received - bot mentioned: {message.Channel.Id.ToString()}");
                // SendMessageAsync(message.Channel, $"{message.Author.Mention} Hello!");
            }

            return Task.CompletedTask;
        }
    }
}
