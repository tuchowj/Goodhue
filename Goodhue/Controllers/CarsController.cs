using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Goodhue.Models;

namespace Goodhue.Controllers
{
    [Authorize (Roles="Admin")]
    public class CarsController : Controller
    {
        private CarDBContext db = new CarDBContext();
        private ReservationDBContext reservationDb = new ReservationDBContext();

        // GET: Cars
        [AllowAnonymous]
        public ActionResult Index()
        {
            List<Car> cars = db.Cars.ToList();
            foreach (Car car in cars) {
                //list of active reservations for each car
                List<Reservation> reservations = reservationDb.Reservations.Where(r => r.CarId == car.ID).Where(r => r.IsActive).Where(r => r.EndDate > DateTime.Now).ToList();
                if (reservations.Count >= 1)
                {
                    //note: this operation's runtime can most likely be improved if necessary
                    Reservation nextRes = reservations.OrderBy(r => r.StartDate).First();
                    car.NextReservation = nextRes.StartDate;
                    car.NextUser = nextRes.Username;
                }
            }
            return View(cars);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(DateTime? startDate, DateTime? endDate)
        {
            if (startDate == null || endDate == null)
            {
                return RedirectToAction("Index");
            }
            List<Car> availableCars = db.Cars.ToList();
            List<Reservation> reservations = reservationDb.Reservations.ToList();
            foreach (Reservation res in reservations)
            {
                if (res.IsActive)
                {
                    if ((startDate >= res.StartDate && startDate < res.EndDate) ||
                        (endDate > res.StartDate && endDate <= res.EndDate) ||
                        (startDate <= res.StartDate && endDate >= res.EndDate))
                    {
                        Car badCar = db.Cars.Find(res.CarId);
                        availableCars.Remove(badCar);
                    }
                }
            }
            return View(availableCars);
        }

        // GET: Cars/Details/5
        [AllowAnonymous]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = db.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            return View(car);
        }

        // GET: Cars/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Cars/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,CountyID,Description,Location,Odometer,OilChangeMiles")] Car car)
        {
            if (ModelState.IsValid)
            {
                car.NextReservation = null;
                car.IsAvailable = true;
                db.Cars.Add(car);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(car);
        }

        // GET: Cars/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = db.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            return View(car);
        }

        // POST: Cars/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,CountyID,Description,Location,Odometer,OilChangeMiles,NextReservation,NextUser,IsAvailable")] Car car)
        {
            if (ModelState.IsValid)
            {
                db.Entry(car).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(car);
        }

        // GET: Cars/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
          
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = db.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            //Delete Car
            Car car = db.Cars.Find(id);
            db.Cars.Remove(car);
            db.SaveChanges();

            //Delete Associated Reservations
            List<Reservation> reservations = reservationDb.Reservations.ToList();
            foreach (Reservation reservation in reservations)
            {
                if (reservation.CarId == id && reservation.IsActive)
                {
                    reservationDb.Reservations.Remove(reservation);
                }
            }
            reservationDb.SaveChanges();
            
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
