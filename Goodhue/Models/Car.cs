using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace Goodhue.Models
{
    public class Car
    {
        //public Car()
        //{
        //    Reservations = new List<Reservation>();
        //}

        //unchanging
        public int ID { get; set; }
        [Display(Name = "ID")]
        public int CountyID { get; set; }
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
        //TODO: comments, availability?

        //public virtual ICollection<Reservation> Reservations { get; set; }
    }

    public class CarDBContext : DbContext
    {
        public DbSet<Car> Cars { get; set; }
    }
}