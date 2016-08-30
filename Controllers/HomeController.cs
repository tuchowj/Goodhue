using Goodhue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Goodhue.Controllers
{
    public class HomeController : Controller
    {
        private CarDBContext carDb = new CarDBContext();
        private ReservationDBContext reservationDb = new ReservationDBContext();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Help()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        //
        // GET: Home/ReturnNext
        [Authorize]
        public ActionResult ReturnNext()
        {
            IEnumerable<Reservation> reservations = reservationDb.Reservations;
            bool isMaintenance = User.IsInRole("Maintenance");
            IEnumerable<Reservation> myReservations = reservations.Where(r => r.IsActive &&
                (r.Username == User.Identity.Name || ((r.Username == "Maintenance") && isMaintenance)));
            if (!myReservations.Any())
            {
                ViewBag.UnreturnedRes = "You have no reservations.";
                return View("Index");
            }
            Reservation nextRes = myReservations.OrderBy(r => r.EndDate).First();
            IEnumerable<Reservation> carReservations =
                reservations.Where(r => r.IsActive && r.CarId == nextRes.CarId);
            Reservation nextCarRes = carReservations.OrderBy(r => r.EndDate).First();
            if (nextRes.Equals(nextCarRes))
            {
                return RedirectToAction("Return", "Reservations",
                    new { carId = nextRes.CarId, reservationId = nextRes.ID });
            }
            else
            {
                Car car = carDb.Cars.Find(nextCarRes.CarId);
                ViewBag.UnreturnedRes = "Your car \"" + car.Description + " (" + car.ID +
                    ")\"" + " has not yet been returned by " + nextCarRes.Username;
                return View("Index");
            }
        }

        // GET: Cars/FindCar
        [Authorize]
        public ActionResult FindCar(DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
            {
                ViewBag.Error = "Return date/time "+ endDate + " is before start date/time " + startDate + 
                    ". Please re-enter reservation times.";
                return View("Index");
            }

            if (startDate < DateTime.Today)
            {
                ViewBag.Error = "Cannot reserve car before today";
                return View("Index");
            }

            List<Car> availableCars = carDb.Cars.Where(c => c.IsAvailable).ToList();
            List<Reservation> reservations = reservationDb.Reservations.Where(r => r.IsActive).ToList();
            foreach (Reservation res in reservations)
            {
                if ((startDate >= res.StartDate && startDate < res.EndDate) ||
                    (endDate > res.StartDate && endDate <= res.EndDate) ||
                    (startDate <= res.StartDate && endDate >= res.EndDate))
                {
                    Car badCar = carDb.Cars.Find(res.CarId);
                    availableCars.Remove(badCar);
                }
            }
            if (!availableCars.Any())
            {
                ViewBag.Error = "No cars available at that time";
                return View("Index");
            }
            ViewBag.Start = startDate;
            ViewBag.End = endDate;
            return View("FindCar", availableCars.OrderBy(c => c.ID));
        }
    }
}