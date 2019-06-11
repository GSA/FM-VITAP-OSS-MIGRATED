using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using System.Reflection;
using VITAP.Library.Strings;
using System.Globalization;

namespace VITAP.Controllers
{
    public class ExceptionClearedController : VitapBaseController
    {

        [HttpGet]
        public ActionResult Index()
        {
            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            if (roleList.HasExceptionsRole == false)
            {
                RedirectToAction("Index", "Home");
            }

            var model = new ExceptionClearedViewModel();
            model.SelectedAssignedService = AssignSrv;

            return View(model);
        }

        [HttpPost]
        public ActionResult Index( ExceptionClearedViewModel model)
        {

            string extrasel ="";

            switch (model.SearchCriteria)
            {
                case "All":
                    extrasel = " ";
                    break;
                case "PrepCode":
                    model.ChkBox = "false";
                    extrasel = " and PREPCODE = '" + model.SearchValue + "' ";
                    break;
                case "ErrCode":
                    extrasel = " and ERR_CODE = '" + model.SearchValue + "' ";
                    break;
                default:
                    extrasel = " ";
                    break;
            }

            if (model.ChkBox != null && model.ChkBox.Equals("false"))
            {
                extrasel = extrasel + " and PREPCODE != 'VI' ";
            }

            // Default to today (if no dates were entered)
            // or a single-day range if only one date was entered.
            if (model.From == null)
            {
                if (model.To == null)
                {
                    // Default to today.
                    model.From = DateTime.Today.ToString("MM/dd/yyyy");
                }
                else model.From = model.To;

                ModelState["From"].Value = new ValueProviderResult(model.From, "", CultureInfo.InvariantCulture);
            }
            if (model.To == null)
            {
                model.To = model.From;
                ModelState["To"].Value = new ValueProviderResult(model.To, "", CultureInfo.InvariantCulture);
            }

            // Legacy adds one day to the To date.
            String nextDay = DateTime.Parse(model.To).AddDays(1).ToString("MM/dd/yyyy");
            model.ExceptCleared = GetExceptionCleared(extrasel, model.From, nextDay, model.ChkBox, model.SelectedAssignedService);

            return (View(model));
        }

        private List<ExceptionCleared> GetExceptionCleared(string strExtrasel, string strFrom, string strTo, string strCheck, string assignedService)
        {
            var mgr = new ExceptionsManager();
            List<ExceptionCleared> model = new List<ExceptionCleared>();
            model = mgr.SearchExceptClearedRpt(assignedService, strExtrasel, strFrom, strTo, strCheck);
            return model;
        }
    }
}