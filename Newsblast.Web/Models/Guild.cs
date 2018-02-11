using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Newsblast.Web.Models
{
    public class Guild
    {
        [Required]
        public ulong Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string IconUrl { get; set; }
        [Required]
        public bool IsAdministrator { get; set; }
        [Required]
        public bool BotConnected { get; set; }
        public List<Channel> Channels { get; set; }
    }
}
