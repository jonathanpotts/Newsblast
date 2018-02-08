using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Newsblast.Shared.Data.Models
{
    public class Source
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Url { get; set; }
        public string FeedUrl { get; set; }
        
        public List<Subscription> Subscriptions { get; set; }
        public List<Embed> Embeds { get; set; }
    }
}
