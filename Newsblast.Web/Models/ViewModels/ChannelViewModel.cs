using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newsblast.Shared.Data.Models;
using Newsblast.Web.Models;

namespace Newsblast.Web.Models.ViewModels
{
    public class ChannelViewModel
    {
        [Required]
        public Channel Channel { get; set; }
        public List<Source> Sources { get; set; }
    }
}
