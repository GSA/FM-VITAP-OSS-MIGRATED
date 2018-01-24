using System;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using VITAP.Data.Models;
using VITAP.Library.Strings;

namespace VITAP.Utilities.Security
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public class SecureAuthAttribute : AuthorizeAttribute
    {
        public SecureAuthAttribute()
        {
            Order = 1;
        }

        public SecureAuthAttribute(string RolesConfigKey)
        {
            var authorizedRoles = ConfigurationManager.AppSettings[RolesConfigKey];
            Roles = String.IsNullOrEmpty(Roles) ? authorizedRoles : Roles;
            Order = 0;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var redirect = true;
            var Session = HttpContext.Current.Session;

            // Does this resource allow anonymous access?
            bool skipAuthorization = !Authentication.IsUsingSecureAuth()
                    || filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true)
                    || filterContext.ActionDescriptor.ControllerDescriptor.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
            if (skipAuthorization)
            {
                redirect = false;
            }
            else if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. See if there is SecureAuth session state.
                if (Authentication.IsValidSecureAuthUser()) redirect = false;
            }
            else
            {
                var roleModel = Session[SessionKey.RoleModel] as RoleListModel;
                if (Authentication.HasTopLevelAccess(roleModel)) redirect = false;    
            }

            if (redirect)
            {
                HandleUnauthorizedRequest(filterContext);
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Redirect to the Login page.
            filterContext.Result = new RedirectToRouteResult(new System.Web.Routing.RouteValueDictionary(new { controller = "Login", action = "Unauthorized" }));
        }
    }
}