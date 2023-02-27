using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace MVC_Music.Models
{
    public class Genre : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "You cannot leave the name blank.")]
        [StringLength(50, ErrorMessage = "Name cannot be more than 50 characters long.")]
        public string Name { get; set; }

        //collections
        [Display(Name = "Album")]
        public ICollection<Album> Albums { get; set; } = new HashSet<Album>();

        [Display(Name = "Song")]
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
    }
}
