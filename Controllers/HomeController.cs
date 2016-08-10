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
        private CarDBContext carDb = new CarDBContext();
        private ReservationDBContext reservationDb = new ReservationDBContext();

        public ActionResult Index()
        {
            return View();
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