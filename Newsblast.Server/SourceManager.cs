using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using HtmlAgilityPack;
using Newsblast.Shared.Data;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Server
{
    public class SourceManager
    {
        const int MaxDescriptionLength = 256;
        const int MaxEmbeds = 20;

        ILogger Logger;
        DbContextOptions<NewsblastContext> ContextOptions;

        public SourceManager(ILogger logger, DbContextOptions<NewsblastContext> contextOptions)
        {
            Logger = logger;
            ContextOptions = contextOptions;
        }

        public async Task UpdateAsync(int maxParallelUpdates)
        {
            Logger.LogInformation("Updating sources.");

            try
            {
                List<Source> sources = null;

                using (var context = new NewsblastContext(ContextOptions))
                {
                    sources = await context.Sources
                        .Include(e => e.Subscriptions)
                        .Where(e => e.Subscriptions.Count > 0)
                        .ToListAsync();
                }

                var updates = new List<Task>();

                foreach (var source in sources)
                {
                    updates.Add(UpdateEmbedsAsync(source));

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

                Logger.LogInformation($"Sources processed: {sources.Count()}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process sources.");
            }
        }

        async Task UpdateEmbedsAsync(Source source)
        {
            Logger.LogInformation($"Source updating: {source.Name}");

            try
            {
                using (var context = new NewsblastContext(ContextOptions))
                {
                    var trackedSource = context.Sources
                        .Include(e => e.Embeds)
                        .First(e => e.Id == source.Id);

                    using (var xml = XmlReader.Create(trackedSource.FeedUrl, new XmlReaderSettings() { Async = true }))
                    {
                        var rss = new RssFeedReader(xml);

                        while (await rss.Read())
                        {
                            if (rss.ElementType == SyndicationElementType.Item)
                            {
                                var item = await rss.ReadItem();

                                var html = new HtmlDocument();
                                html.LoadHtml(item.Description);

                                var description = HtmlEntity.DeEntitize(html.DocumentNode.InnerText);

                                if (description.Length > MaxDescriptionLength)
                                {
                                    description = description.Substring(0, description.Substring(0, (MaxDescriptionLength - 4)).LastIndexOf(" "));
                                    description += " ...";
                                }

                                var embed = new Embed()
                                {
                                    Title = item.Title,
                                    Url = item.Links.FirstOrDefault(e => e.RelationshipType == "alternate")?.Uri.ToString(),
                                    Date = item.Published.UtcDateTime,
                                    Description = description
                                };

                                if (trackedSource.Embeds.FirstOrDefault(e => e.Url == embed.Url) == null)
                                {
                                    var web = new HtmlWeb();
                                    html = await web.LoadFromWebAsync(embed.Url);

                                    embed.ImageUrl = html.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);

                                    trackedSource.Embeds.Add(embed);
                                }
                            }
                        }
                    }

                    if (trackedSource.Embeds.Count > MaxEmbeds)
                    {
                        var oldEmbeds = trackedSource.Embeds.OrderByDescending(e => e.Date).TakeLast(trackedSource.Embeds.Count - MaxEmbeds);

                        foreach (var oldEmbed in oldEmbeds)
                        {
                            trackedSource.Embeds.Remove(oldEmbed);
                        }
                    }

                    await context.SaveChangesAsync();
                }

                Logger.LogInformation($"Source updated: {source.Name}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to update source: {source.Name}");
            }
        }
    }
}
