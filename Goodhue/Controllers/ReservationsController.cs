﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Goodhue.Models;
using System.Data.Entity.Infrastructure;

namespace Goodhue.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private static int checkoutCarId;
        private static int oldOdometer;
        private ReservationDBContext db = new ReservationDBContext();
        private CarDBContext carDb = new CarDBContext();

        // GET: Reservations
        [AllowAnonymous]
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
            return View(db.Reservations.ToList());
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

        // GET: Reservations/Create
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
            return View();
        }

        // POST: Reservations/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,StartDate,EndDate,Destination,Department")] Reservation reservation)
        {
            if (ModelState.IsValid)
            {
                reservation.CarId = checkoutCarId;
                reservation.Username = User.Identity.Name;
                reservation.IsActive = true;
                db.Reservations.Add(reservation);
                db.SaveChanges();
                //Car car = carDb.Cars.Find(checkoutCarId);
                //ViewBag.Car = car;
                return RedirectToAction("Index");
            }

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

        // GET: Reservations/Return/5?carID=2
        public ActionResult Return(int? id, int? carId)
        {
            //Deactivate Reservation
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Reservation reservation = db.Reservations.Find(id);
            if (reservation == null)
            {
                return HttpNotFound();
            }
            if (User.Identity.Name != Constants.ADMIN && User.Identity.Name != reservation.Username)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
            }

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
        public ActionResult ReturnConfirmed(int id, [Bind(Include = "ID,Make,Model,Color,Year,Location,Odometer,OilChangeMiles,LastReservation")] Car car)
        {
            if (ModelState.IsValid)
            {
                //Deactivate Reservation
                Reservation reservation = db.Reservations.Find(id);

                reservation.IsActive = false;
                db.Entry(reservation).State = EntityState.Modified;
                db.SaveChanges();

                //Edit Car Info
                car.OilChangeMiles = car.OilChangeMiles + oldOdometer - car.Odometer;
                if (DateTime.Now < reservation.EndDate)
                {
                    car.LastReservation = DateTime.Now;
                }
                else
                {
                    car.LastReservation = reservation.EndDate;
                }                
                carDb.Entry(car).State = EntityState.Modified;
                carDb.SaveChanges();

                return RedirectToAction("Index");
            }
            return View(car);
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
