using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Data.Models.MainNav;
using VITAP.Library.Strings;
using VITAP.Utilities.Security;

namespace VITAP.Controllers
{
    public class MainNavController : VitapBaseController
    {
        // GET: NavMenu
        public ActionResult Index()
        {
            var model = new MainNavModel();
            RoleListModel roleModel = new RoleListModel();
  
            if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. Redirect to the login page.
                return RedirectToAction("Index", "Login");
            }
              
            roleModel = Session[SessionKey.RoleModel] as RoleListModel;
            model.HasExceptionsRole = roleModel.HasExceptionsRole;

            return View("Index", roleModel);
        }

        private RoleListModel GetRolesViewModel()
        {
            RoleListModel roleModel = new RoleListModel();

            //UserRoles
            roleModel.HasApacctRole = false;
            roleModel.HasCustServiceRole = false;
            roleModel.HasDataEntryRole = false;
            roleModel.HasExceptionsRole = false;
            roleModel.HasMgtReportsRole = false;
            roleModel.HasTopsRole = false;
            roleModel.HasVcRole = false;
            roleModel.HasFinanceuserRole = false;
            roleModel.HasVendCoderRole = false;
            roleModel.HasVerifyDupRole = false;
            roleModel.PREPCODE = "";
            roleModel.SYMBOL = "";
            roleModel.ASSIGN_SRV = "";

            if (this.UserName != null)
            {
                var mgr = new RolesManager();
                roleModel = mgr.GetUserRoles(this.UserName, roleModel,
                    this.Roles as List<String>, 
                    this.AssignSrv,
                    this.PrepCode, 
                    this.Symbol);
            } 
                       
            return roleModel;
        }
    }
}