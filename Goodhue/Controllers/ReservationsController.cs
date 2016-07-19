using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Goodhue.Models;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Core.Objects;

namespace Goodhue.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private static int checkoutCarId;
        private static int oldOdometer;
        private static Reservation returnReservation;
        private ReservationDBContext db = new ReservationDBContext();
        private CarDBContext carDb = new CarDBContext();

        // GET: Reservations
        [Authorize (Roles="Admin")]
        public ActionResult Index()
        {
            ViewBag.Admin = Constants.ADMIN;
            return View(db.Reservations.ToList());
        }

        // GET: Reservations
        [AllowAnonymous]
        public ActionResult Schedule(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = carDb.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            ViewBag.Car = car;
            return View(db.Reservations.OrderBy(r=>r.StartDate).ToList());
        }

        // GET: Reservations/Details/5
        [AllowAnonymous]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // GET: Reservations/Create/2
        public ActionResult Create(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            checkoutCarId = (int)id;
            Car car = carDb.Cars.Find(id);
            if (car == null)
            {
                return HttpNotFound();
            }
            ViewBag.Car = car;
            ViewBag.HasScheduleConflict = false;
            return View();
        }

        // POST: Reservations/Create/2
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,StartDate,EndDate,Destination,Department,Miles,CarID,IsActive")] Reservation reservation, TimeSpan? startHour, TimeSpan? endHour)
        {
            Car car = carDb.Cars.Find(checkoutCarId);
            if (ModelState.IsValid)
            {
                if (!startHour.HasValue)
                {
                    startHour = TimeSpan.Zero;
                }
                if (!endHour.HasValue)
                {
                    endHour = TimeSpan.Zero;
                }
                reservation.StartDate = reservation.StartDate.Add((TimeSpan) startHour);
                reservation.EndDate = reservation.EndDate.Add((TimeSpan) endHour);
                if ((reservation.StartDate > reservation.EndDate) || (reservation.StartDate < DateTime.Today))
                {
                    ViewBag.Car = car;
                    ViewBag.HasScheduleConflict = true;
                    return View(reservation);
                }
                List<Reservation> reservations = db.Reservations.ToList();
                foreach (Reservation res in reservations)
                {
                    if (checkoutCarId == res.CarId && res.IsActive)
                    {
                        if ((reservation.StartDate >= res.StartDate && reservation.StartDate < res.EndDate) ||
                            (reservation.EndDate > res.StartDate && reservation.EndDate <= res.EndDate) ||
                            (reservation.StartDate <= res.StartDate && reservation.EndDate >= res.EndDate))
                        {
                            ViewBag.Car = car;
                            ViewBag.HasScheduleConflict = true;
                            return View(reservation);
                        }
                    }
                }
                reservation.CarId = checkoutCarId;
                if (User.IsInRole("Maintenance"))
                {
                    reservation.Username = "Maintenance";
                }
                else
                {
                    reservation.Username = User.Identity.Name;
                }
                reservation.IsActive = true;
                db.Reservations.Add(reservation);
                db.SaveChanges();
                return RedirectToAction("Index","Cars");
            }
            ViewBag.Car = car;
            ViewBag.HasScheduleConflict = false;
            return View(reservation);
        }

        //// GET: Reservations/Edit/5
        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Reservation reservation = db.Reservations.Find(id);
        //    if (reservation == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(reservation);
        //}

        //// POST: Reservations/Edit/5
        //// To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        //// more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "ID,StartDate,EndDate,Destination,Department")] Reservation reservation)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        db.Entry(reservation).State = EntityState.Modified;
        //        db.SaveChanges();
        //        return RedirectToAction("Index");
        //    }
        //    return View(reservation);
        //}

        // GET: Reservations/Return/5/2
        public ActionResult Return(int? carId, int? reservationId)
        {
            //Deactivate Reservation
            if (reservationId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = db.Reservations.Find(reservationId);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            //Only Admin can return other user's reservations
            if (!User.IsInRole("Admin") && User.Identity.Name != reservation.Username &&
                !(User.IsInRole("Maintenance") && reservation.Username == "Maintenance"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }
            returnReservation = reservation;

            //Edit Car Info
            if (carId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Car car = carDb.Cars.Find(carId);
            if (car == null)
            {
                return HttpNotFound();
            }
            oldOdometer = car.Odometer;
            return View(car);
        }

        // POST: Reservations/Return/5?carId=2
        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        public ActionResult ReturnConfirmed([Bind(Include = "ID,CountyID,Description,Location,Odometer,OilChangeMiles,isAvailable")] Car car)
        {
            //Deactivate Reservation
            returnReservation.IsActive = false;
            //reservation.Charge = Constants.GAS_PRICE * (oldOdometer - car.Odometer);
            returnReservation.Miles = car.Odometer - oldOdometer;
            db.Entry(returnReservation).State = EntityState.Modified;
            db.SaveChanges();

            //Edit Car Info
            if (ModelState.IsValid)
            {
                car.OilChangeMiles = car.OilChangeMiles + oldOdometer - car.Odometer;
                if (DateTime.Now < returnReservation.EndDate)
                {
                    car.LastReservation = DateTime.Now;
                }
                else
                {
                    car.LastReservation = returnReservation.EndDate;
                }
                carDb.Entry(car).State = EntityState.Modified;

                bool saveFailed;
                do
                {
                    saveFailed = false;
                    try
                    {
                        carDb.SaveChanges();
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        saveFailed = true;

                        //// Update original values from the database 
                        var objContext = ((IObjectContextAdapter)carDb).ObjectContext;
                        var entry = ex.Entries.Single();
                        //entry.Reload(); //***** DELETE THIS ********************************************
                        //entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        objContext.Refresh(RefreshMode.ClientWins, entry.Entity);
                    }

                } while (saveFailed);

                return RedirectToAction("Index","Cars");
            }
            return View(car);
        }

        // GET: Reservations/Delete/5
        [Authorize (Roles="Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            return View(reservation);
        }

        // POST: Reservations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Reservation reservation = db.Reservations.Find(id);
            db.Reservations.Remove(reservation);
            db.SaveChanges();
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
