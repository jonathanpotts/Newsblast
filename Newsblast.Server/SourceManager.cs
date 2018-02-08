using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
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

        NewsblastContext Context;

        public SourceManager(NewsblastContext context)
        {
            Context = context;
        }

        public async Task UpdateAsync(int maxParallelUpdates)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Updating sources...");

            var sources = await Context.Sources
                .Include(e => e.Embeds)
                .Include(e => e.Subscriptions)
                .Where(e => e.Subscriptions.Count > 0)
                .ToListAsync();

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

            Console.WriteLine($"{DateTime.Now.ToString()} - Sources processed: {sources.Count()}");
        }

        async Task UpdateEmbedsAsync(Source source)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - Source updating: { source.Name }");

            try
            {
                var xml = XmlReader.Create(source.FeedUrl, new XmlReaderSettings() { Async = true });
                var rss = new RssFeedReader(xml);

                source.Embeds.Clear();

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
                            Url = item.Links.Where(e => e.RelationshipType == "alternate").FirstOrDefault()?.Uri.ToString(),
                            Date = item.Published.DateTime,
                            Description = description
                        };

                        var web = new HtmlWeb();
                        html = await web.LoadFromWebAsync(embed.Url);

                        embed.ImageUrl = html.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);

                        source.Embeds.Add(embed);
                    }
                }

                await Context.SaveChangesAsync();

                Console.WriteLine($"{DateTime.Now.ToString()} - Source updated: {source.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now.ToString()} - Failed to update source: {source.Name}");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }
}
