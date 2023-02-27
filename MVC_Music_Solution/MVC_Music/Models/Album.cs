using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace MVC_Music.Models
{
    public class Album : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Name")]
        [Required(ErrorMessage = "You cannot leave the name blank.")]
        [StringLength(50, ErrorMessage = "Name cannot be more than 50 characters long.")]
        public string Name { get; set; }

        [Display(Name = "Year Produced")]
        [Required(ErrorMessage = "You cannot leave year produced blank.")]
        [RegularExpression("^\\d{4}$", ErrorMessage = "Please enter a valid 4 digit year.")]
        public string YearProduced { get; set; }

        [Display(Name = "Price")]
        [Required(ErrorMessage = "You must enter a price.")]
        [Range(1, 200000, ErrorMessage = "")]
        [DataType(DataType.Currency)]
        public double Price { get; set; }

        [ScaffoldColumn(false)]
        [Timestamp]
        public Byte[] RowVersion { get; set; }//Added for concurrency


        //Navigation Properties
        [Display(Name = "Genre")]
        public int GenreID { get; set; }
        public Genre Genre { get; set; }

        //collection
        [Display(Name = "Songs")]
        public ICollection<Song> Songs { get; set; } = new HashSet<Song>();
    }
}
