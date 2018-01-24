using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Data;
using System.Web.Mvc;
using VITAP.Data.Models.Login;
using GSA.R7BD.Utility;
using VITAP.Data.Managers.LoginManager;
using VITAP.Data.Models;
using VITAP.Data.Managers;
using VITAP.Library.Strings;
using VITAP.Utilities;
using VITAP.Utilities.Attributes;
using VITAP.Utilities.Security;

namespace VITAP.Controllers
{
    [AllowAnonymous]
    public class LoginController : VitapBaseController
    {
        Audit ad = new Audit();
        // GET: 
        [HttpGet]
        public ActionResult Index()
        {
            var model = new LoginModel();
            model.SecureAuth = Authentication.IsUsingSecureAuth();
            model.ShowSecureAuthButton = false;

            ViewBag.ErrorMessage = TempData["ErrorMessage"];

            if (!model.SecureAuth)
            {
                return Login("seungjunlee");
            }

            // This is the SecureAuth path. If no user, show the login page, which will automatically
            // login by "clicking" the button via Javascript.
            if (Session[SessionKey.UserName] == null)
            {
                return View("Login", model);
            }

            // We already have a username so just redirect to home.
            return RedirectToAction("Index", "Home");
        }

        [HttpPost, ActionName("Login"), SubmitButton(Name = "btnLogin")]
        public ActionResult Login(string UserName)
        {
            // If we're using SecureAuth, prevent manual login.
            if (Authentication.IsUsingSecureAuth()) return LoginSSO();

            if (Authentication.VerifyUser(UserName))
            {
                // Update user log.
                ad.WriteUserEvent(Library.Strings.Login.APPNAME, UserName, Audit.UserEvent.LogonSuccessful);
                return RedirectToAction("Index", "Home");
            }
            else
            {
                // Insufficient access.
                string errorMessage = UserName + VITAP.Library.Strings.Login.INSUFFICIENTACCESS;
                TempData["ErrorMessage"] = errorMessage;
                ad.WriteUserEvent(Library.Strings.Login.APPNAME, UserName, Audit.UserEvent.LogonFailed);
                return ClearSessionStateAndRedirect();
            }
        }

        [HttpPost, ActionName("Login"), SubmitButton(Name = "btnLoginSSO")]
        public ActionResult LoginSSO()
        {
            // By returning 401, we tell the app to go to the SecureAuth loginUrl in the <authentication> tag
            // block in web.config.
            return new HttpStatusCodeResult(System.Net.HttpStatusCode.Unauthorized);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            if (!string.IsNullOrEmpty(this.UserName))
            {                
                ad.WriteUserEvent(Library.Strings.Login.APPNAME, UserName, Audit.UserEvent.Logoff);
                TempData["ErrorMessage"] = this.UserName + VITAP.Library.Strings.Login.LOGGEDOUT; ;
            }

            return ClearSessionStateAndRedirect();
        }

        [HttpGet]
        public ActionResult Unauthorized()
        {
            string userName = "";
            var secureAuthUser = HttpContext.Items[VITAP.Library.Strings.Login.SECUREAUTHUSERKEY];
            if (secureAuthUser != null) userName = secureAuthUser.ToString();
            if (!String.IsNullOrEmpty(userName))
            {
                TempData["ErrorMessage"] = userName + VITAP.Library.Strings.Login.INSUFFICIENTACCESS;
                return ClearSessionStateAndRedirect();
            } 
            else
            {
                // No SecureAuth user name means we try again to get one.
                return LoginSSO();
            }
        }

        private ActionResult ClearSessionStateAndRedirect()
        {
            var model = new LoginModel();
            model.SecureAuth = Authentication.IsUsingSecureAuth();
            // Show the login button if using SecureAuth.
            model.ShowSecureAuthButton = model.SecureAuth;

            Authentication.ClearSessionState();

            if (model.SecureAuth) return View("Login", model);

            // Local Visual Studio/dev just redirects to login action.
            return RedirectToAction("Index", "Login");
        }

        [HttpGet]
        public ActionResult SessionOut()
        {
            if (!string.IsNullOrEmpty(this.UserName))
            {
                ad.WriteUserEvent(Library.Strings.Login.APPNAME, UserName, Audit.UserEvent.SessionTimeout);
                TempData["ErrorMessage"] = UserName + Audit.UserEvent.SessionTimeout;
            }

            return ClearSessionStateAndRedirect();
        }
    }
}
