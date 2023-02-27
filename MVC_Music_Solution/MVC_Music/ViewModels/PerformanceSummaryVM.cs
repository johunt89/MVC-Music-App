using System.ComponentModel.DataAnnotations;

namespace MVC_Music.ViewModels
{
    public class PerformanceSummaryVM
    {
        public int ID { get; set; }

        [Display(Name = "Musician")]
        public string FormalName
        {
            get
            {
                return FirstName
                    + (string.IsNullOrEmpty(MiddleName) ? " " :
                        (" " + (char?)MiddleName[0] + ". ").ToUpper())
                    + LastName;
            }
        }

        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }

        [Display(Name = "Average Fee Paid")]
        [DataType(DataType.Currency)]
        public double AverageFeePaid { get; set; }

        [Display(Name = "Highes Fee Paid")]
        [DataType(DataType.Currency)]
        public double HighestFeePaid { get; set; }

        [Display(Name = "Lowest Fee Paid")]
        [DataType(DataType.Currency)]
        public double LowestFeePaid { get; set; }

        [Display(Name = "Total Performances")]
        public double TotalPerformances { get; set; }
    }
}
