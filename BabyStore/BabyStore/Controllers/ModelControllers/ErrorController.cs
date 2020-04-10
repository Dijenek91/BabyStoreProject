using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BabyStore.Controllers.ModelControllers
{
    public class ErrorController : Controller
    {
        public ActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            return View();
        }

        public ActionResult InternalServerError()
        {
            Response.StatusCode = 500;
            return View();
        }

        // GET: Error
        public ActionResult FileUploadLimitExceeded()
        {
            return View();
        }
    }
}