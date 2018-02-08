using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Newsblast.Shared.Data.Models
{
    public class Subscription
    {
        public string Id { get; set; }
        [Required]
        public long __ChannelId { get; set; }
        
        [NotMapped]
        public ulong ChannelId
        {
            get
            {
                unchecked
                {
                    return (ulong)__ChannelId;
                }
            }

            set
            {
                unchecked
                {
                    __ChannelId = (long)value;
                }
            }
        }

        public DateTime LastDate { get; set; }

        [Required]
        public string SourceId { get; set; }
        public Source Source { get; set; }
    }
}
