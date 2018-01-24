using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Library.Strings;

namespace VITAP.Controllers
{
    public class PreparerCodeProductivityController : VitapBaseController
    {

        [HttpGet]
        public ActionResult Index()
        {
            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            if (roleList.HasExceptionsRole == false)
            {
                RedirectToAction("Index", "Home");
            }

            var pcpModel = new PreparerCodeProductivityViewModel();

            pcpModel.SearchValue = PrepCode;
            

            return View(pcpModel);
        }

        [HttpPost]
        public ActionResult Index(PreparerCodeProductivityViewModel model)
        { 
            string clearDate = "";
            model.SearchValue = PrepCode;

            switch (model.Answer)
            {
                case "1":
                default:
                    model.Answer = "1";
                    clearDate = " not cleared_date is null  ";
                    break;
                case "2":
                    clearDate = " cleared_date >= sysdate";
                    break;
                case "3":
                    clearDate = " cleared_date >= (sysdate - 7) ";
                    break;
                case "4":
                    clearDate = " cleared_date >= (sysdate - 30) ";
                    break;
                case "5":
                    clearDate = " cleared_date >= to_date('" + model.From.ShortDate() + "','MM/DD/YYYY') ";
                    clearDate = clearDate + " and cleared_date  <= (to_date('" + model.To.ShortDate() + "', 'MM/DD/YYYY') + 1) ";
                    break;
            }

            model.PCProductivity = GetPCProductivity(clearDate, model.SearchValue);

            return (View(model));
        }

        private List<PreparerCodeProductivity> GetPCProductivity(string strClearDate, string strPrepCode)
        {
            var mgr = new ExceptionsManager();
            List<PreparerCodeProductivity> model = new List<PreparerCodeProductivity>();
            model = mgr.GetPrepCodeProductivity(AssignSrv, strClearDate, strPrepCode);
            return model;
        }        
    }
}