using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using GSA.R7BD.Utility;
using VITAP.Data.Managers;
using VITAP.Data.Managers.LoginManager;
using VITAP.Data.Models;
using VITAP.Library.Strings;

namespace VITAP.Utilities.Security
{
    public class Authentication
    {
        public static bool IsUsingSecureAuth()
        {
            return !HttpContext.Current.Request.IsLocal;
        }

        public static void SetSecureAuthSessionState(string UserID, string Email)
        {
            HttpContext.Current.Items.Add(Login.SECUREAUTHUSERKEY, UserID);
            HttpContext.Current.Items.Add(Login.SECUREAUTHEMAILKEY, Email);
        }

        public static void ClearSecureAuthSessionState()
        {
            HttpContext.Current.Items.Remove(Login.SECUREAUTHUSERKEY);
            HttpContext.Current.Items.Remove(Login.SECUREAUTHEMAILKEY);
        }

        public static void ClearSessionState()
        {
            // Remove all session info and redirect.
            HttpContext.Current.Session.RemoveAll();
            Authentication.ClearSecureAuthSessionState();
        }

        // Two valid conditions:
        // 1) SecureAuth user exists, but no session user has been set yet (1st-time login).
        // 2) SecureAuth user and session users both exist, and names match.
        public static bool IsValidSecureAuthUser()
        {
            var session = HttpContext.Current.Session;
            var secureAuthUser = HttpContext.Current.Items[Login.SECUREAUTHUSERKEY];
            var sessionUser = session[SessionKey.UserName];
            
            if (secureAuthUser == null) return false;

            if (sessionUser != null) {
                // If users don't match between SecureAuth and session state, someone else 
                // reauthenticated during this session. That's not allowed.
                if (sessionUser.ToString() != secureAuthUser.ToString().ToUpper()) return false;
            }

            // Now verify the user.
            bool verified = VerifyUser(secureAuthUser.ToString());
            if (verified)
            {
                // Update user log.
                EventLog.RecordLogin(secureAuthUser.ToString(), "", HttpContext.Current.Request.UserHostAddress, Login.APPNAME);
            }
            else
            {
                // Insufficient access.
                string errorMessage = secureAuthUser.ToString() + Login.INSUFFICIENTACCESS;
                Logging.AddWebError(errorMessage);
            }
            return verified;
        }

        // If the user has the Financeuser role, this will set all user session state
        // and return true.
        // If not, clear all session state and return false.
        public static bool VerifyUser(string UserName)
        {
            var session = HttpContext.Current.Session;
            UserName = UserName.ToUpper();
            session[SessionKey.UserName] = UserName;

            var roleModel = new RoleListModel();
            var loginMgr = new LoginManager();
            var mgr = new RolesManager();
            List<String> roleList = new List<String>();

            // Get Roles
            var roles = DataAccess.GetRole(UserName, "VITAP");
            foreach (DataRow row in roles.Rows)
            {
                roleList.Add(row["ROLE_NAME"].ToString());
            }
            roleModel = mgr.GetUserRoles(UserName, roleModel, roleList, "", "", "");

            // Top-level access check (finance role).
            if (!HasTopLevelAccess(roleModel))
            {
                ClearSessionState();
                return false;
            }

            roleModel.USERNAME = UserName;
            session.Add(SessionKey.Roles, roleList);

            // Get Other Login Data
            var LoginData = loginMgr.GetLoginData(UserName).ToList().FirstOrDefault();
            if (LoginData != null)
            {
                session.Add(SessionKey.AssignSrv, LoginData.ASSIGN_SRV);
                roleModel.ASSIGN_SRV = LoginData.ASSIGN_SRV;
                session.Add(SessionKey.PrepCode, LoginData.PREPCODE);
                roleModel.PREPCODE = LoginData.PREPCODE;
                session.Add(SessionKey.Symbol, LoginData.SYMBOL);
                roleModel.SYMBOL = LoginData.SYMBOL;
            }
            session.Add(SessionKey.RoleModel, roleModel);

            return true;
        }

        public static bool HasTopLevelAccess(RoleListModel roleModel)
        {
            return (roleModel.HasFinanceuserRole || roleModel.HasCustServiceRole);
        }
    }
}