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

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        // GET: Home/ReturnNext
        [AllowAnonymous]
        public ActionResult ReturnNext()
        {
            IEnumerable<Reservation> reservations = reservationDb.Reservations;
            bool isMaintenance = User.IsInRole("Maintenance");
            IEnumerable<Reservation> myReservations = reservations.Where(r => r.IsActive &&
                (r.Username == User.Identity.Name || ((r.Username == "Maintenance") && isMaintenance)));
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

            //IEnumerable<Car> cars = db.Cars;
            //IEnumerable<Reservation> reservations = reservationDb.Reservations;
            //List<Reservation> nextReservations = new List<Reservation>();
            //foreach (Car car in cars)
            //{
            //    List<Reservation> resList = reservations.Where(r => r.IsActive && r.CarId == car.ID).ToList();
            //    if ((resList).Any())
            //    {
            //        nextReservations.Add(resList.OrderBy(r => r.EndDate).First());
            //    }
            //}
            //return View(nextReservations.OrderBy(r => r.EndDate));
        }
    }
}