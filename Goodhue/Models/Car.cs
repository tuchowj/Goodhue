using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace Goodhue.Models
{
    public class Car
    {
        //unchanging
        //public int ID { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ID { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public string ImageURL { get; set; }

        //changing
        public int Odometer { get; set; }
        [Display(Name = "Miles to Oil Change")]
        public int OilChangeMiles { get; set; }
        [Display(Name = "Next Reservation")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime? NextReservation { get; set; }
        [Display(Name = "Next Reserved By")]
        public string NextUser { get; set; }
        [Display(Name = "Available?")]
        public bool IsAvailable { get; set; }

    }

    public class CarDBContext : DbContext
    {
        public DbSet<Car> Cars { get; set; }
    }
}