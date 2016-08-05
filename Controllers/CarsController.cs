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
        private CommentDBContext commentDb = new CommentDBContext();

        // GET: Cars
        [AllowAnonymous]
        public ActionResult Index()
        {
            List<Car> cars = db.Cars.ToList();
            setNextReservations(cars);
            return View(cars.OrderBy(c => c.ID));
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Index(int? duration, DateTime? startDate)
        {
            if (startDate == null)
            {
                return RedirectToAction("Index");
            }
            //if (duration == null)
            //{
            //    duration = 0;
            //}
            TimeSpan startTime = TimeSpan.Zero;

            if (duration == -12)
            {
                startTime = new TimeSpan(12,0,0);
                duration = 12;
            }
            int dur = (int)duration;
            
            DateTime date = (DateTime)startDate;
            startDate = date.Add(startTime);
            DateTime? endDate = date.Add(startTime).AddHours(dur);

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
            setNextReservations(availableCars);
            return View(availableCars.OrderBy(c => c.ID));
        }

        private void setNextReservations(List<Car> cars)
        {
            foreach (Car car in cars)
            {
                //list of active reservations for each car
                List<Reservation> reservations = reservationDb.Reservations.Where(r => (r.CarId == car.ID) && r.IsActive).ToList();
                if (reservations.Any())
                {
                    //note: this operation's runtime can most likely be improved if necessary
                    Reservation nextRes = reservations.OrderBy(r => r.StartDate).First();
                    car.NextReservation = nextRes.StartDate;
                    car.NextUser = nextRes.Username;
                }
            }
            db.SaveChanges();
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
        public ActionResult Create([Bind(Include = "ID,Description,Location,ImageURL,Odometer,OilChangeMiles")] Car car)
        {
            bool hasUniqueID = !db.Cars.Where(c => c.ID == car.ID).ToList().Any();
            if (ModelState.IsValid && hasUniqueID)
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
        public ActionResult Edit([Bind(Include = "ID,Description,Location,ImageURL,Odometer,OilChangeMiles,NextReservation,NextUser,IsAvailable")] Car car)
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
            
            return RedirectToAction("Index", "Cars");
        }

        // GET: Cars/Return
        [AllowAnonymous]
        public ActionResult Return() {
            IEnumerable<Car> cars = db.Cars;
            IEnumerable<Reservation> reservations = reservationDb.Reservations;
            List<Reservation> nextReservations = new List<Reservation>();
            foreach (Car car in cars)
            {
                List<Reservation> resList = reservations.Where(r => r.IsActive && r.CarId == car.ID).ToList();
                nextReservations.Add(resList.OrderBy(r => r.EndDate).First());
            }
            return View(nextReservations.OrderBy(r => r.EndDate));
        }

        // GET: Cars/Comments/5
        public ActionResult Comments(int? id)
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
            ViewBag.Car = car;
            return View(commentDb.Comments.Where(c => c.CarId == id));
        }

        // GET: Cars/DeleteComment/5
        public ActionResult DeleteComments(int? id)
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
            return View();
        }

        // POST: Cars/DeleteComments/5
        [HttpPost, ActionName("DeleteComments")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCommentsConfirmed(int id)
        {
            IEnumerable<Comment> comments = commentDb.Comments.Where(c => c.CarId == id);
            foreach (Comment comment in comments)
            {
                commentDb.Comments.Remove(comment);
            }
            commentDb.SaveChanges();
            return RedirectToAction("Comments/" + id);
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
