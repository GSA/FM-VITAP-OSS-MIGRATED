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
    public class ExceptionPreparerCodeController : VitapBaseController
    {
        
        [HttpGet]
        public ActionResult Index()
        {

            var roleModel = this.RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            return View(new ExceptionPrepCodeViewModel());
        }

        [HttpPost]
        public ActionResult Index(ExceptionPrepCodeViewModel model)
        {
            string clearDate = "", sortBy = "", searchIn = "";

            switch (model.Answer)
            {
                case "1":
                default:
                    clearDate = " not cleared_date is null  ";
                    break;
                case "2":
                    clearDate = " cleared_date >= trunc(sysdate) ";
                    break;
                case "3":
                    clearDate = " cleared_date >= trunc(sysdate - 7) ";
                    break;
                case "4":
                    clearDate = " cleared_date >= trunc(sysdate - 30) ";
                    break;
                case "5":
                    clearDate = " cleared_date >= to_date('" + model.From.ShortDate() + "','MM/DD/YYYY') ";
                    clearDate = clearDate + " and cleared_date  <= (to_date('" + model.To.ShortDate() + "', 'MM/DD/YYYY') + 1) ";
                    break;
            }

            switch (model.SearchCriteria)
            {
                case "PrepCode":
                    searchIn = "prepcode";
                    break;
                case "CorSymbol":
                    searchIn = "corsymb";
                    break;
                //default:
                //    searchIn = "";
                //    break;
            }


            //switch (model.Sort)
            //{
            //    case "Name":
                    sortBy = " pcname ASC ";
            /*        break;
                case "PrepCode":
                    sortBy = " prepcode ASC ";
                    break;
                case "WGTScore":
                    sortBy = " wgtscore DESC ";
                    break;
                case "CorSymbol":
                    sortBy = " corsymb ASC ";
                    break;
                //default:
                //    sortBy = "";
                //    break;
            }*/

            model.ExceptPC = GetExceptionPrepCode(clearDate, searchIn, (String.IsNullOrEmpty(model.SearchValue) ? "" : model.SearchValue), sortBy);

            return (View(model));
        }

        private List<ExceptionPreparerCode> GetExceptionPrepCode(string strClearDate, string strSearchIn, string strPrepCode, string strSortBy)
        {
            var mgr = new ExceptionsManager();
            List<ExceptionPreparerCode> model = new List<ExceptionPreparerCode>();
            model = mgr.GetExceptPrepCode(AssignSrv, strClearDate, strSearchIn, strPrepCode, strSortBy);
            return model;
        }
    }
}