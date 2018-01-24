using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using System.Reflection;

namespace VITAP.Controllers
{
    public class PaymentsController : VitapBaseController
    {
        // GET: Payments
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string ACT)
        {
            UPPSWIPManager uppsWIPManager = new UPPSWIPManager();

            List<UPPSWIP> uwip = new List<UPPSWIP>();

            if (ACT.Equals("All"))
            {
                uwip = uppsWIPManager.GeneralSearch("ZZZZZZZZ").ToList();
            }
            else if (ACT.Equals("Matched"))
            {
                uwip = uppsWIPManager.GeneralSearch("").ToList();
            }
            else if (ACT.Equals("UnMatched"))
            {
                uwip = uppsWIPManager.GeneralSearch("").ToList();
            }

            return View("Results", uwip);
        }


        [HttpPost, ActionName("Results"), AcceptParameter(Name = "Search")]
        public ActionResult Results(string ACT, string invoice, string vendno)
        {
            UPPSWIPManager uppsWIPManager = new UPPSWIPManager();

            List<UPPSWIP> uwip = new List<UPPSWIP>();
            //UPPSWIP uwip = new UPPSWIP();

            uwip = uppsWIPManager.UPPSWIPSearch(ACT, invoice, vendno).ToList();

            return View("Results");
        }

        [HttpPost, ActionName("Results"), AcceptParameter(Name = "Image")]
        public ActionResult Results(string imgID)
        {
            UPPSWIPManager uppsWIPManager = new UPPSWIPManager();

            //List<UPPSWIP> uwip = new List<UPPSWIP>();


            //uwip = uppsWIPManager.ViewImage(imgID);

            return View("Results");
        }

        [HttpPost, ActionName("Results"), AcceptParameter(Name = "Save")]
        public ActionResult Results()
        {
            UPPSWIPManager uppsWIPManager = new UPPSWIPManager();

            List<UPPSWIP> uwip = new List<UPPSWIP>();


            //bool flag = uppsWIPManager.SaveToFile();

            return View("Results");
        }

        public class AcceptParameter : ActionMethodSelectorAttribute
        {
            public string Name { get; set; }
            //public string Value { get; set; }

            public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
            {
                //var req = controllerContext.RequestContext.HttpContext.Request;
                //return req.Form[this.Name] == this.Value;

                var value = controllerContext.Controller.ValueProvider.GetValue(Name);
                if (value != null)
                {
                    return true;
                }
                return false;
            }
        }
    }
}