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
    public class VitapBaseController : Controller
    {
        public string UserName
        {
            get
            {
                if (Session != null && Session[SessionKey.UserName] != null)
                {
                    return Session[SessionKey.UserName].ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        public List<SelectListItem> ddlSearchIn
        {
            get
            {
                return new List<SelectListItem>()
                    {
                        new SelectListItem { Text = "PDocNo", Value = "PDocNo" },
                        new SelectListItem { Text = "ACT", Value = "ACT" },
                        new SelectListItem { Text = "PO ID", Value = "PO_ID" },
                        new SelectListItem { Text = "Inv Key ID", Value = "INV_KEY_ID" },
                        new SelectListItem { Text = "RR ID", Value = "RR_ID" },
                        new SelectListItem { Text = "AE ID", Value = "AE_ID" },
                        new SelectListItem { Text = "PA ID", Value = "PA_ID" },
                        new SelectListItem { Text = "Recurring Master ID", Value = "RECUR_ID" },
                        new SelectListItem { Text = "Vendor Name", Value = "VendName" },
                        new SelectListItem { Text = "Contract", Value = "Contract" },
                        new SelectListItem { Text = "PO Number", Value = "PONumber" },
                        new SelectListItem { Text = "Invoice", Value = "Invoice" },
                        new SelectListItem { Text = "Amount", Value = "Amount" }
                    };
            }
        }

        public string PrepCode {
            get {
                if (Session != null && Session[SessionKey.PrepCode] != null) {
                    return Session[SessionKey.PrepCode].ToString();
                }
                else {
                    return "";
                }
            }
        }
        public string Symbol
        {
            get
            {
                if (Session != null && Session[SessionKey.Symbol] != null) {
                    return Session[SessionKey.Symbol].ToString();
                }
                else {
                    return "";
                }
            }
        }
        public string AssignSrv
        {
            get
            {
                if(Session != null && Session[SessionKey.AssignSrv] != null) {
                    return Session[SessionKey.AssignSrv].ToString();
                }
                else {
                    return "";
                }
            }
        }

        public RoleListModel RoleList
        {
            get
            { 
                if (Session != null && Session[SessionKey.RoleModel] != null) {
                    return Session[SessionKey.RoleModel] as RoleListModel;
                }
                else {
                    return null;
                }
            }
        }

        public List<String> Roles
        {
            get
            {
                List<String> rolesList = new List<String>();

                if(Session != null  && Session[SessionKey.Roles] != null) {
                    rolesList = (List<String>)Session[SessionKey.Roles];
                }

                return rolesList;
            }
        }
        public bool UserHasRole(string role)
        {
            bool hasRole = false;

            if(Roles != null) {
                if (Roles.Contains(role))
                {
                    hasRole = true;
                }
            }

            return hasRole;
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = int.MaxValue
            };
        }

        [HttpPost]
        public ActionResult Excel_Export_Save(string contentType, string base64, string fileName)
        {
            var fileContents = Convert.FromBase64String(base64);

            return File(fileContents, contentType, fileName);
        }
    }
}
