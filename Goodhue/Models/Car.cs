using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace Goodhue.Models
{
    public class Car
    {
        public Car()
        {
            Reservations = new List<Reservation>();
        }

        //unchanging
        public int ID { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
        //TODO: mileage and date put in service?

        //changing
        public string Location { get; set; }
        public int Odometer { get; set; }
        [Display(Name = "Miles to Oil Change")]
        public int OilChangeMiles { get; set; }
        [Display(Name = "Last Reservation")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime LastReservation { get; set; }
        //TODO: tire rotation, comments, availability?

        public virtual ICollection<Reservation> Reservations { get; set; }
    }

    public class CarDBContext : DbContext
    {
        public DbSet<Car> Cars { get; set; }
    }
}