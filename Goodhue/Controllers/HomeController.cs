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
        private CarDBContext db = new CarDBContext();

        public ActionResult Index()
        {
            ViewBag.Admin = "jonahg@redwingignite.org";
            return View(db.Cars.ToList());
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}