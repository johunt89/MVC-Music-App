using System.ComponentModel.DataAnnotations;

namespace MVC_Music.Models
{
    public class Song : Auditable
    {
        public int ID { get; set; }

        [Display(Name = "Title")]
        [Required(ErrorMessage = "Must enter a song title")]
        [StringLength(80, ErrorMessage = "title cannot be longer than 80 characters.")]
        public string Title { get; set; }

        [Display(Name = "Date Recorded")]
        [Required(ErrorMessage = "Must enter date recroded.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateRecorded { get; set; }

        [ScaffoldColumn(false)]
        [Timestamp]
        public Byte[] RowVersion { get; set; }//Added for concurrency

        [Display(Name = "Genre")]
        [Required(ErrorMessage = "You must enter a Genre ID.")]
        public int GenreID { get; set; }

        [Display(Name = "Genre")]
        public Genre Genre { get; set; }


        //navigation properties
        [Display(Name = "Album")]
        [Required(ErrorMessage = "You must enter a Album ID.")]
        public int AlbumID { get; set; }

        [Display(Name = "Album")]
        public Album Album { get; set; }

        //collection
        [Display(Name = "Performance")]
        public ICollection<Performance> Performances { get; set; } = new HashSet<Performance>();
    }
}
