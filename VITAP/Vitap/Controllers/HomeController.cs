using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.PegasysEntities;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Data.Models.Home;
using VITAP.Library.Strings;
using VITAP.Data.Models.Login;
using GSA.R7BD.Utility;

namespace VITAP.Controllers
{
    public class HomeController : VitapBaseController
    {
        // GET: Home
        public ActionResult Index()
        {
            try
            {
                var model = new HomeModel();
                // For testing, add roles here.
                //var roleList = new RoleListModel();
                //roleList.HasDataEntryRole = true;
                //roleList.HasExceptionsRole = true;
                //Session.Add(VITAP.Library.Strings.SessionKey.Roles, roleList);
                //Session[SessionKey.UserName] = "RONALDDSELE";
               
                var ClosureMsg = DataAccess.GetAppClosed("VITAP").ReplaceNull("").Replace("OPEN", "");
                var WarningMsg = "";
                if (String.IsNullOrWhiteSpace(ClosureMsg))
                {
                    WarningMsg = DataAccess.GetAppWarning("VITAP").ReplaceNull("").Replace("OPEN", "");
                }
                TempData["WarningMsg"] = WarningMsg;

                ViewBag.ErrorMessage = TempData["ErrorMessage"];            

                if (!String.IsNullOrWhiteSpace(ClosureMsg))
                {
                    TempData["ClosureMsg"] = ClosureMsg;
                    return View("Closure");
                }

                //var loginModel = Session["LoginModel"] as LoginModel;
                if (Session[SessionKey.UserName] == null)
                {
                    return RedirectToAction("Index", "Login");
                }
                else
                {
                    // Show the GSA warning?
                    if (Session[SessionKey.FirstTime] == null) 
                    {
                        ViewBag.CompanyHomeUrl = GSA.R7BD.Utility.Utilities.GetUrl("GSALOGO", "Vitap");
                        model.firstTime = true;
                        Session[SessionKey.FirstTime] = "yes";
                    }
                    else
                    {
                        model.firstTime = false;
                    }

                    return View("Index", model);
                }
            }
            catch (System.Exception ex)
            {
                return RedirectToAction("Index", "Login");
            }
        }


        public ActionResult Error()
        {

            ViewBag.Message = "An error has occurred in the application";

            return View("ViewErrorMessage");
        }

    }
}