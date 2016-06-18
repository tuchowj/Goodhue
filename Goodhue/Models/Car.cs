using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace Goodhue.Models
{
    public class Car
    {
        //unchanging
        public int ID { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
        //TODO: mileage and date put in service?

        //changing
        public string location { get; set; }
        public int Odometer { get; set; }
        public int OilChangeMiles { get; set; }
        public DateTime LastReservation { get; set; }
        //TODO: comments?
    }

    public class CarDBContext : DbContext
    {
        public DbSet<Car> Cars { get; set; }
    }
}