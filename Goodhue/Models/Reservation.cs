using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Goodhue.Models
{
    public class Reservation
    {
        public Reservation() { }

        public int ID { get; set; }
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:d}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; }
        public string Destination { get; set; }
        public string Department { get; set; }

        public virtual Car Car { get; set; }
    }
    public class ReservationDBContext : DbContext
    {
        public DbSet<Reservation> Reservations { get; set; }
    }
}