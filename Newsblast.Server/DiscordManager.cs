using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    public class DiscordManager : IDisposable
    {
        string Token;

        NewsblastContext Context;
        DiscordSocketClient Client;

        public DiscordManager(NewsblastContext context, string token)
        {
            Context = context;

            Client = new DiscordSocketClient();
            Token = token;

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

        public async Task SendMessageAsync(ulong channelId, string content = null, Embed embed = null)
        {
            try
            {
                var channel = Client.GetChannel(channelId) as SocketTextChannel;
                await channel.SendMessageAsync(content, false, embed);

                Console.WriteLine($"{DateTime.Now.ToString()} - Discord message sent: {channel.Guild.Name} ({channel.Guild.Id.ToString()}) -> {channel.Name} ({channel.Id.ToString()})");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Failed to send message: {ex.Message}");
                Console.ResetColor();
            }
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

        Task LeftGuild(SocketGuild guild)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild left: {guild.Name} ({guild.Id.ToString()})");

            var channelIds = guild.Channels.Select(c => c.Id);

            return Task.CompletedTask;
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
