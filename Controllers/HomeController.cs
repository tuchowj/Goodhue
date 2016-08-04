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
            return RedirectToAction("Index", "Cars");
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }
    }
}