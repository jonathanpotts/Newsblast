using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using Newsblast.Shared.Data;
using Newsblast.Shared.Data.Models;
using Newsblast.Web.Services;
using Newsblast.Web.Models.ViewModels;

namespace Newsblast.Web.Controllers
{
    [Route("source")]
    [Authorize]
    public class SourceController : Controller
    {
        NewsblastContext Context;
        DiscordUserClient UserClient;

        public SourceController(NewsblastContext context, DiscordUserClient userClient)
        {
            Context = context;
            UserClient = userClient;
        }

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var sources = await Context.Sources.ToListAsync();

            return View(sources);
        }

        [Route("{id}")]
        public IActionResult Inspect(string id)
        {
            var source = Context.Sources
                .Include(e => e.Embeds)
                .FirstOrDefault(e => e.Id == id);

            if (source == null)
            {
                return NotFound();
            }

            return View(source);
        }

        [HttpGet]
        [Route("add")]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("add")]
        public async Task<IActionResult> Add(AddSourceViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (Context.Sources.FirstOrDefault(e => e.FeedUrl == model.FeedUrl) != null)
                {
                    ModelState.AddModelError("FeedUrl", "This feed has already been added.");
                    return View();
                }

                var name = "";
                var url = "";

                try
                {
                    using (var xml = XmlReader.Create(model.FeedUrl, new XmlReaderSettings() { Async = true }))
                    {
                        var rss = new RssFeedReader(xml);

                        while (await rss.Read())
                        {
                            if (rss.ElementType == SyndicationElementType.Content && rss.ElementName == "title")
                            {
                                var content = await rss.ReadContent();
                                name = content.Value;
                            }
                            else if (rss.ElementType == SyndicationElementType.Link && rss.ElementName == "link")
                            {
                                var link = await rss.ReadLink();
                                url = link.Uri.AbsoluteUri;
                            }

                            if (name != "" && url != "")
                            {
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError("FeedUrl", "There was a problem while processing this feed.");
                    return View();
                }

                if (name.EndsWith(" RSS Feed"))
                {
                    name = name.Substring(0, name.LastIndexOf(" RSS Feed"));
                }

                if (name == "" || url == "")
                {
                    ModelState.AddModelError("FeedUrl", "The provided RSS feed is invalid.");
                    return View();
                }

                if (Context.Sources.FirstOrDefault(e => e.Name == name) != null)
                {
                    ModelState.AddModelError("FeedUrl", "There is already a feed with that name.");
                    return View();
                }

                var client = await UserClient.GetRestClientAsync();
                var userId = client.CurrentUser?.Id;

                if (userId == null)
                {
                    ModelState.AddModelError("", "The current user session is invalid.");
                }

                var source = await Context.Sources.AddAsync(new Source()
                {
                    Name = name,
                    Url = url,
                    FeedUrl = model.FeedUrl,
                    AddedByUserId = userId.Value
                });

                await Context.SaveChangesAsync();

                return RedirectToAction("Inspect", new { id = source.Entity.Id });
            }

            return View();
        }

        [Route("image/{embedId}")]
        public async Task<IActionResult> Image(string embedId)
        {
            var embed = Context.Embeds
                .FirstOrDefault(e => e.Id == embedId);

            if (embed == null || embed.ImageUrl == null)
            {
                return NotFound();
            }

            using (var client = new HttpClient())
            {
                var response = await client.GetAsync(embed.ImageUrl);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    return StatusCode((int)response.StatusCode);
                }

                return new FileStreamResult(await response.Content.ReadAsStreamAsync(), response.Content.Headers.ContentType.MediaType);
            }
        }
    }
}