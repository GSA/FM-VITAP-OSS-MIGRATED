using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.Models;
using System.Reflection;
using VITAP.ViewModels.Reports.User;
using VITAP.Library.Strings;

namespace VITAP.Controllers
{
    public class DaysOldController : VitapBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {
            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            if (roleList.HasExceptionsRole == false)
            {
                RedirectToAction("Index", "Home");
            }

            return View(new DaysOldInvoiceViewModel());
        }

        [HttpPost]
        public ActionResult Index(DaysOldInvoiceViewModel model)
        {
            string ssql = "", sortOpt = "";

            switch (model.SearchBy)
            {
                case "FTS, GSAP":
                    ssql = ssql + " and (SUBSTR(a.orgcode,1,1)='A') and c.pegsystem = '3GS'";
                    break;
                case "FTS, nonGSAP":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1)='A') and (c.pegsystem in ('FTS','06') and (substr(pdocno,1,2) = '2B' or substr(pdocno,1,2) like 'T%'))"; 
                    break;
                case "FTS, Network Services":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1)='A')  and ((c.pegsystem = 'TOPS' or (substr(pdocno,1,2) not in ('1B','RO','PJ','PN','PS','2I') and c.ba like 'B%')))";
                    break;
                case "192X, VCPO only":
                    ssql = ssql + "  and ((a.err_code = 'R200' or b.vcpo = 'T' or SUBSTR(a.pdocnopo,1,2) = 'RO') and (SUBSTR(a.orgcode,1,1) in ('K','P') or (a.orgcode is null and substr(nvl(a.pdocnopo,c.pdocno),1,2) = '1B'))) ";
                    break;
                case "192X, Non-Recurring":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1) in ('K','P')) and (c.pegsystem in ('PBS','07') and substr(nvl(a.pdocnopo,c.pdocno),1,2) in ('1B','PJ','PS'))";
                    break;
                case "192X, All":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1) in ('K','P') or (a.orgcode is null and substr(nvl(a.pdocnopo,c.pdocno),1,2) in ('1B','PJ','PS','RO'))) ";
                    break;
                case "PBS, 442":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1) in ('K','P')) and (c.pegsystem in ('PBS','07','ARRA') and substr(nvl(a.pdocnopo,c.pdocno),1,2) in ('1B','PJ','PS','GP'))";
                    break;
                case "Other":
                    ssql = ssql + "  and (SUBSTR(a.orgcode,1,1) not in ('K','A','P'))";
                    break;
                case "All":
                    ssql = ssql + "";
                    break;
                default:
                    ssql = ssql + "";
                    break;
            }

            switch (model.OrderBy)
            {
                case "Days Old":
                    ssql = ssql + " order by daysold desc";
                    sortOpt = " daysold desc";
                    break;
                case "Amount":
                    ssql = ssql + " order by amount DESC";
                    sortOpt = " amount DESC";
                    break;
                case "ACT #":
                    ssql = ssql + " order by act";
                    sortOpt = " act";
                    break;
                case "INV #":
                    ssql = ssql + " order by invoice";
                    sortOpt = " invoice";
                    break;
                case "Start Date/Log Date":
                    ssql = ssql + " order by logdate desc";
                    sortOpt = " logdate desc";
                    break;
                case "OrgCode":
                    ssql = ssql + " order by orgcode";
                    sortOpt = " orgcode";
                    break;
                case "Vendor":
                    ssql = ssql + " order by vendname";
                    sortOpt = " vendname";
                    break;
                case "Cumulative Interest":
                    ssql = ssql + " order by cumint desc";
                    sortOpt = " cumint desc";
                    break;
                case "Daily Interest":
                    ssql = ssql + " order by dailyint desc";
                    sortOpt = " dailyint desc";
                    break;
                case "PDocNoPo":
                    ssql = ssql + " order by pdocnopo";
                    sortOpt = " pdocnopo";
                    break;
                case "Inv Key Id":
                    ssql = ssql + " order by inv_key_id";
                    sortOpt = " inv_key_id";
                    break;
                case "None":
                    ssql = ssql + "";
                    sortOpt = "";
                    break;
                default:
                    ssql = ssql + "";
                    sortOpt = "";
                    break;
            }
                    model.DaysOldInv = GetDOInvoice(model.SearchBy, ssql, sortOpt, model.From, model.To);
                    return (View(model));
        }

        private List<DaysOldInvoice> GetDOInvoice(string strSearch, string strSql, string strOrder, int daysFrom, int daysTo)
        {
            var mgr = new PegasysInvoiceManager();
            List<DaysOldInvoice> model = new List<DaysOldInvoice>();
            model = mgr.GetDaysOldInvoice(strSearch, strSql, strOrder, daysFrom, daysTo);
            return model;
        }
    }
}

