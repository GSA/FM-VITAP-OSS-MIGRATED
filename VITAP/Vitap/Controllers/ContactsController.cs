using System.Web.Mvc;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using System.Linq;
using System.Collections.Generic;
using VITAP.Data;
using System;
using VITAP.SharedLogic;
using VITAP.Data.PegasysEntities;
using VITAP.Data.VitapEntities;
using VITAP.Library.Strings;

namespace VITAP.Controllers
{
    public class ContactsController : VitapBaseController
    {
        public ActionResult Edit(string openOption, string act = null, string exceptionId = null, string errorCode = null, string assignedService = null)
        {

            if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. Redirect to the login page.
                return RedirectToAction("Index", "Login");
            }

            var model = new ContactViewModel();
            model.OpenOption = string.IsNullOrEmpty(openOption) ? "menu" : openOption;
            model.PoAct = act;
            model.ExceptionId = exceptionId;
            model.ErrorCode = errorCode;

            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            if (roleList.ASSIGN_SRV != null)
            {
                assignedService = roleList.ASSIGN_SRV;
            }

            if (string.IsNullOrEmpty(assignedService) ||
                (!roleList.HasApacctRole && !roleList.HasExceptionsRole))
            {
                return RedirectToAction("index", "home");
            }

            model.AssignedService = assignedService;
            BindModel(model);

            return View("Edit", model);
        }

        [HttpPost]
        public ActionResult Edit(ContactViewModel model)
        {
            var manager = new ContactManager();

            if (model.Command == "viewpo")
            {
                return RedirectToAction("ViewPO", "TransHist", new { po_Id = model.PoId });
            }

            if (model.Command == "viewrr")
            {
                return RedirectToAction("ViewRR", "TransHist", new { rr_Id = model.RRId });
            }

            if (model.Command == "save" || model.Command == "accept")
            {
                var message = ValidateOrgCode(model);
                if (!string.IsNullOrEmpty(message))
                {
                    model.ModelStatus = message;
                    return View(model);
                }

                manager.UpdateContact(model);

                if (model.OpenOption.ToLower() != "menu")
                {
                    var exeption = TempData["exception"] as EXCEPTION;

                    manager.UpdateNotification(model, exeption);
                }
            }

            if (model.Command == "accept")
            {
                return RedirectToAction("AcceptMain", "Exceptions", new { pDocNo = model.PoDocNumber });
            }

            if (model.Command == "delete")
            {
                manager.DeleteContact(model);

                
            }

            return View("Edit", model);
        }

        [HttpPost]
        public ActionResult ContactRecords([DataSourceRequest]DataSourceRequest request, ContactViewModel search)
        {
            var manager = new ContactManager();

            List<ContactViewModel> data = null;
            if (!string.IsNullOrEmpty(search.ErrorCode))
            {
                var exceptionManager = new ExceptionsManager();
                var exception = exceptionManager.GetExceptionByExId(search.ExceptionId);
                TempData["exception"] = exception;

                data = manager.GetByException(search.ExceptionId, search.ErrorCode, search);
            }
            else
            {
                search.FtsOrgCodeList = FetchFtsOrgCodeList();
                search.PbsOrgCodeList = FetchPbsOrgCodeList();
                search.AssignedService = AssignSrv;

                data = manager.Get(search);
            }

            var result = data.AsQueryable().ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private void BindModel(ContactViewModel model)
        {
            model.PageTitle = "Contact Data Entry for COOP";
            if (!string.IsNullOrEmpty(model.ErrorCode))
            {
                model.PageTitle = "Contact Data Entry for Exception " + model.ErrorCode;
            }
            else
            {
                model.PageTitle = "Contact Data Entry for " + model.AssignedService;
            }

            model.OpenOption = model.OpenOption.ToLower();
            if (model.OpenOption == "menu")
            {
                model.ViewPOEnabled = true;
                model.ViewRREnabled = true;
                model.ViewInvEnabled = true;
                model.SaveEnabled = true;
                model.DeleteEnabled = true;
                model.NewEnabled = true;
                model.CancelEnabled = true;
                model.SkipVisible = false;
                model.AcceptVisible = false;
            }
            else if (model.OpenOption == "exception")
            {
                model.ViewPOEnabled = false;
                model.ViewRREnabled = false;
                model.ViewInvEnabled = false;
                model.SaveEnabled = false;
                model.DeleteEnabled = false;
                model.NewEnabled = false;
                model.CancelEnabled = true;
                model.SkipVisible = true;
                model.AcceptVisible = true;

                //sets UI model properties based on exception and notification look up
                var manager = new ContactManager();
                manager.GetByException(model.ExceptionId, model.ErrorCode, model);
            }
        }

        private string ValidateOrgCode(ContactViewModel model)
        {
            var message = "";

            model.FtsOrgCodeList = FetchFtsOrgCodeList();
            model.PbsOrgCodeList = FetchPbsOrgCodeList();

            if ((model.FtsOrgCodeList.Count == 0 && model.AssignedService == "FTS") || (model.PbsOrgCodeList.Count == 0 && model.AssignedService == "PBS"))
            {
                message = "Org codes for the selected assigned service have not been set...Please exit out of application and try again";
                return message;
            }

            var pbsComparelist = model.PbsOrgCodeList.Select(s => s = s.Trim(' ').Trim('\''));
            if (!pbsComparelist.Any(x => x.StartsWith(model.OrgCode.Substring(0,1))) && model.AssignedService == "PBS")
            {
                message = "PBS user can ONLY enter OrgCodes starting with: " + string.Join(", ", model.PbsOrgCodeList);
                return message;
            }

            var ftsComparelist = model.FtsOrgCodeList.Select(s => s = s.Trim(' ').Trim('\''));
            if (!ftsComparelist.Any(x => x.StartsWith(model.OrgCode.Substring(0, 1))) && model.AssignedService == "FTS")
            {
                message = "FTS user can ONLY enter OrgCodes starting with with: " + string.Join(", ", model.FtsOrgCodeList);
                return message;
            }

            return message;
        }

        private List<string> FetchFtsOrgCodeList()
        {
            var list = new List<string>();

            using (var context = new OracleVitapBdrReportContext())
            {
                var item = context.SYS_APP_CONFIG.FirstOrDefault(x => x.CONFIG_APP.ToUpper() == "VITAP" && x.CONFIG_NAME.ToUpper() == "FTS_ORGCODE_PREFIXES");
                if (item != null)
                {
                    list = item.CONFIG_VALUE.Split(',').ToList();
                }
            }

            return list;
        }

        private List<string> FetchPbsOrgCodeList()
        {
            var list = new List<string>();

            using (var context = new OracleVitapBdrReportContext())
            {
                var item = context.SYS_APP_CONFIG.FirstOrDefault(x => x.CONFIG_APP.ToUpper() == "VITAP" && x.CONFIG_NAME.ToUpper() == "PBS_ORGCODE_PREFIXES");
                if (item != null)
                {
                    list = item.CONFIG_VALUE.Split(',').ToList();
                }
            }

            return list;
        }
    }
}