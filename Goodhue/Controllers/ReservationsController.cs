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
using System.Net.Mail;
using System.Text;
using System.IO;

namespace Goodhue.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private static int oldOdometer;
        private ReservationDBContext db = new ReservationDBContext();
        private CarDBContext carDb = new CarDBContext();
        private CommentDBContext commentDb = new CommentDBContext();

        // GET: Reservations
        [Authorize (Roles="Admin")]
        public ActionResult Index()
        {
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
            List<Reservation> reservations = db.Reservations.Where(r => (r.CarId == car.ID) && r.IsActive).ToList();
            Reservation nextRes;
            if (reservations.Any())
            {
                //note: this operation's runtime can most likely be improved if necessary
                nextRes = reservations.OrderBy(r => r.StartDate).First();
            }
            else { nextRes = null; }

            ViewBag.NextRes = nextRes;
            ViewBag.Car = car;

            
            return View(db.Reservations.Where(r => r.IsActive && (r.CarId == car.ID)).OrderBy(r=>r.StartDate));
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
            //checkoutCarId = (int)id;
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
        public ActionResult Create(int? id, [Bind(Include = "ID,StartDate,EndDate,Destination,Department,Miles,TankFilled,CarID,IsActive")] Reservation reservation, TimeSpan? startHour, TimeSpan? endHour)
        {
            Car car = carDb.Cars.Find(id);
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
                    ViewBag.Message = "Invalid input";
                    return View(reservation);
                }
                List<Reservation> reservations = db.Reservations.Where(r => r.CarId == car.ID).Where(r => r.IsActive).ToList();
                foreach (Reservation res in reservations)
                {
                    //check for double booking
                    if ((reservation.StartDate >= res.StartDate && reservation.StartDate < res.EndDate) ||
                        (reservation.EndDate > res.StartDate && reservation.EndDate <= res.EndDate) ||
                        (reservation.StartDate <= res.StartDate && reservation.EndDate >= res.EndDate))
                    {
                        ViewBag.Car = car;
                        ViewBag.Message = "Conflicts with an existing reservation starting at " + res.StartDate.ToString() + " and ending at " + res.EndDate.ToString();
                        return View(reservation);
                    }
                }
                reservation.CarId = car.ID;
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
            ViewBag.Message = null;
            return View(reservation);
        }

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
            IEnumerable<Reservation> unreturnedReservations = db.Reservations.Where(r => (r.IsActive && (r.CarId == carId) && (r.EndDate <= reservation.StartDate)));
            if (unreturnedReservations.Any()) {
                ViewBag.Car = car;
                return RedirectToAction("Schedule", new { id = carId });
            } else {
                return View(car);
            }
        }

        // POST: Reservations/Return/5/2
        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        public ActionResult ReturnConfirmed(int? carId, int? reservationId, [Bind(Include = "ID,CountyID,Description,Location,ImageURL,Odometer,OilChangeMiles,IsAvailable")] Car car, bool tankFilled, string comment)
        {
            Reservation returnReservation = db.Reservations.Find(reservationId);

            //Deactivate Reservation
            returnReservation.IsActive = false;
            returnReservation.Miles = car.Odometer - oldOdometer;
            returnReservation.TankFilled = tankFilled;
            db.Entry(returnReservation).State = EntityState.Modified;
            db.SaveChanges();

            ////send email
            //MailMessage mail = new MailMessage();

            //SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            //smtpServer.Credentials = new System.Net.NetworkCredential("jonahg@redwingignite.org", "lolcats1");
            //smtpServer.Port = 587; // Gmail works on this port

            //mail.From = new MailAddress("jonahg@redwingignite.org");
            //mail.To.Add("tuchowj@gmail.com");
            //mail.Subject = "Comment";
            //mail.Body = comment;

            //smtpServer.Send(mail);

            //Add comment if there's anything in textbox
            if (comment.Length > 0)
            {
                Comment userComment = new Comment();
                userComment.CarId = (int)carId;
                userComment.Username = User.Identity.Name;
                userComment.Text = comment;
                commentDb.Comments.Add(userComment);
                commentDb.SaveChanges();
            }

            //Edit Car Info
            if (ModelState.IsValid)
            {
                car.OilChangeMiles = car.OilChangeMiles + oldOdometer - car.Odometer;
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

                        // Update original values from the database 
                        var objContext = ((IObjectContextAdapter)carDb).ObjectContext;
                        var entry = ex.Entries.Single();
                        objContext.Refresh(RefreshMode.ClientWins, entry.Entity);
                    }

                } while (saveFailed);

                return RedirectToAction("Index","Cars");
            }

            return View(car);
        }

        // GET: Reservations/Delete/5
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
            //Only Admin can return other user's reservations
            if (!User.IsInRole("Admin") && User.Identity.Name != reservation.Username &&
                !(User.IsInRole("Maintenance") && reservation.Username == "Maintenance"))
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
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

        // GET: Reservations/DeleteAll
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteAll()
        {
            return View();
        }

        // POST: Reservations/DeleteAll
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("DeleteAll")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed()
        {
            IEnumerable<Reservation> badReservations = db.Reservations.Where(r => !r.IsActive);
            foreach (Reservation reservation in badReservations)
            {
                db.Reservations.Remove(reservation);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public void WriteCSV()
        {
            StringWriter sw = new StringWriter();
            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment;filename=Reservations.csv");
            Response.ContentType = "text/csv";

            IEnumerable<Reservation> inactiveReservations = db.Reservations.Where(r => !r.IsActive);
            foreach (Reservation res in inactiveReservations)
            {
                var startDate = res.StartDate;
                var endDate = res.EndDate;
                var destination = res.Destination;
                var department = res.Department;
                var miles = res.Miles;

                sw.WriteLine(string.Format("{0},{1},{2},{3},{4}", startDate, endDate,
                    destination, department, miles));
            }
            Response.Write(sw.ToString());
            Response.End();
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
