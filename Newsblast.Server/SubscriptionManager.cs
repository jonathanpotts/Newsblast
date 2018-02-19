using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newsblast.Shared.Data;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Server
{
    public class SubscriptionManager
    {
        ILogger Logger;
        DbContextOptions<NewsblastContext> ContextOptions;
        DiscordManager Discord;

        public SubscriptionManager(ILogger logger, DbContextOptions<NewsblastContext> contextOptions, DiscordManager discord)
        {
            Logger = logger;
            ContextOptions = contextOptions;
            Discord = discord;
        }

        public async Task UpdateAsync(int maxParallelUpdates)
        {
            Logger.LogInformation("Updating subscriptions.");

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

                Logger.LogInformation($"Subscriptions processed: {subscriptions.Count().ToString()}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process subscriptions.");
            }
        }

        async Task UpdateSubscriptionAsync(Subscription subscription)
        {
            Logger.LogInformation($"Subscription updating: {subscription.Source.Name} -> {subscription.ChannelId}");

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

                Logger.LogInformation($"Subscription updated: {subscription.Source.Name} -> {subscription.ChannelId.ToString()}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to update subscription: {subscription.Source.Name} -> {subscription.ChannelId.ToString()}");
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
