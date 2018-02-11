using System.ComponentModel.DataAnnotations;

namespace Newsblast.Web.Models.ViewModels
{
    public class AddSourceViewModel
    {
        [Required]
        [Url]
        [Display(Name = "RSS Feed URL")]
        public string FeedUrl { get; set; }
    }
}
