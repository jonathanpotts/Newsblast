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
        DbContextOptions<NewsblastContext> ContextOptions;
        DiscordManager Discord;

        public SubscriptionManager(DbContextOptions<NewsblastContext> contextOptions, DiscordManager discord)
        {
            ContextOptions = contextOptions;
            Discord = discord;
        }

        public async Task UpdateAsync(int maxParallelUpdates)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Updating subscriptions...");

            try
            {
                List<Subscription> subscriptions = null;

                using (var context = new NewsblastContext(ContextOptions))
                {
                    subscriptions = await context.Subscriptions
                        .Include(e => e.Source)
                        .ToListAsync();
                }

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
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Failed to process subscriptions...");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }

        async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Subscription updating: {subscription.Source.Name} -> {subscription.ChannelId}");

            try
            {
                using (var context = new NewsblastContext(ContextOptions))
                {
                    var trackedSubscription = context.Subscriptions
                        .Include(e => e.Source)
                        .ThenInclude(e => e.Embeds)
                        .First(e => e.Id == subscription.Id);

                    var embeds = trackedSubscription.Source.Embeds.OrderBy(e => e.Date);

                    if (trackedSubscription.LastDate == null || trackedSubscription.LastDate == new DateTime())
                    {
                        var embed = embeds.LastOrDefault();

                        if (embed != null)
                        {
                            await SendEmbedAsync(trackedSubscription.ChannelId, embed);

                            trackedSubscription.LastDate = embed.Date;
                        }
                    }
                    else
                    {
                        foreach (var embed in embeds)
                        {
                            if (trackedSubscription.LastDate < embed.Date)
                            {
                                await SendEmbedAsync(trackedSubscription.ChannelId, embed);
                            }
                        }

                        if (embeds.Count() > 0)
                        {
                            trackedSubscription.LastDate = embeds.Last().Date;
                        }
                    }

                    await context.SaveChangesAsync();
                }

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
