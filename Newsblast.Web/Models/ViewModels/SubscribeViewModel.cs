using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newsblast.Shared.Data.Models;

namespace Newsblast.Web.Models.ViewModels
{
    public class SubscribeViewModel
    {
        public Channel Channel { get; set; }
        public List<Source> Sources { get; set; }
        [Required]
        [Display(Name = "Feed")]
        public string SourceId { get; set; }
    }
}
