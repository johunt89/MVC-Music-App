using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MVC_Music.Models
{
    public class Performance
    {
        public int ID { get; set; }

        [Display(Name ="Comments")]
        [Required(ErrorMessage = "You must enter a comment at least 10 characters long.")]
        [StringLength(250, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 250 characters long")]
        public string Comments { get; set; }


        [Display(Name = "Fee Paid")]
        [Required(ErrorMessage = "You must enter an amount for the extra fee.")]
        [DataType(DataType.Currency)]
        public double FeePaid { get; set; }


        //navigation properties

        [Display(Name = "Song")]
        [Required(ErrorMessage = "You must enter a Song ID.")]
        public int SongID { get; set; }
        public Song Song { get; set; }

        [Display(Name = "Musician")]
        [Required(ErrorMessage = "You must enter a Musician ID.")]
        public int MusicianID { get; set; }
        public Musician Musician { get; set; }

        [Display(Name = "Instrument")]
        [Required(ErrorMessage = "You must enter a Instrument ID.")]
        public int InstrumentID { get; set; }
        public Instrument Instrument { get; set; }
    }
}
