using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Newsblast.Shared.Data;

namespace Newsblast.Server
{
    public class DiscordManager : IDisposable
    {
        NewsblastContext Context;
        DiscordClient Client;

        public DiscordManager(NewsblastContext context, string botToken)
        {
            Context = context;

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = botToken,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            });

            Client.GuildCreated += GuildCreated;
            Client.GuildAvailable += GuildAvailable;
            Client.GuildDeleted += GuildDeleted;
            Client.GuildUnavailable += GuildUnavailable;

            Client.MessageCreated += MessageCreated;
        }

        public void Dispose()
        {
            Client.Dispose();
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
        }

        public async Task DisconnectAsync()
        {
            await Client.DisconnectAsync();
        }

        public async Task SendMessageAsync(ulong channelId, string content = null, DiscordEmbed embed = null)
        {
            try
            {
                var channel = await Client.GetChannelAsync(channelId);
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

        Task GuildCreated(GuildCreateEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild access created: {e.Guild.Name} ({e.Guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task GuildAvailable(GuildCreateEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild available: {e.Guild.Name} ({e.Guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task GuildDeleted(GuildDeleteEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild access deleted: {e.Guild.Name} ({e.Guild.Id.ToString()})");

            var channelIds = e.Guild.Channels.Select(c => c.Id);

            return Task.CompletedTask;
        }

        Task GuildUnavailable(GuildDeleteEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Discord guild unavailable: {e.Guild.Name} ({e.Guild.Id.ToString()})");
            return Task.CompletedTask;
        }

        Task MessageCreated(MessageCreateEventArgs e)
        {
            if (e.MentionedUsers.Contains(Client.CurrentUser))
            {
                Console.WriteLine($"{DateTime.Now.ToString()} - Discord message created - bot mentioned: {e.Guild.Name} ({e.Guild.Id.ToString()}) -> {e.Channel.Name} ({e.Channel.Id.ToString()})");
                // SendMessageAsync(e.Channel, $"{e.Message.Author.Mention} Hello!");
            }

            return Task.CompletedTask;
        }
    }
}
