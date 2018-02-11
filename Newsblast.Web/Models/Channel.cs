using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Web.Models
{
    public class Channel
    {
        [Required]
        public ulong Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public Guild Guild { get; set; }
        public List<Subscription> Subscriptions { get; set; }
    }
}
