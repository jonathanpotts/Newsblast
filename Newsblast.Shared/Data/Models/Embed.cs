using System;
using System.ComponentModel.DataAnnotations;

namespace Newsblast.Shared.Data.Models
{
    public class Embed
    {
        public string Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public string Url { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        [Required]
        public string SourceId { get; set; }
        public Source Source { get; set; }
    }
}
