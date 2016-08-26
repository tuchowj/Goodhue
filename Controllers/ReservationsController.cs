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
        private DepartmentDBContext departmentDb = new DepartmentDBContext();

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
            ViewBag.Error = TempData["Error"];
            return View(db.Reservations.Where(r => r.IsActive && (r.CarId == car.ID)).OrderBy(r=>r.StartDate));
        }

        // GET: Reservations/Create/2
        public ActionResult Create(int? id, DateTime start, DateTime end)
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
            ViewBag.Start = start;
            ViewBag.End = end;
            return View();
        }

        // POST: Reservations/Create/2
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int? id, DateTime start, DateTime end, [Bind(Include = "ID,StartDate,EndDate,Destination,Department,Miles,TankFilled,CarID,IsActive")] Reservation reservation, string grant)
        {
            Car car = carDb.Cars.Find(id);
            ViewBag.Car = car;
            ViewBag.Start = start;
            ViewBag.End = end;
            if (ModelState.IsValid)
            {
                reservation.StartDate = start;
                reservation.EndDate = end;
                List<Reservation> reservations = db.Reservations.Where(r => r.CarId == car.ID).Where(r => r.IsActive).ToList();
                foreach (Reservation res in reservations)
                {
                    //check for double booking
                    if ((reservation.StartDate >= res.StartDate && reservation.StartDate < res.EndDate) ||
                        (reservation.EndDate > res.StartDate && reservation.EndDate <= res.EndDate) ||
                        (reservation.StartDate <= res.StartDate && reservation.EndDate >= res.EndDate))
                    {
                        ViewBag.Error = "Schedule Conflict -- Someone was probably in the process of reserving this car at the same time as you!";
                        return View(reservation);
                    }
                }
                reservation.CarId = car.ID;
                reservation.Username = User.Identity.Name;
                reservation.IsActive = true;
                if (!(grant == null || grant == "")) { //append grant info to dept field
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
                mail.Subject = "Pool Car Reservation Confirmation";
                mail.Body = "You have successfully reserved car \"" + car.Description +
                    " (" + car.ID + ")\" starting on " + reservation.StartDate + ".<br/><br/>" +
                    "<b>You are expected to return this car by " + reservation.EndDate +
                    ".<br/><br/>When you return the car, you must enter the odometer reading " +
                    "on the Return Your Car screen. Your ending odometer reading becomes the " +
                    "beginning odometer reading for the next reservation.</b><br/>";
                mail.IsBodyHtml = true;
                client.Send(mail);

                return RedirectToAction("Schedule", new { id = car.ID });
            }
            ViewBag.Error = "Invalid Model State";
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
            if (!User.IsInRole("Admin") && User.Identity.Name != reservation.Username)
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
            if (car.Odometer < oldOdometer)
            {
                ViewBag.Error = "Odometer cannot be less than before. If the old odometer is incorrect, contact an administrator.";
                return View(car);
            }

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

                //send comment email
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
                    if (account.UserManager.GetRoles(user.Id).Contains("Emailed_Comments"))
                    {
                        mail.To.Add(user.Email);
                    }
                }
                mail.Subject = "Car Pool Comment";
                mail.Body = "<b>" + car.Description + " (" + car.ID + ")</b><br/><br/>" +
                    User.Identity.Name + ": " + comment;
                mail.IsBodyHtml = true;
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

                if (car.OilChangeMiles < 300)
                {
                    //send oil change email
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
                        if (account.UserManager.GetRoles(user.Id).Contains("Emailed_Comments"))
                        {
                            mail.To.Add(user.Email);
                        }
                    }
                    mail.Subject = "Oil Change Needed Soon";
                    mail.Body = "The car \"" + car.Description + " (" + car.ID +
                        ")\" will need an oil change in " + car.OilChangeMiles + " miles.";
                    //mail.IsBodyHtml = true;
                    client.Send(mail);
                }

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
            if (!User.IsInRole("Admin") && User.Identity.Name != reservation.Username)
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

        // GET: Reservations/MyReservations
        public ActionResult MyReservations()
        {
            IEnumerable<Reservation> myReservations = db.Reservations.Where(r => r.IsActive &&
                (r.Username == User.Identity.Name)).OrderBy(r => r.EndDate);
            List<Reservation> nextReservations = new List<Reservation>();
            foreach (Car car in carDb.Cars) {
                Reservation nextRes = getNextRes(car);
                if (nextRes != null) {
                    nextReservations.Add(nextRes);
                }
            }
            ViewBag.NextReservations = nextReservations;
            return View(myReservations);
        }

        // GET: Reservations/Conflicts
        [Authorize (Roles="Admin")]
        public ActionResult Conflicts()
        {
            IEnumerable<Reservation> conflicts = db.Reservations.Where(r => r.IsActive && r.EndDate < DateTime.Now).OrderBy(r => r.EndDate);
            List<Reservation> nextReservations = new List<Reservation>();
            foreach (Car car in carDb.Cars)
            {
                Reservation nextRes = getNextRes(car);
                if (nextRes != null)
                {
                    nextReservations.Add(nextRes);
                }
            }
            ViewBag.NextReservations = nextReservations;
            return View(conflicts);
        }

        // GET: Reservations/Day
        [AllowAnonymous]
        public ActionResult Day()
        {
            IEnumerable<Reservation> activeReservations = db.Reservations.Where(r => r.IsActive);
            ViewBag.Day = "All Days (No Date Specified)";
            return View(activeReservations.ToList());
        }

        // POST: Reservations/Day
        [HttpPost, ActionName("Day")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult DayChanged(DateTime? date)
        {
            if (date == null)
            {
                return RedirectToAction("Day");
            }
            DateTime day = (DateTime)date;
            IEnumerable<Reservation> activeReservations = db.Reservations.Where(r => r.IsActive);
            ViewBag.Day = day.ToShortDateString();
            return View(activeReservations.Where(r => r.StartDate < day.AddDays(1) &&
                r.EndDate > day).ToList());
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

        public ActionResult AddRes(int? carId, DateTime startDate, DateTime endDate)
        {
            if (endDate < startDate)
            {
                TempData["Error"] = "Return time must be after checkout time";
                return RedirectToAction("Schedule", new { id = carId });
            }

            if (startDate < DateTime.Today)
            {
                TempData["Error"] = "Cannot reserve car before today";
                return RedirectToAction("Schedule", new { id = carId });
            }

            List<Reservation> reservations = db.Reservations.Where(r => r.IsActive && r.CarId == carId).ToList();
            foreach (Reservation res in reservations)
            {
                if ((startDate >= res.StartDate && startDate < res.EndDate) ||
                    (endDate > res.StartDate && endDate <= res.EndDate) ||
                    (startDate <= res.StartDate && endDate >= res.EndDate))
                {
                    TempData["Error"] = "Conflicts with reservation starting on " + 
                        res.StartDate + " and ending on " + res.EndDate;
                    return RedirectToAction("Schedule", new { id = carId });
                }
            }
            return RedirectToAction("Create", new { id = carId, start = startDate, end = endDate });
        }

        public PartialViewResult ShowDepartments()
        {
            return PartialView("Department",departmentDb.Departments.ToList().OrderBy(d => d.Name));
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
