﻿using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;
using Newsblast.Shared.Data;
using Newsblast.Shared.Data.Models;
using Newsblast.Web.Models.ViewModels;

namespace Newsblast.Web.Controllers
{
    [Route("source")]
    [Authorize]
    public class SourceController : Controller
    {
        NewsblastContext Context;

        public SourceController(NewsblastContext context)
        {
            Context = context;
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

                var xml = XmlReader.Create(model.FeedUrl, new XmlReaderSettings() { Async = true });
                var rss = new RssFeedReader(xml);

                var name = "";
                var url = "";

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

                var source = Context.Sources.Add(new Source()
                {
                    Name = name,
                    Url = url,
                    FeedUrl = model.FeedUrl
                });

                await Context.SaveChangesAsync();

                return RedirectToAction("Inspect", new { id = source.Entity.Id });
            }

            return View();
        }
    }
}