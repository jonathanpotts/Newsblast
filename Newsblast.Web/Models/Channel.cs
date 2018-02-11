using System.ComponentModel.DataAnnotations;

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
    }
}
