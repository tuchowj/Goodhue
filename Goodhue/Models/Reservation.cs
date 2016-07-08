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
        //public Reservation() { }

        public int ID { get; set; }
        public string Username { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime StartDate { get; set; }
        
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:g}", ApplyFormatInEditMode = true)]
        public DateTime EndDate { get; set; }

        [Required]
        public string Destination { get; set; }

        [Required]
        public string Department { get; set; }

        [DataType(DataType.Currency)]
        public double Charge { get; set; }

        public int CarId { get; set; }

        public bool IsActive { get; set; }
    }
    public class ReservationDBContext : DbContext
    {
        public DbSet<Reservation> Reservations { get; set; }
    }
}