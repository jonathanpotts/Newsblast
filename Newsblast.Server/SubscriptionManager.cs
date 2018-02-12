using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newsblast.Shared.Data;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Server
{
    public class SubscriptionManager
    {
        NewsblastContext Context;
        DiscordManager Discord;

        public SubscriptionManager(NewsblastContext context, DiscordManager discord)
        {
            Context = context;
            Discord = discord;
        }

        public async Task UpdateAsync(int maxParallelUpdates)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Updating subscriptions...");

            var subscriptions = await Context.Subscriptions
                .Include(e => e.Source)
                .ThenInclude(e => e.Embeds)
                .ToListAsync();

            var updates = new List<Task>();

            foreach (var subscription in subscriptions)
            {
                updates.Add(UpdateSubscriptionAsync(subscription));

                if (updates.Count >= maxParallelUpdates)
                {
                    await Task.WhenAll(updates);
                    updates.Clear();
                }
            }

            if (updates.Count > 0)
            {
                await Task.WhenAll(updates);
            }

            Console.WriteLine($"{DateTime.Now.ToString()} - Subscriptions processed: {subscriptions.Count().ToString()}");
        }

        async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Subscription updating: {subscription.Source.Name} -> {subscription.ChannelId}");

            try
            {
                var embeds = subscription.Source.Embeds.OrderBy(e => e.Date);

                if (subscription.LastDate == null || subscription.LastDate == new DateTime())
                {
                    var embed = embeds.LastOrDefault();

                    if (embed != null)
                    {
                        await SendEmbedAsync(subscription.ChannelId, embed);

                        subscription.LastDate = embed.Date;
                    }
                }
                else
                {
                    foreach (var embed in embeds)
                    {
                        if (subscription.LastDate < embed.Date)
                        {
                            await SendEmbedAsync(subscription.ChannelId, embed);
                        }
                    }

                    if (embeds.Count() > 0)
                    {
                        subscription.LastDate = embeds.Last().Date;
                    }
                }

                await Context.SaveChangesAsync();

                Console.WriteLine($"{DateTime.Now.ToString()} - Subscription updated: {subscription.Source.Name} -> {subscription.ChannelId.ToString()}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Failed to update subscription: {subscription.Source.Name} -> {subscription.ChannelId.ToString()}");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        async Task SendEmbedAsync(ulong channelId, Embed embed)
        {
            var discordEmbed = new Discord.EmbedBuilder()
            .WithAuthor(embed.Source.Name, null, embed.Source.Url)
            .WithTitle(embed.Title)
            .WithUrl(embed.Url)
            .WithDescription(embed.Description)
            .WithTimestamp(embed.Date);

            if (embed.ImageUrl != null && embed.ImageUrl.Length > 0)
            {
                discordEmbed.WithImageUrl(embed.ImageUrl);
            }

            await Discord.SendMessageAsync(channelId, "", discordEmbed.Build());
        }
    }
}
