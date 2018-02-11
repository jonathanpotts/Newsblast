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
    }
}
