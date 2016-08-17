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
            setNextReservations();
            List<Car> cars = db.Cars.ToList();
            return View(cars.OrderBy(c => c.ID));
        }

        private void setNextReservations()
        {
            foreach (Car car in db.Cars)
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
                else  //in case a reservation was deleted
                {
                    car.NextReservation = null;
                    car.NextUser = null;
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

        // GET: Cars/Comments/5
        [Authorize(Roles="Admin,Maintenance")]
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
        [Authorize(Roles = "Admin,Maintenance")]
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
        [Authorize(Roles = "Admin,Maintenance")]
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
