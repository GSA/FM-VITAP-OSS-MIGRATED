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
    public class AssoProdController : VitapBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var roleModel = this.RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            var model = new AssoProdViewModel();
            model.Answer = "1";
            return View(new AssoProdViewModel());
        }


        [HttpPost]
        public ActionResult Index(AssoProdViewModel model)
        {  
            string clearDate = "";

            switch (model.Answer)
            {
                case "1":
                    clearDate = " not cleared_date is null  ";
                    break;
                case "2":
                    clearDate = " cleared_date >= trunc(sysdate)";
                    break;
                case "3":
                    clearDate = " cleared_date >= trunc(sysdate - 7) ";
                    break;
                case "4":
                    clearDate = " cleared_date >= trunc(sysdate - 30) ";
                    break;
                case "5":
                    clearDate = " cleared_date >= to_date('" + model.From.ShortDate() + "','MM/DD/YYYY') " ;
                    clearDate = clearDate + " and cleared_date  <= (to_date('" + model.To.ShortDate() + "', 'MM/DD/YYYY') + 1) ";
                    break;
                default:
                    clearDate = "";
                    break;
            }

            model.AssociateProd = GetAssociateProductivity(clearDate);

            return (View(model));
        }

        private List<AssociateProductivity> GetAssociateProductivity(string strClearDate)
        {
            var mgr = new ExceptionsManager();
            List<AssociateProductivity> model = new List<AssociateProductivity>();
            model = mgr.GetAssoProd(AssignSrv, strClearDate);
            return model;
        }
    }
}