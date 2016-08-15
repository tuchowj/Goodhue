using Goodhue.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;

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
            ViewBag.NextRes = getNextRes(car);
            ViewBag.Car = car;
            return View(db.Reservations.Where(r => r.IsActive && (r.CarId == car.ID)).OrderBy(r=>r.StartDate));
        }

        private Reservation getNextRes(Car car)
        {
            List<Reservation> reservations = db.Reservations.Where(r => (r.CarId == car.ID) && r.IsActive).ToList();
            if (reservations.Any())
            {
                //note: this operation's runtime can most likely be improved if necessary
                return reservations.OrderBy(r => r.StartDate).First();
            }
            else { return null; }
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
        public ActionResult Create(int? id, [Bind(Include = "ID,StartDate,EndDate,Destination,Department,Miles,TankFilled,CarID,IsActive")] Reservation reservation, string grant)
        {
            Car car = carDb.Cars.Find(id);
            ViewBag.Car = car;
            if (ModelState.IsValid)
            {
                if (reservation.StartDate > reservation.EndDate)
                {
                    ViewBag.Message = "Return Time must come after Checkout Time";
                    return View(reservation);
                }
                else if (reservation.EndDate < DateTime.Today)
                {
                    ViewBag.Message = "Cannot checkout car before today";
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
                if (!(grant == null || grant == "")) {
                    reservation.Department = reservation.Department + " -- " + grant;
                }
                db.Reservations.Add(reservation);
                db.SaveChanges();

                //send confirmation email
                MailMessage mail = new MailMessage();
                SmtpClient client = new SmtpClient();
                client.Port = 25;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Host = "mail.goodhue.county";
                mail.From = new MailAddress("carshare.donotreply@co.goodhue.mn.us");
                mail.To.Add(User.Identity.Name);

                mail.Subject = "Car Pool Comment";
                mail.Body = "You have successfully checked out the car \"" + car.Description +
                    " (" + car.ID + ") starting on " + reservation.StartDate + ". Please " +
                    "remember to keep track of the odometer when you are done.<br/><br/>" + 
                    "<b>You are expected to return this car by " + reservation.EndDate + ".</b>";
                mail.IsBodyHtml = true;
                client.Send(mail);

                return RedirectToAction("Schedule", new { id = car.ID });
            }
            ViewBag.Car = car;
            ViewBag.Message = null;
            return View(reservation);
        }

        // GET: Reservations/Return/5/2
        public ActionResult Return(int? carId, int? reservationId)
        {
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
            } else { //user cannot return any reservation but the next one
                return View(car);
            }
        }

        // POST: Reservations/Return/5/2
        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        public ActionResult ReturnConfirmed(int? carId, int? reservationId, [Bind(Include = "ID,Description,Location,ImageURL,Odometer,OilChangeMiles,IsAvailable")] Car car, bool tankFilled, string comment)
        {
            Reservation returnReservation = db.Reservations.Find(reservationId);

            //Edit Reservation Info
            returnReservation.IsActive = false;
            returnReservation.Miles = car.Odometer - oldOdometer;
            returnReservation.TankFilled = tankFilled;
            db.Entry(returnReservation).State = EntityState.Modified;
            db.SaveChanges();

            //Add comment if there's anything in textbox
            if (comment.Length > 0)
            {
                Comment userComment = new Comment();
                userComment.CarId = (int)carId;
                userComment.Username = User.Identity.Name;
                userComment.Text = comment;
                commentDb.Comments.Add(userComment);
                commentDb.SaveChanges();

                //send email
                MailMessage mail = new MailMessage();
                SmtpClient client = new SmtpClient();
                client.Port = 25;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                client.Host = "mail.goodhue.county";
                mail.From = new MailAddress("carshare.donotreply@co.goodhue.mn.us");
                ApplicationDbContext appDb = new ApplicationDbContext();
                var account = new AccountController();
                foreach (ApplicationUser user in appDb.Users)
                {
                    if (account.UserManager.GetRoles(user.Id).Contains("Admin"))
                    {
                        mail.To.Add(user.Email); //user is an admin
                    }
                }
                
                mail.Subject = "Car Pool Comment";
                mail.Body = User.Identity.Name + ": " + comment;
                client.Send(mail);
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
                return RedirectToAction("Schedule", new { id = carId });
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
            return RedirectToAction("Schedule", new { id = reservation.CarId });
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

        public ActionResult MyReservations()
        {
            bool isMaintenance = User.IsInRole("Maintenance");
            return View(db.Reservations.Where(r => r.IsActive && (r.Username == User.Identity.Name ||
                ((r.Username == "Maintenance") && isMaintenance))).OrderBy(r => r.EndDate).ToList());
        }

        public void WriteCSV()
        {
            StringWriter sw = new StringWriter();
            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment;filename=Reservations.csv");
            Response.ContentType = "text/csv";

            sw.WriteLine("Car_ID,User_Email,Checkout_Date,Return_Date,Destination,Department,Miles_Driven");
            IEnumerable<Reservation> inactiveReservations = db.Reservations.Where(r => !r.IsActive);
            foreach (Reservation res in inactiveReservations)
            {
                var id = res.CarId;
                var user = res.Username.Replace(",", ""); //strip commas to sanitize input
                var startDate = res.StartDate;
                var endDate = res.EndDate;
                var destination = res.Destination.Replace(",", ""); //
                var department = res.Department.Replace(",", ""); //
                var miles = res.Miles;
                sw.WriteLine(string.Format("{0},{1},{2},{3},{4},{5},{6}", id, user, startDate,
                    endDate, destination, department, miles));
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
