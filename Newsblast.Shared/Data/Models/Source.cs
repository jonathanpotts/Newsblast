using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Newsblast.Shared.Data.Models
{
    public class Source
    {
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Url { get; set; }
        public string FeedUrl { get; set; }
        public long __AddedByUserId { get; set; }

        [NotMapped]
        public ulong AddedByUserId
        {
            get
            {
                unchecked
                {
                    return (ulong)__AddedByUserId;
                }
            }

            set
            {
                unchecked
                {
                    __AddedByUserId = (long)value;
                }
            }
        }

        public List<Subscription> Subscriptions { get; set; }
        public List<Embed> Embeds { get; set; }
    }
}
