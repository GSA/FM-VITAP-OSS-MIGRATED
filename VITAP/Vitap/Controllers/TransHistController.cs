using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Library.Strings;
using VITAP.ViewModels.TransHist;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using Vitap.Data.Models.TransHist;
using VITAP.ViewModels.TransHist.PegasysRR;
using Vitap.Data.Managers;
using VITAP.Data.Models.MainNav;
using VITAP.Utilities;
using VITAP.Data.Models.Exceptions;
using Elmah;
using VITAP.Data.Models.CombineAccruals;

namespace VITAP.Controllers
{

    public class TransHistController : VitapBaseController
    {
        TransHistManager transHistManager = new TransHistManager();
        POManager poManager = new POManager();

        #region Index View Methods

        public ActionResult Index(string searchFor, string searchIn, string searchShow, string searchFY, string id, string act, string pDocNo, string searchedBy)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            // This is suspicious. We may want to use TempData instead, and it probably shouldn't
            // be set here.
            Session[SessionKey.ListRRs] = null;
            NormalizeSearchParams(ref searchFor, ref searchIn, ref searchShow, ref searchFY, ref id, ref act, ref pDocNo, ref searchedBy);

            if (string.IsNullOrEmpty(searchFor))
            {
                // Get VendorInfo when user click one item from POList.cshtml
                if (!string.IsNullOrWhiteSpace(pDocNo) || !string.IsNullOrWhiteSpace(act))
                {
                    GetVendorInfoFromPOList(pDocNo, act);
                }
                var list = GetTransHistByPDocOrAct(pDocNo, act, searchedBy, searchShow, searchFY);

                // Will return empty list on initial redirect to Index.
                if (list.Count == 0 &&
                    (!string.IsNullOrWhiteSpace(pDocNo) || !string.IsNullOrWhiteSpace(act)) &&
                    !string.IsNullOrWhiteSpace(searchedBy))
                {
                    ViewBag.NoResult = "No Data Found";
                }
                if (list.Count == 0 && !string.IsNullOrWhiteSpace(searchedBy))
                {
                    ViewBag.NoResult = "No Data Found";
                }
                return GetIndexView(list);
            }

            // Validate user inputs
            string errorMessage = ValidateSearchFor(searchFor, searchIn);
            if (errorMessage.Length > 0)
            {
                ViewBag.ValidationError = errorMessage;
                return GetIndexView();
            }

            var isPOSearch = "VendName/Contract/PONumber/Invoice/Amount".Split('/').Contains(searchIn);
            if (isPOSearch)
                return GetPOView(searchFor, searchIn, searchShow, searchFY);

            var lstTransHist = transHistManager.GeneralSearch(searchFor, searchIn, searchShow, searchFY, true);

            if (lstTransHist.Count() > 0)
            {
                var tranHist = lstTransHist.FirstOrDefault();
                GetVendorInfo(searchIn, tranHist.ACT, tranHist.PDOCNO, tranHist.RECUR_ID);
            }
            else
            {
                ViewBag.NoResult = "No Data Found";
            }
            return GetIndexView(lstTransHist);
        }

        private void GetVendorInfoFromPOList(string pDocNo, string act)
        {
            if (!string.IsNullOrWhiteSpace(pDocNo) && !string.IsNullOrWhiteSpace(act))
            {
                GetVendorInfo("PDocNo", act, pDocNo, string.Empty);
            }
            else if (!string.IsNullOrWhiteSpace(pDocNo) && string.IsNullOrWhiteSpace(act))
            {
                GetVendorInfo("PDocNo", act, pDocNo, string.Empty);
            }
            else
            {
                GetVendorInfo("ACT", act, pDocNo, string.Empty);
            }
        }

        private void GetVendorInfo(string searchIn, string act, string pDocNo, string recurId)
        {
            if (searchIn == "ACT" && act.Length == 8)
            {
                GetVendorInfoByAct(act);
            }
            if (searchIn == "RECUR_ID")
            {
                GetVendorInfoByRecurId(recurId);
            }
            else if (searchIn.ToUpper() == "PDOCNO" && !string.IsNullOrWhiteSpace(pDocNo))
            {
                GetVendorInfoByPDocNo(pDocNo);
            }

        }

        private void GetVendorInfoByAct(string act)
        {
            // From Pegasys MF_IO
            var vendors = GetVendorsFromIoByAct(act);
            if (vendors.Count() > 0)
            {
                InitVendorInfoGrid(vendors.FirstOrDefault());
                return;
            }
            // From Pegasys MF_TG
            vendors = GetVendorsFromTgByAct(act);
            if (vendors.Count() > 0)
            {
                InitVendorInfoGrid(vendors.FirstOrDefault());
                return;
            }
            if (act.StartsWith("B"))
            {
                // From Pegasys MF_LEAS_VEND
                vendors = GetVendorsFromLeasByAct(act);
                if (vendors.Count() > 0)
                {
                    InitVendorInfoGrid(vendors.FirstOrDefault());
                    return;
                }
            }
            // From VITAP PegasysPo_Frm
            vendors = GetVendorsFromPPFrmByAct(act);
            if (vendors.Count() == 0)
                return;
            InitVendorInfoGrid(vendors.FirstOrDefault());
            return;
        }

        private void GetVendorInfoByRecurId(string recurId)
        {
            var vendors = GetVendorsFromLeasByRecurId(recurId);
            if (vendors.Count() > 0)
            {
                InitVendorInfoGridByRecurId(vendors.FirstOrDefault());
                return;
            }
        }

        private void GetVendorInfoByPDocNo(string pDocNo)
        {
            // From Pegasys MF_IO
            var vendors = GetVendorsFromIoByPDocNo(pDocNo);
            if (vendors.Count() > 0)
            {
                InitVendorInfoGrid(vendors.FirstOrDefault());
                return;
            }
            // From Pegasys MF_TG
            vendors = GetVendorsFromTgByPDocNo(pDocNo);
            if (vendors.Count() > 0)
            {
                InitVendorInfoGrid(vendors.FirstOrDefault());
                return;
            }
            if (pDocNo.StartsWith("1B"))
            {
                // From Pegasys MF_LEAS_VEND
                vendors = GetVendorsFromLeasByPDocNo(pDocNo);
                if (vendors.Count() > 0)
                {
                    InitVendorInfoGrid(vendors.FirstOrDefault());
                    return;
                }
            }
            // From VITAP PegasysPo_Frm
            vendors = GetVendorsFromPPFrmByPDocNo(pDocNo);
            if (vendors.Count() > 0) InitVendorInfoGrid(vendors.FirstOrDefault());
            return;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromIoByAct(string act)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var lstVendors = transHistPODataManager.GetVendorInfoActMf_Io(act);
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromIoByPDocNo(string pDocNo)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var lstVendors = transHistPODataManager.GetVendorInfoPDocNoMf_Io(pDocNo);
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromTgByAct(string act)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoActMf_Tg(act);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEF(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromTgByPDocNo(string pDocNo)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoPDocNoMf_Tg(pDocNo);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEF(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }
        private IEnumerable<TransHistPOData> GetVendorsFromLeasByAct(string act)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoActLeas(act);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEFLeas(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromLeasByRecurId(string recurId)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoRecurIdLeas(recurId);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEFLeasByRecurId(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }


        private IEnumerable<TransHistPOData> GetVendorsFromLeasByPDocNo(string pDocNo)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var lstVendors = transHistPODataManager.GetVendorInfoPDocNoLeas(pDocNo);
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromPPFrmByAct(string act)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoActPPFrm(act);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEFPPFrm(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }

        private IEnumerable<TransHistPOData> GetVendorsFromPPFrmByPDocNo(string pDocNo)
        {
            var transHistPODataManager = new TransHistPODataManager();
            var efVendors = transHistPODataManager.GetVendorInfoPDocNoPPFrm(pDocNo);

            var lstVendors = new List<TransHistPOData>();
            foreach (var efVendor in efVendors)
            {
                var vmVendor = TransHistPOData.MapFromEFPPFrm(efVendor);
                lstVendors.Add(vmVendor);
            }
            return lstVendors;
        }

        private void InitVendorInfoGrid(TransHistPOData vendor)
        {
            var mrgVendorInfo = new TransHistVendorInfoManager();
            var vendorInfo = new TransHistVendorInfo();
            var vendorInfos = new List<TransHistVendorInfo>();

            ViewBag.VendorTitle = "Vendor Name";
            if (vendor.FROMWHERE == DBNames.Pegasys)
            {
                if (!string.IsNullOrWhiteSpace(vendor.DGGT_ID))
                {
                    ViewBag.VendorTitle = "Designated Agent Name";
                    vendorInfo = mrgVendorInfo.GetVendorInfo(vendor.DGGT_ID, vendor.CONTRACT).FirstOrDefault();
                }
                else if (!string.IsNullOrWhiteSpace(vendor.RMIT_VEND_ID))
                {
                    ViewBag.VendorTitle = "Remit Vendor Name";
                    vendorInfo = mrgVendorInfo.GetVendorInfo(vendor.RMIT_VEND_ID, vendor.CONTRACT).FirstOrDefault();
                }
                else if (!string.IsNullOrWhiteSpace(vendor.VEND_ID))
                {
                    vendorInfo = mrgVendorInfo.GetVendorInfo(vendor.VEND_ID, vendor.CONTRACT).FirstOrDefault();
                }
            }
            else
            {
                ViewBag.VendorTitle = "Remit Vendor Name";
                vendorInfo = GetVendorInfoFromVitap(vendor).FirstOrDefault();

            }
            if (vendor.FROMWHERE == DBNames.Lease)
            {
                vendorInfo.CONTRACT = string.Empty;
            }
            vendorInfo = GetPoType(vendor, vendorInfo);
            vendorInfos.Add(vendorInfo);
            ViewBag.VendorInfo = vendorInfos;
            ViewBag.PDocNoFromPoData = vendor.DOC_NUM;
        }


        private void InitVendorInfoGridByRecurId(TransHistPOData vendor)
        {

            var mrgPOData = new TransHistPODataManager();
            var mrgVendorInfo = new TransHistVendorInfoManager();
            var vendorInfo = new TransHistVendorInfo();
            var vendorInfos = new List<TransHistVendorInfo>();

            ViewBag.VendorTitle = "Vendor Name";

            vendorInfo.VENDORNAME = vendor.REMIT_VENDNAME;
            vendorInfo.VENDORCODE = vendor.VEND_CD;
            vendorInfo.ADDRESSCODE = vendor.VEND_ADDR_CD;

            var rtn = mrgPOData.GetVendorInfoAddrLevl(vendor.VEND_CD, vendor.VEND_ADDR_CD);
            if (rtn.Count() > 0)
            {
                var v = rtn.FirstOrDefault();
                vendorInfo.VENDORNAME = v.NM;
                vendorInfo.VENDORTYPE = v.DFLT_PYMT_TYP_ID.Substring(6);
            }
            vendorInfo.POTYPE = "RECUR";

            vendorInfos.Add(vendorInfo);
            ViewBag.VendorInfo = vendorInfos;
        }
        private TransHistVendorInfo GetPoType(TransHistPOData vendor, TransHistVendorInfo vendorInfo)
        {
            if (!string.IsNullOrWhiteSpace(vendor.DOC_NUM))
            {
                switch (vendor.DOC_NUM.Left(2).ToUpper())
                {
                    case "2B":
                        vendorInfo.POTYPE = TransHistPoType.FTS;
                        break;
                    case "HB":
                        vendorInfo.POTYPE = TransHistPoType.TOPS;
                        break;
                    case "1B":
                        if (vendor.FROMWHERE == DBNames.Pegasys)
                        {
                            if (!string.IsNullOrWhiteSpace(vendor.DSCR) && vendor.DSCR.Contains(TransHistPoType.VCPO))
                                vendorInfo.POTYPE = TransHistPoType.VCPO;
                            else
                                vendorInfo.POTYPE = TransHistPoType.PBS;
                        }
                        else
                        {
                            if (vendor.FROMWHERE != DBNames.Lease)
                            {
                                if (vendor.VCPO == "T")
                                    vendorInfo.POTYPE = TransHistPoType.VCPO;
                                else
                                    vendorInfo.POTYPE = TransHistPoType.PBS;
                            }
                            else
                            {
                                vendorInfo.POTYPE = TransHistPoType.VCPO;
                            }
                        }
                        break;
                    default:
                        break;
                }
            }

            if (vendorInfo.POTYPE == TransHistPoType.PBS || vendorInfo.POTYPE == TransHistPoType.VCPO)
            {
                var mrgPoData = new TransHistPODataManager();
                var rtn = mrgPoData.CheckWithPegasysPo(vendor.DOC_NUM.Trim());
                if (rtn.Count() > 0)
                    vendorInfo.POTYPE = TransHistPoType.LEAS;
            }
            return vendorInfo;
        }
        private IEnumerable<TransHistVendorInfo> GetVendorInfoFromVitap(TransHistPOData vendor)
        {
            var vendorInfos = new List<TransHistVendorInfo>();
            var vendorInfo = new TransHistVendorInfo();

            vendorInfo.VENDORNAME = vendor.REMIT_VENDNAME;
            if (vendor.FROMWHERE != DBNames.Lease)
            {
                vendorInfo.CONTRACT = vendor.CONTRACT;
                vendorInfo.VENDORCODE = vendor.VEND_CD;
                vendorInfo.ADDRESSCODE = vendor.VEND_ADDR_CD;
                vendorInfo.VENDORTYPE = string.Empty;
            }
            vendorInfos.Add(vendorInfo);
            return vendorInfos;
        }

        private string ValidateSearchFor(string searchFor, string searchIn)
        {
            string errorMessage = string.Empty;
            switch (searchIn)
            {
                case "ACT":
                    errorMessage = searchFor.Trim().Length == 8 ? string.Empty : "ACT must be 8 characters in length";
                    break;
                case "PDocNo":
                    errorMessage = searchFor.Trim().Length >= 5 ? string.Empty : "PDocNo must be at least 5 characters in length";
                    break;
                case "RECUR_ID":
                    errorMessage = searchFor.Trim().Length >= 8 ? string.Empty : "Recurring Master ID must be at least 8 digits";
                    break;
                case "VendName":
                    errorMessage = searchFor.Trim().Length >= 3 ? string.Empty : "Vendor Name must be at least 3 characters";
                    break;
                case "Contract":
                case "PONumber":
                    errorMessage = searchFor.Trim().Length >= 5 ? string.Empty : searchIn + " must be at least 5 characters";
                    break;
                case "PO_ID":
                case "INV_KEY_ID":
                case "RR_ID":
                case "AE_ID":
                case "PA_ID":
                    errorMessage = searchFor.Trim().Length >= 9 ? string.Empty : searchIn + " must be at least 9 characters";
                    break;
                case "Amount":
                    decimal amount = 0.0M;
                    errorMessage = decimal.TryParse(searchFor.Trim(), out amount) ? string.Empty : "The Amount field must contain only numeric characters";
                    break;
                default:
                    errorMessage = string.Empty;
                    break;
            }
            return errorMessage;
        }

        private ViewResult GetIndexView(List<TransHist> model = null)
        {
            PopulateIndexLookups();
            if (model != null)
            {
                if (model.Count > 4806)
                {
                    ViewBag.NoResult = "Too many records were found. Please use additional search options to filter the data and narrow the search results.";
                    return View("Index", null);
                }
                else
                {
                    var viewModel = model.Select(m => new TransHistViewModel
                    {
                        TRANSDATE = m.TRANSDATE,
                        ACT = m.ACT,
                        PDOCNO = m.PDOCNO,
                        INVOICE = m.INVOICE,
                        PO_ID = m.PO_ID,
                        RR_ID = m.RR_ID,
                        INV_KEY_ID = m.INV_KEY_ID,
                        AE_ID = m.AE_ID,
                        PA_ID = m.PA_ID,
                        AC_ID = m.AC_ID,
                        ERR_CODE = m.ERR_CODE,
                        ERR_RESPONSE = m.ERR_RESPONSE,
                        CLEARED_DATE = m.CLEARED_DATE,
                        PREPCODE = m.PREPCODE,
                        ALLPROCESS = m.ALLPROCESS,
                        RECUR_ID = m.RECUR_ID,
                        CUFF_MEMO = m.CUFF_MEMO
                    });
                    return View("Index", viewModel);
                }
            }
            return View("Index", null);
        }

        private void PopulateIndexLookups()
        {
            PopulateSearchKeyLookup();
            PopulateTransHistTypeLookup();
            PopulateFiscalYearLookup();
        }

        private void PopulateSearchKeyLookup()
        {
            var list = new List<SelectListItem>()
            {
                new SelectListItem { Text = "ACT", Value = "ACT" },
                new SelectListItem { Text = "PDocNo", Value = "PDocNo" },
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
            ViewBag.SearchKey = list;
        }

        private void PopulateTransHistTypeLookup()
        {
            var list = new List<SelectListItem>()
            {
                new SelectListItem{ Text = "All", Value="All"},
                new SelectListItem{ Text = "EA's", Value="EAs"},
                new SelectListItem{ Text = "INV's", Value="INVs"},
                new SelectListItem{ Text = "PA's", Value="PAs"},
                new SelectListItem{ Text = "PO's", Value="POs"},
                new SelectListItem{ Text = "RR's", Value="RRs"}
            };
            ViewBag.TransHistType = list;
        }

        private void PopulateFiscalYearLookup()
        {
            var list = new List<SelectListItem>()
            {
                new SelectListItem{ Text = "All", Value="All"},
                new SelectListItem{ Text = "Last 12 Months", Value="Last 12 Mon"}
            };
            for (int year = DateTime.Now.Year; year >= 1995; year--)
            {
                list.Add(new SelectListItem { Text = year.ToString(), Value = year.ToString() });
            }
            ViewBag.FiscalYear = list;
        }

        private void NormalizeSearchParams(ref string searchFor, ref string searchIn, ref string searchShow, ref string searchFY, ref string id, ref string act, ref string pDocNo, ref string searchedBy)
        {
            searchFor = FixTextNullAndWhiteSpace(searchFor);
            searchIn = FixTextNullAndWhiteSpace(searchIn);
            searchShow = FixTextNullAndWhiteSpace(searchShow);
            searchFY = FixTextNullAndWhiteSpace(searchFY);
            id = FixTextNullAndWhiteSpace(id);
            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);
            searchedBy = FixTextNullAndWhiteSpace(searchedBy);
        }

        private ViewResult GetPOView(string searchFor, string searchIn, string searchShow, string searchFY)
        {
            var poList = new List<PO>();

            switch (searchIn.ToUpper())
            {
                case "VENDNAME":
                    poList = GetPOList(searchFor, searchIn, searchShow, searchFY, "Search By Vendor Name", "VendName");
                    break;
                case "CONTRACT":
                    poList = GetPOList(searchFor, searchIn, searchShow, searchFY, "Search By Contract", "Contract");
                    break;
                case "PONUMBER":
                    poList = GetPOList(searchFor, searchIn, searchShow, searchFY, "Search By PONumber", "PONumber");
                    break;
                case "INVOICE":
                    poList = GetPOList(searchFor, searchIn, searchShow, searchFY, "Search By Invoice", "Invoice");
                    break;
                case "AMOUNT":
                    poList = GetPOList(searchFor, searchIn, searchShow, searchFY, "Search By Amount", "Amount");
                    break;
            }

            if (poList == null || poList.Count() == 0)
            {
                ViewBag.NoResult = "No Data Found";
                return GetIndexView(null);
            }

            if (poList.Count() == 1)
            {
                if (string.IsNullOrWhiteSpace(poList.Single().PDocNo))
                {
                    GetVendorInfo("ACT", poList.Single().ACT, poList.Single().PDocNo, string.Empty);
                    return GetIndexView(transHistManager.GeneralSearch(poList.Single().ACT, "ACT", searchShow, searchFY, false));
                }
                GetVendorInfo("PDocNo", poList.Single().ACT, poList.Single().PDocNo, string.Empty);
                return GetIndexView(transHistManager.GeneralSearch(poList.Single().PDocNo, "PDocNo", searchShow, searchFY, false));
            }
            return View("POList", poList);
        }

        private List<PO> GetPOList(string searchFor, string searchIn, string searchShow, string searchFY, string title, string searchMethod)
        {
            var poList = new List<PO>();
            poList = poManager.SearchByVCPIA(searchFor, searchIn).ToList();
            ViewBag.PageTitle = title;
            ViewBag.SearchedBy = searchMethod;
            ViewBag.SearchShow = searchShow;
            ViewBag.SearchFY = searchFY;
            return poList;
        }

        private List<TransHist> GetTransHistByPDocOrAct(string pDocNo, string act, string searchedBy, string searchShow, string searchFY)
        {
            var lstTransHist = new List<TransHist>();
            if ((!string.IsNullOrEmpty(act) || !string.IsNullOrEmpty(pDocNo)) && !string.IsNullOrEmpty(searchedBy))
            {
                if (!string.IsNullOrEmpty(pDocNo))
                {
                    lstTransHist = transHistManager.GeneralSearch(pDocNo, "PDOCNO", searchShow, searchFY, false).ToList();
                }
                else
                {
                    lstTransHist = transHistManager.GeneralSearch(act, "ACT", searchShow, searchFY, false).ToList();
                }
            }
            return lstTransHist;
        }

        #endregion Index View Methods

        public ActionResult OraTwo(string searchFor, string searchIn)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            searchFor = FixTextNullAndWhiteSpace(searchFor);
            searchIn = FixTextNullAndWhiteSpace(searchIn);

            searchFor = searchFor.Replace("__", "&");

            var lstTransHist = transHistManager.GeneralSearch(searchFor, searchIn, "ALL", "ALL", false);
            ViewBag.Caption = "Search Result by " + searchIn + ":" + searchFor;
            return GetOraTwoView(lstTransHist);
        }

        private ViewResult GetOraTwoView(object viewModel = null)
        {
            return View("OraTwo", viewModel);
        }

        #region Receiving Report

        public ActionResult ViewRR(string rr_Id)
        {
            // Clear old Rrchoice.
            Session[SessionKey.Rrchoice] = null;
            if (rr_Id == null)
            {
                ViewBag.Message = "No RR ID was passed in";
                return View("ViewErrorMessage");
            }

            var rejectStatusMessage = Request.QueryString["RejectStatusMessage"];
            if (rejectStatusMessage != null && rejectStatusMessage.Length > 0)
            {
                ViewBag.RejectStatusMessage = rejectStatusMessage;
            }
            var mgrRR = new ReceivingReportManager();
            var mgrRRAccting = new ReceivingReportAccountingManager();
            var mgrRREdi = new RREdiManager();

            try
            {
                var vmRR = null as PegasysRRModel;
                vmRR = mgrRR.GetPegasysRR(rr_Id);
                if (vmRR != null)
                {
                    if (!string.IsNullOrWhiteSpace(vmRR.InboxUidy))
                    {
                        vmRR.IsSourcePegasys = true;
                        vmRR.FinReceivedDate = mgrRR.GetPegasysRRStampDate(rr_Id);
                        vmRR.RRAccounting.IsSourcePegasys = true;
                        vmRR.RRAccounting.PegasysRRAccounting = mgrRRAccting.GetPegasysRRAccounting(rr_Id);
                        vmRR.MatchedAmount = vmRR.RRAccounting.PegasysRRAccounting.Sum(a => a.MatchedAmount);

                        // Assign date fields from the returned RRAccounting list
                        var orderedRRAccounting = vmRR.RRAccounting.PegasysRRAccounting.FirstOrDefault();
                        vmRR.ReceivedDate = orderedRRAccounting.ReceivedDate;
                        vmRR.AcceptanceDate = orderedRRAccounting.AcceptanceDate;
                        vmRR.PopStartDate = orderedRRAccounting.PopStartDate;
                        vmRR.PopEndDate = orderedRRAccounting.PopEndDate;

                        vmRR.PegasysRREdi = mgrRREdi.GetPegasysRREdi(vmRR.InboxUidy).ToList();
                        vmRR.ImageId = mgrRR.GetPegasysRRImageId(rr_Id);
                        vmRR.IsEnabledViewTJ = true;

                        // Reject is not enabled for Pegasys RRs.
                        vmRR.ViewRRRejectButtonText = ControllerAction.Exception.Reject;
                        vmRR.ViewRRRejectButtonDisabled = true;
                    }
                }
                else
                {
                    vmRR = mgrRR.GetVitapRR(rr_Id);
                    if (vmRR == null)
                    {
                        ViewBag.Message = "No Receiving Report was found.";
                        return View("ViewErrorMessage");
                    }
                    vmRR.OrderNumber = vmRR.PONumber;
                    vmRR.InboxUidy = vmRR.InboxUidy ?? "";
                    vmRR.IsSourcePegasys = false;
                    vmRR.RRAccounting.IsSourcePegasys = false;
                    vmRR.RRAccounting.VitapRRAccounting = mgrRRAccting.GetRRAccountingById(rr_Id).ToList();
                    vmRR.IsEnabledViewTJ = false;

                    var roleList = Session[SessionKey.RoleModel] as RoleListModel;

                    // roleList.ASSIGN_SRV needs to be 'FTS' to be enabled.
                    SetRejectButtonForRR(vmRR, roleList.ASSIGN_SRV);
                }
                vmRR.PegasysRRDetail.Description = vmRR.Description;
                Session[SessionKey.Rrchoice] = vmRR;

                return View("ViewRR/Index", vmRR);
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/ViewRR:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "ViewRR");
                ViewBag.Message = "System error encountered.";
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View("ViewErrorMessage");
            }
        }

        // Set Enabled and text for Reject button.
        private void SetRejectButtonForRR(PegasysRRModel vmRR, string userAssignedService)
        {
            vmRR.ViewRRRejectButtonAction = "";

            if (vmRR.Status == DataEntry.Reject || vmRR.Status == DataEntry.Reject)
            {
                var exceptionManager = new ExceptionsManager();
                var exceptionHistManager = new ExceptionHistManager();

                // These exception lists are already filtered by A230 and Err_Response = 'X'
                var exceptionList = exceptionManager.GetExceptionsByRRId(vmRR.RR_ID).ToList();
                IEnumerable<Data.ExceptionHist> exceptionHistList = Enumerable.Empty<Data.ExceptionHist>();
                if (exceptionList.Count() == 0)
                {
                    exceptionHistList = exceptionHistManager.GetExceptionHistListByRRId(vmRR.RR_ID).ToList();
                }


                // Pseudo code checks Exceptions table first, then if count == 0 checks ExceptionHist.
                // Current Vitap DEV DB contains no records with A230 and Err_Response = 'X', this is a temporary state,
                // may have to create state for testing.
                if (exceptionList.Count() > 0 || exceptionHistList.Count() > 0)
                {
                    vmRR.ViewRRRejectButtonDisabled = true;
                    vmRR.ViewRRRejectButtonText = "Un-Reject RR";
                }
                else
                {
                    // COOP role is eliminated
                    // Enabled is FTS for 2C or HC docs
                    // ImageBatch is no longer used.
                    if (new List<string> { "2C", "HC" }.Contains(vmRR.RR_ID.Substring(0, 2)) && userAssignedService == "FTS")
                    {
                        vmRR.ViewRRRejectButtonDisabled = false;
                        vmRR.ViewRRRejectButtonText = "Un-Reject RR";
                        vmRR.ViewRRRejectButtonAction = ControllerAction.Exception.UnReject;
                    }
                    else
                    {
                        vmRR.ViewRRRejectButtonDisabled = true;
                        vmRR.ViewRRRejectButtonText = "Un-Reject RR";
                    }
                }
            }
            else if (vmRR.Status.Contains("EXCEPTION") || "ROUTE/INPEG/OUTBOX/CANCELLED/PROCESSED/IMGREJECT".Split('/').Contains(vmRR.Status))
            {
                vmRR.ViewRRRejectButtonDisabled = true;
                vmRR.ViewRRRejectButtonText = "Reject RR";
            }
            else if ("KEYED/RE-VERIFY/VERIFY/PREOUT".Split('/').Contains(vmRR.Status))
            {
                if (new List<string> { "2C", "HC" }.Contains(vmRR.RR_ID.Substring(0, 2)) && userAssignedService == "FTS")
                {
                    vmRR.ViewRRRejectButtonDisabled = false;
                    vmRR.ViewRRRejectButtonText = "Reject RR";
                    vmRR.ViewRRRejectButtonAction = ControllerAction.Exception.Reject;
                }
                else
                {
                    vmRR.ViewRRRejectButtonDisabled = true;
                    vmRR.ViewRRRejectButtonText = "Reject RR";
                }
            }
            else
            {
                vmRR.ViewRRRejectButtonDisabled = true;
                vmRR.ViewRRRejectButtonText = "Reject RR";
            }

            // Check if exception is being worked - This was only checked after they clicked Reject/Unreject button
            // so only looking for working exceptions if button is enabled.
            if (vmRR.ViewRRRejectButtonDisabled == false)
            {
                var exceptionManager = new ExceptionsManager();
                var workingExceptionList = exceptionManager.GetWorkingExceptionsByRRId(vmRR.RR_ID).ToList();
                if (workingExceptionList.Where(x => x.OUT == "T").Count() > 0)
                {
                    // Disable button
                    vmRR.ViewRRRejectButtonDisabled = true;

                    // Update Button Text.
                    vmRR.ViewRRRejectButtonText += " - Being Worked";
                    vmRR.ViewRRRejectButtonAction = "";
                }
            }

        }

        //// This is just a copy of ViewRR. What's up with that???
        //public ActionResult ReceivingReport(string rr_Id)
        //{
        //    // Get RR Accounting
        //    var rrAcctManager = new ReceivingReportAccountingManager();
        //    var rrAcctList = rrAcctManager.GetRRAccountingById(rr_Id).ToList();

        //    ViewBag.RRAccounting = rrAcctList;
        //    ViewBag.RejectButtonDisabled = true;
        //    ViewBag.RejectButtonText = "Reject";

        //    ViewBag.RREdi = GetRREdiListById(rr_Id);

        //    var rrManager = new ReceivingReportManager();
        //    var rrList = rrManager.GetRRById(rr_Id).ToList();
        //    if (rrList.Count() == 0)
        //        return GetIndexView();

        //    // Decide Reject Button Text and Disabled
        //    var exceptionsManager = new ExceptionsManager();
        //    var exceptionsList = exceptionsManager.GetExceptionsByRRId(rr_Id).ToList();
        //    var hasExceptions = exceptionsList.Count() > 0;

        //    var rr = rrList.Single();
        //    rr.RR_STATUS = rr.RR_STATUS.ToUpper();
        //    SetRejectButtonForRR(rr.RR_ID, rr.RR_STATUS, hasExceptions);

        //    return View(rr);
        //}

        #endregion Receiving Report

        #region View EDI RR

        public ActionResult EdiRr(string rr_Id)
        {
            var version = "";
            var bkey = "";
            var status = "";
            GetEdiHeaderInfo(rr_Id, ref version, ref bkey, ref status);

            var header = GetEdiHeader(version, bkey, rr_Id, status);
            var details = GetEdiDetails(version, bkey, rr_Id);
            var vmEdiRR = new ReceivingReportEdiModel(header, details);

            return View("EdiRr", vmEdiRR);
        }

        private void GetEdiHeaderInfo(string rrId, ref string version, ref string bkey, ref string status)
        {
            var mgrRR = new ReceivingReportManager();
            var uidy = "";

            var rrFrm = mgrRR.GetRRFrm(rrId);
            if (rrFrm != null)
            {
                uidy = rrFrm.INBOX_UIDY;
                status = rrFrm.RR_STATUS;
            }
            else
            {
                var rr = mgrRR.GetRR(rrId);
                if (rr != null)
                {
                    uidy = rr.INBOX_UIDY;
                    status = rr.RR_STATUS;
                }
            }

            var uidyParts = uidy.Split('&');
            if (uidyParts == null || uidyParts.Length < 2)
                return;
            version = uidyParts[1];
            if (uidyParts.Length < 3)
                return;
            bkey = uidyParts[2];
        }

        private ReceivingReportEdiHeaderModel GetEdiHeader(string version, string bkey, string rrId, string status)
        {
            var mgrRR = new ReceivingReportManager();
            var vmEdiHeader = new ReceivingReportEdiHeaderModel();

            if (version == InboxVersion.V007)
            {
                var header = mgrRR.GetV7RRHeader(bkey);
                vmEdiHeader = ReceivingReportEdiHeaderModel.MapToViewModel(header, rrId, status);
            }
            else if (version == InboxVersion.V008)
            {
                var header = mgrRR.GetV8RRHeader(bkey);
                vmEdiHeader = ReceivingReportEdiHeaderModel.MapToViewModel(header, rrId, status);
            }
            else if (version == InboxVersion.V009)
            {
                var header = mgrRR.GetV9RRHeader(bkey);
                vmEdiHeader = ReceivingReportEdiHeaderModel.MapToViewModel(header, rrId, status);
            }

            return vmEdiHeader;
        }

        private List<ReceivingReportEdiDetailModel> GetEdiDetails(string version, string bkey, string rrId)
        {
            var mgrRR = new ReceivingReportManager();
            var list = new List<ReceivingReportEdiDetailModel>();

            if (version == InboxVersion.V007)
            {
                var details = mgrRR.GetV7RRDetails(bkey);
                foreach (var detail in details)
                {
                    var vm = ReceivingReportEdiDetailModel.MapToViewModel(detail, rrId);
                    list.Add(vm);
                }
            }
            else if (version == InboxVersion.V008)
            {
                var details = mgrRR.GetV8RRDetails(bkey);
                foreach (var detail in details)
                {
                    var vm = ReceivingReportEdiDetailModel.MapToViewModel(detail, rrId);
                    list.Add(vm);
                }
            }
            else if (version == InboxVersion.V009)
            {
                var details = mgrRR.GetV9RRDetails(bkey);
                foreach (var detail in details)
                {
                    var vm = ReceivingReportEdiDetailModel.MapToViewModel(detail, rrId);
                    list.Add(vm);
                }
            }

            return list;
        }

        #endregion View EDI RR

        [HttpPost]
        public ActionResult Select(IEnumerable<TransHist> TransHistList, string id)
        {
            return GetIndexView();
        }

        #region ListInv
        /// <summary>
        /// Query Invoices from VIATP PegasysInvoices table
        /// and refered Itemized Invoices from Pegasys tables.
        /// Preapre 2 Lists to support the 2 Grids in the ListInv View
        /// </summary>
        /// <param name="recurring_id"></param>
        /// <param name="pdocno"></param>
        /// <param name="actNumber"></param>
        /// <param name="inv_key_id"></param>
        /// <returns></returns>
        public ActionResult ListInv(string recurring_id, string pdocno, string actNumber, string inv_key_id)
        {
            var tempInvoiceList = new List<InvoiceListModel>();
            var curInvoiceList = new List<InvoiceListModel>();
            var allInvoiceList = new List<InvoiceListModel>();

            try
            {
                ListInvManager ListInvManager = new ListInvManager();

                // Unit Test
                //actNumber = "21240357";
                //pdocno = "2B21240357";

                tempInvoiceList = ListInvManager.GetInvoiceList(recurring_id, pdocno, actNumber, inv_key_id);

                // Check Invoice Keyed ID in order to preapre cuurent Invoice List
                if (!string.IsNullOrWhiteSpace(inv_key_id))
                {
                    curInvoiceList = ListInvManager.GetInvoiceByInvoiceKeyID(tempInvoiceList, inv_key_id);
                    allInvoiceList = ListInvManager.GetAllInvoiceFromTempInvoiceList(tempInvoiceList, inv_key_id);
                }
                else
                {
                    allInvoiceList = tempInvoiceList;
                }

                // Assign Invoice Lists and Page Title
                ViewBag.CurrentInvoiceList = curInvoiceList;
                ViewBag.AllInvoiceList = allInvoiceList;
                ViewBag.Title = ListInvManager.ViewCaption;

                return View();
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/ViewInv:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "ViewEA");
                ViewBag.Message = "System error encountered.";
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View("ViewErrorMessage");
            }



        }
        #endregion

        #region ListEA
        /// <summary>
        /// List Expense Accrual screen is called from TransHist screen.  It receives ACT, AC_ID, r_Pegasys, and pdocno parameters. 
        /// It call ListEAManager to preapre ListEA data. 
        /// </summary>
        /// <param name="act"></param>                
        /// <param name="pdocno"></param>
        /// <returns></returns>
        public ActionResult ListEA(string act, string pdocno)
        {
            //pdocno = "2BA200208120229";

            try
            {
                var EAList = new List<ExpenseAccrualModel>();

                ListEAManager ListEAManager = new ListEAManager();

                //EAList = ListEAManager.GetExpenseAccrualListByPegDocNum(pDocNoPO); // for unit test
                if (pdocno.HasValue())
                {
                    EAList = ListEAManager.GetExpenseAccrualListByPegDocNum(pdocno);
                }
                else
                {
                    if (act.HasValue())
                    {
                        EAList = ListEAManager.GetExpenseAccrualListByACTNumnber(act);
                    }
                }
                ViewBag.AEIDColumnHeader = ListEAManager.GetACIDColumnHeader(EAList);
                ViewBag.TransHistButtonStatus = ListEAManager.TransHistButtonStatus;
                //ViewBag.NEARButtonStatus = ListEAManager.NEARButtonStatus;
                ViewBag.ViewEAButtonStatus = ListEAManager.ViewEAButtonStatus;
                //ViewBag.ListUEButtonStatus = ListEAManager.ListUEButtonStatus;

                // Combined Accruals - available if non-PBS and pdocno set.
                ViewBag.CombinedAccrualsButtonStatus = (pdocno.HasValue() && AssignSrv != "PBS" ? ListEAManager.CombinedAccrualsButtonStatus : "disabled");
                ViewBag.pdocno = pdocno;

                return View(EAList);
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/ViewEA:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "ViewEA");
                ViewBag.Message = "System error encountered.";
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View("ViewErrorMessage");
            }


        }

        [HttpPost]
        public ActionResult ListEA([DataSourceRequest]DataSourceRequest request)
        {
            return View();
        }
        #endregion

        #region ListPA
        /// <summary>
        /// List Payment Authorization screen is called from TransHist screen.  It receives ACT, and pdocno parameters. 
        /// It calls PaymentAuthManager to preapre ListPA data. 
        /// </summary>
        /// <param name="act"></param>                
        /// <param name="pdocno"></param>
        /// <returns></returns>
        public ActionResult ListPA(string act, string pdocno)
        {
            //pdocno = "2BA200208120229";

            var PAList = new ListPaymentAuthModel();

            PaymentAuthManager paymentAuthManager = new PaymentAuthManager();

            var PAList1 = new List<PaymentAuthorization>();
            decimal Total = 0;

            PAList1 = paymentAuthManager.GetVitapPaymentAuthByAct(act);

            if (PAList1 == null || PAList1.Count == 0)
            {
                if (pdocno.HasValue())
                {
                    PAList1 = paymentAuthManager.GetPegasysPaymentAuthByPDocNo(pdocno);
                }
                else
                {
                    if (act.HasValue())
                    {
                        PAList1 = paymentAuthManager.GetPegasysPaymentAuthByAct(act);
                    }
                }
            }

            if (PAList1 != null && PAList1.Count > 0)
            {
                foreach (var row in PAList1)
                {
                    if (row.PA_STATUS == "INPEG")
                    {
                        var paStatus = paymentAuthManager.GetStatus(row.PA_ID);
                        if (!String.IsNullOrWhiteSpace(paStatus))
                        {
                            row.PA_STATUS = paStatus;
                        }
                    }
                    Total += row.AMOUNT;
                }
            }
            PAList.Payments = PAList1;
            PAList.Total = Total;

            return View(PAList);
        }

        #endregion 

        #region ListPO
        public ActionResult ListPO(string act, string po_id, string pdocno, string recur_id)
        {
            //pdocno = "2B21221978";
            //recur_id = "1B9N01166";
            //recur_id = "ARC86C9490";
            try
            {                
                ListPOManager ListPO = new ListPOManager();

                act = act.ToLower() == "null" ? "" : act.Trim();
                po_id = po_id.Trim();
                pdocno = pdocno.Trim();
                recur_id = recur_id.Trim();

                var ListPOResult = ListPO.GetPegasysPurchaseOrderList(act, po_id, pdocno, recur_id);

                return View(ListPOResult);
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/ListPO:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "ListPO");
                ViewBag.Message = "System error encountered.";
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View("ViewErrorMessage");
            }

        }
        #endregion

        #region ListRR

        public ActionResult ListRR(string recur_Id, string act, string pDocNo)
        {
            var listRRORA = GetRRs(recur_Id, act, pDocNo);
            if (listRRORA == null)
                return View();

            return View(listRRORA);
        }

        private ListRR_ORA GetRRs(string recur_Id, string act, string pDocNo)
        {
            ListRR_ORA model = new ListRR_ORA();
            var listRRManager = new ListRRManager();
            bool updateStatusByException = false;

            if (!string.IsNullOrEmpty(recur_Id))
                model.listrrora = GetRRsByRecurId(recur_Id);
            else if (!string.IsNullOrEmpty(act) && string.IsNullOrEmpty(pDocNo))
                model.listrrora = GetRRsByAct(act);
            else if (!string.IsNullOrEmpty(pDocNo))
                model.listrrora = GetRRsByPDocNo(pDocNo, ref updateStatusByException);

            if (model.listrrora.Count() > 0)
                UpdateListRRsByPegasysRR(model.listrrora);
            if (updateStatusByException)
                UpdateListRRStatusByExceptions(model.listrrora);

            model.RRLABEL = "All RR";

            return model;
        }

        private List<ListRR> GetRRsByRecurId(string recur_Id)
        {
            var listRRManager = new ListRRManager();
            List<ListRR> listRR = new List<ListRR>();
            List<ListRR> listPegRR = new List<ListRR>();
            List<ListRR> listVitapRR = new List<ListRR>();
            List<ListRR> listTempRR = new List<ListRR>();

            listRR = listRRManager.GetListRRByRecur_IDFromVitap(recur_Id).ToList();
            listPegRR = listRRManager.GetListRRByRecur_IDFromPegasysNonItem(recur_Id).ToList();

            if (!listPegRR.Equals(null) && listPegRR.Count > 0)
            {
                foreach (var item in listPegRR)
                {
                    listRR.Add(item);
                    listTempRR.Add(item);
                }
            }

            return listRR;
        }

        private List<ListRR> GetRRsByAct(string act)
        {
            var listRRManager = new ListRRManager();
            List<ListRR> listRR = new List<ListRR>();
            List<ListRR> listPegRR = new List<ListRR>();

            listRR = listRRManager.GetListRRByActFromVitap(act).ToList();
            listPegRR = listRRManager.GetListRRByActFromPegasys(act).ToList();

            if (!listPegRR.Equals(null) && listPegRR.Count > 0)
            {
                listPegRR = listRRManager.GetListRRPegasysByRRID(listPegRR);

                foreach (var item in listPegRR)
                {
                    listRR.Add(item);
                }
            }
            return listRR;
        }

        private List<ListRR> GetRRsByPDocNo(string pDocNo, ref bool updateStatusByException)
        {
            var listRRManager = new ListRRManager();
            List<ListRR> listRR = new List<ListRR>();
            List<ListRR> listPegRR = new List<ListRR>();
            List<ListRR> listMatchAERC = new List<ListRR>();

            listRR = listRRManager.GetListRRByPDocNoFromVitap(pDocNo).ToList();
            listPegRR = listRRManager.GetListRRByPDocNoFromPegasys1(pDocNo).ToList();

            if (listPegRR.Equals(null) || listPegRR.Count < 1)
            {
                listPegRR = listRRManager.GetListRRByPDocNoFromPegasys2(pDocNo).ToList();
            }

            if (!listPegRR.Equals(null) && listPegRR.Count > 0)
            {
                listPegRR = listRRManager.GetListRRPegasysByRRID(listPegRR);

                foreach (var item in listPegRR)
                {
                    listRR.Add(item);
                }
            }

            listMatchAERC = listRRManager.GetListRRByPDocNoFromPegasys3(pDocNo).ToList();
            if (listMatchAERC.Equals(null) || listMatchAERC.Count() < 1)
            {
                listMatchAERC = listRRManager.GetListRRByPDocNoFromPegasys4(pDocNo).ToList();
            }

            List<ListRR> matchTemp = new List<ListRR>();
            if (!listMatchAERC.Equals(null) && listMatchAERC.Count > 0)
            {
                listMatchAERC = listRRManager.GetListRRMatchAERCByRRID(listMatchAERC);
                foreach (var itemMatch in listMatchAERC)
                {
                    matchTemp = (from m in listRR
                                 where m.RR_ID == itemMatch.RR_ID
                                 select m).ToList();

                    if (matchTemp.Equals(null) || matchTemp.Count == 0)
                    {
                        listRR.Add(itemMatch);
                    }
                }
            }
            updateStatusByException = true;

            return listRR;
        }

        // Update the imagebatch, imageid, rr_status, and edi_ind from PegasysRR table
        private List<ListRR> UpdateListRRsByPegasysRR(List<ListRR> listRRs)
        {
            var listRRIds = listRRs.Select(r => r.RR_ID).ToArray();

            var pegasysRRManager = new PegasysRRManager();
            var pegasysRRList = pegasysRRManager.GetPegasysRRsByRR_Ids(listRRIds).ToList();
            if (pegasysRRList.Count() > 0)
            {
                foreach (var rr in listRRs)
                {
                    var pegasysRR = pegasysRRList.SingleOrDefault(r => r.RR_ID == rr.RR_ID);
                    if (pegasysRR != null)
                    {
                        rr.IMAGEBATCH = pegasysRR.IMAGEBATCH;
                        rr.IMAGEID = pegasysRR.IMAGEID;
                        rr.EDI_IND = pegasysRR.EDI_IND;
                    }
                }
            }
            Session[SessionKey.ListRRs] = listRRs;
            return listRRs;
        }

        // If the rr_status is “EXCEPTION”, it updates err_code from Exceptions table
        private List<ListRR> UpdateListRRStatusByExceptions(List<ListRR> listRRs)
        {
            var listRRActId = listRRs.Where(r => r.RR_STATUS == "EXCEPTION").ToList();
            var exceptionManager = new ExceptionsManager();
            if (!listRRActId.Equals(null) && listRRActId.Count > 0)
            {
                var exceptionList = exceptionManager.GetExceptionsByActRRId(listRRActId).ToList();
                if (exceptionList.Count() > 0)
                {
                    foreach (var rr in listRRs)
                    {
                        var exception = exceptionList.SingleOrDefault(e => e.ACT == rr.ACT && e.RR_ID == rr.RR_ID);
                        if (exception != null)
                        {
                            rr.RR_STATUS = exception.ERR_CODE;
                        }
                    }
                }
            }
            Session[SessionKey.ListRRs] = listRRs;
            return listRRs;
        }

        #endregion ListRR

        public ActionResult ViewInvoice(string inv_Key_Id, string voldr, string NoteValue, string modelStatus)
        {
            //clear any previous invoices that were cached
            Session["currentInvoice"] = null;

            RoleListModel roleModel = new RoleListModel();

            if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. Redirect to the login page.
                return RedirectToAction("Index", "Login");
            }

            var model = new InvoiceModel();
            model.ModelStatus = modelStatus;
            model.Voldr = !string.IsNullOrEmpty(voldr) ? bool.Parse(voldr) : false;
            model.InvKeyId = inv_Key_Id;

            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            model.UserAssignedService = string.Empty;

            if (roleList != null)
            {
                model.UserAssignedService = roleList.ASSIGN_SRV;
            }

            BindInvoiceModel(model);
            Session["ViewInvoiceModel"] = model;

            return View(model);
        }

        [HttpPost]
        public ActionResult ViewInvoice(InvoiceModel model)
        {
            var modelOriginal = Session["ViewInvoiceModel"] as InvoiceModel;

            model.PrepCode = base.PrepCode;

            var mgr = new TransHistManager();
            if (model.Command == "viewImage")
            {
                return RedirectToAction("ViewEDIInvoice", new { id = model.InvoiceDocNumber });
            }
            else if (model.Command == "viewTJ")
            {
                return RedirectToAction("ViewTj", new { pDocNo = model.PegDocNoInv });
            }
            else if (model.Command == "clearRR")
            {
                mgr.ClearMismatchInvoice(modelOriginal, PrepCode);
            }
            else if (model.Command == "clearPO")
            {
                //ClearPOInvoice(model);
                Session["model"] = model;
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Add Notes",
                    Question = "Do you want to add notes to popup on the R200?",
                    Origin = "ClearPOInvoice",
                    Controller = "Transhist",
                    ReturnAction = "ClearPOInvoiceMessage",
                    ReturnController = "Transhist",
                    ExId = " ",
                    ErrCode = modelOriginal.ErrorCode,
                };

                return RedirectToAction("MessageYesNoFromTranshist", "Exceptions", vmConfirm);
            }
            else if (model.Command == "viewDeMemo")
            {
                mgr.SetDEMemo(model);
            }
            else if (model.Command == "reject")
            {
                model.PromptForFaxSend = "true";
            }
            else if (model.Command == "unreject")
            {
                Session["currentInvoice"] = model;

                return RedirectToAction("UnrejectShowAddtionalNotes", new { inv_Key_Id = model.InvKeyId });
            }
            else if (model.Command == "sendFax")
            {
                Session["currentInvoice"] = model;
                return RedirectToAction("RejectShowFaxNotesStep2", new { inv_Key_Id = model.InvKeyId });
            }
            else if (model.Command == "addNotes")
            {
                Session["currentInvoice"] = model;
                return RedirectToAction("RejectShowAddtionalNotes", new { inv_Key_Id = model.InvKeyId });
            }

            BindInvoiceModel(model);
            return View(model);
        }

        public ActionResult ClearPOInvoiceMessage(MessageDisplay vmConfirm)
        {
            var modelOriginal = Session["ViewInvoiceModel"] as InvoiceModel;
            var model = Session["model"] as InvoiceModel;
            var mgr = new TransHistManager();

            if (vmConfirm.Response)
            {

                var vmMandMemo = new MandMemoViewModel()
                {
                    Origin = Url.Action("viewInvoice", "transHist") + "?inv_Key_Id=" +
                        model.InvKeyId + "&modelStatus=Reject Cancelled!",
                    Title = "Notes for R200:",
                    NoteValue = "",
                    ReturnAction = "ClearPOInvoiceMandMemo",
                    ReturnController = "TransHist",
                    OptionalParam = model.InvKeyId
                };

                return RedirectToAction("MandMemoFromTranshist", "Exceptions", vmMandMemo);
            }
            else
            {
                mgr.ClearPOTransHist(model, PrepCode, "Clear PO Link Notes - No notes", "Clear PO Link");

            }
            mgr.ClearPOFinalize(model, modelOriginal, PrepCode);
            return RedirectToAction("ViewInvoice", new { inv_Key_Id = modelOriginal.InvKeyId });
        }
        public ActionResult ClearPOInvoiceMandMemo(MandMemoViewModel vmMandMemo)
        {
            var modelOriginal = Session["ViewInvoiceModel"] as InvoiceModel;
            var model = Session["model"] as InvoiceModel;
            var mgr = new TransHistManager();
            if (vmMandMemo.NoteValue == "CANCEL")
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "Clear PO Link Cancelled!",
                    Origin = "ClearPOInvoiceMandMemo",
                    Controller = "Transhist",
                    ReturnAction = "Index",
                    ReturnController = "Transhist",
                    ExId = " ",
                    ErrCode = modelOriginal.ErrorCode,
                };

                return RedirectToAction("MessageOkFromTranshist", "Exceptions", vmMandMemo);
            }
            else
            {
                var DEnotes = vmMandMemo.NoteValue.ReplaceApostrophes();
                mgr.ClearPOTransHist(modelOriginal, PrepCode, "Clear PO Link Notes - " + DEnotes, "Clear PO Link Notes");
            }

            mgr.ClearPOFinalize(model, modelOriginal, PrepCode);
            return RedirectToAction("ViewInvoice", new { inv_Key_Id = modelOriginal.InvKeyId });
        }

        public ActionResult ViewEDIInvoice(string id)
        {
            var model = new EDIInvoiceModel();

            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }

            model.RInvKeyId = id.Trim();
            BindEDIInvoiceModel(model);
            
            return View(model);
        }

        [HttpPost]
        public ActionResult ViewEDIInvoice(EDIInvoiceModel model)
        {
            BindEDIInvoiceModel(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult ViewEDIPO(InboxPermPOHModel model)
        {
            return View(model);
        }

        public ActionResult ViewEstimatedAccrual(string ae_Id)
        {

            ae_Id = FixTextNullAndWhiteSpace(ae_Id);
            var model = new EstimatedAccrualModel();

            model = BindEstimatedAccrualModel(ae_Id);
            return View(model);
        }
        
        public void BindInvoiceModel(InvoiceModel model)
        {
            model.InvoiceAccountingModel = new InvoiceAccountingModel();
            model.InvoiceDescriptionModel = new InvoiceDescriptionModel();
            model.InvoiceItemsModel = new InvoiceItemModel();

            model.Tab1Name = "Invoice Desc";

            var mgr = new TransHistManager();
            model = mgr.FetchInvoice(model);

            if (!string.IsNullOrEmpty(model.Closed))
            {
                model.Closed = string.Format("{0:c2}", double.Parse(model.Closed));
            }

            if (!string.IsNullOrEmpty(model.AppliedCredits))
            {
                model.AppliedCredits = string.Format("{0:c2}", double.Parse(model.AppliedCredits));
            }

            if (!string.IsNullOrEmpty(model.Net))
            {
                model.Net = string.Format("{0:c2}", double.Parse(model.Net));
            }

            if (!string.IsNullOrEmpty(model.Outstanding))
            {
                model.Outstanding = string.Format("{0:c2}", double.Parse(model.Outstanding));
            }

            if (!string.IsNullOrEmpty(model.InvoicedAmount))
            {
                model.InvoicedAmount = string.Format("{0:c2}", double.Parse(model.InvoicedAmount));
            }

            if (!string.IsNullOrEmpty(model.Freight))
            {
                model.Freight = string.Format("{0:c2}", double.Parse(model.Freight));
            }

            if (!string.IsNullOrEmpty(model.TaxAmount))
            {
                model.TaxAmount = string.Format("{0:c2}", double.Parse(model.TaxAmount));
            }
        }

        public void BindEDIInvoiceModel(EDIInvoiceModel model)
        {
            model.EDIInvoiceDescriptionModel = new EDIInvoiceDescriptionModel();
            model.EDIInvoiceDetailModel = new EDIInvoiceDetailModel();

            if (!string.IsNullOrEmpty(model.RInvKeyId))
            {
                var manager = new TransHistManager();
                model = manager.FetchEDIInvoice(model);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public EstimatedAccrualModel BindEstimatedAccrualModel(string aeId)
        {            
            var aeModel = new EstimatedAccrualModel();
            var mgr = new TransHistManager();

            if (!string.IsNullOrEmpty(aeId))
            {
                aeModel = mgr.FetchEstimatedAccrual(aeId);
            }
            return aeModel;
        }

        public void BindNovationHistoryModel(NovationHistoryModel model)
        {
            var mgr = new TransHistManager();

            model.NovationHistoryVendorModel = new NovationHistoryVendorModel();
            model.NovationHistoryDesignatedAgentModel = new NovationHistoryDesignatedAgentModel();

            mgr.FetchNovationHistoryDisplayStatus(model);
        }

        public ActionResult ViewNovationHistory(string docNum, string uidy)
        {
            var model = new NovationHistoryModel();
            model.DocNumber = docNum;
            model.Uidy = uidy;
            BindNovationHistoryModel(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult ViewNovationHistory(NovationHistoryModel model)
        {
            BindNovationHistoryModel(model);
            return View(model);
        }

        public ActionResult ViewRecurringMaster(string recur_Id, string uidy, string source)
        {
            var pDocNoPO = FixTextNullAndWhiteSpace(recur_Id);
            uidy = FixTextNullAndWhiteSpace(uidy);
            source = FixTextNullAndWhiteSpace(source);

            if (!string.IsNullOrEmpty(uidy))
            {
                uidy = uidy.Replace("__", "&");
            }

            var model = new RecurringMasterModel();

            model = BindRecurringMasterModel(model, pDocNoPO, uidy, source);
            BindViewXXButton(model.DocumentModels);

            return View(model);
        }

        private void BindViewXXButton(List<RecurringMasterDocumentModel> docModel)
        {
            RecurringMasterDocumentModel doc = docModel.FirstOrDefault();
            string docType = doc.DocType;
            string buttonName = string.Empty;
            string buttonAction = string.Empty;
            string buttonId = string.Empty;

            if (string.IsNullOrEmpty(docType))
                return;
            switch (docType.Trim().ToUpper())
            {
                case "RO":
                    buttonName = "View PO Document";
                    buttonAction = "ViewPO";
                    buttonId = "po_Id=" + doc.PDN;
                    break;
                case "RA":
                    buttonName = "View EA Document";
                    buttonAction = "ViewEAs";
                    buttonId = "ae_Id=" + doc.PDN;
                    break;
                case "RT":
                    buttonName = "View RR Document";
                    buttonAction = "ViewRR";
                    buttonId = "rr_Id=" + doc.PDN;
                    break;
                default:
                    buttonName = "View XX Document";
                    break;
            }
            ViewBag.ViewXXButtonName = buttonName;
            ViewBag.ViewXXButtonAction = buttonAction;
            ViewBag.ViewXXButtonId = buttonId;
        }

        public RecurringMasterModel BindRecurringMasterModel(RecurringMasterModel model, string pDocNoPO, string uidy, string source)
        {
            var mgr = new RecurringMasterManager();

            var main = GetRecurringMasterMain(pDocNoPO);

            model.MainModel = main;
            model.LeaseChainModels = mgr.GetLeaseChains(main.Uidy);

            var chainUidy = source == "lc" ? uidy : model.LeaseChainModels.Select(chain => chain.Uidy).FirstOrDefault();

            GetVendorByChainUidy(model, chainUidy);
            GetDocTemplateLeaseAmountByChainUidy(model, chainUidy);

            var uidyDocTemplate = source == "dt" ? uidy : model.DocTemplateModels.Select(doc => doc.Uidy).FirstOrDefault();

            GetDocumentFrequencyByDocTemplateUidy(model, uidyDocTemplate);

            return model;
        }

        private void GetVendorByChainUidy(RecurringMasterModel model, string chainUidy)
        {
            var mgr = new RecurringMasterManager();
            var vendorInfo = mgr.GetVendorInfoFromVend(chainUidy);
            if (vendorInfo != null)
            {
                // Get Vendor Address Info
                var vUidy = UidyPrefix.E1250 + "&" + vendorInfo.VEND_ID + "&" + vendorInfo.VEND_ADDR_CD;
                var vendorAddressInfo = mgr.GetVendorAddrInfo(vUidy);
                model.VendorInfoModel = vendorAddressInfo;

                // Get Remit Vendor Address Info
                var rUidy = UidyPrefix.E1250 + "&" + vendorInfo.VEND_ID + "&" + vendorInfo.RMIT_TO_ADDR_CD;
                var remitVendorAddressInfo = mgr.GetRemitVendorAddrInfo(rUidy);
                model.RemitanceInfoModel = remitVendorAddressInfo;

                // Get Dggt Vendor Address Info
                var dUidy = UidyPrefix.E1250 + "&" + vendorInfo.DGGT_ID + "&" + vendorInfo.DGGT_ADDR_CD;
                var dggtVendorAddressInfo = mgr.GetDggtVendorAddrInfo(dUidy);
                model.DesignatedInfoModel = dggtVendorAddressInfo;
            }


        }

        private void GetDocTemplateLeaseAmountByChainUidy(RecurringMasterModel model, string chainUidy)
        {
            var mgr = new RecurringMasterManager();
            var docTemplate = mgr.GetDocTemplate(chainUidy);
            var leaseAmounts = mgr.GetLeaseAmounts(chainUidy);
            model.LeaseAmountModels = leaseAmounts;
            model.DocTemplateModels = docTemplate;
        }

        private void GetDocumentFrequencyByDocTemplateUidy(RecurringMasterModel model, string uidyDocTemplate)
        {
            var mgr = new RecurringMasterManager();
            var documents = mgr.GetDocument(uidyDocTemplate);
            var frequencies = mgr.GetFrequency(uidyDocTemplate);
            model.DocumentModels = documents;
            model.FrequencyModels = frequencies;
        }

        private RecurringMasterMainModel GetRecurringMasterMain(string pDocNoPO)
        {
            var mrg = new RecurringMasterManager();
            var model = mrg.GetMain(pDocNoPO);

            if (!string.IsNullOrEmpty(model.SecurityOrg) && model.SecurityOrg.Split('&').Length > 1)
            {
                model.SecurityOrg = model.SecurityOrg.Split('&')[2];
            }

            var vName = mrg.GetVName_VEND(pDocNoPO);
            if (!string.IsNullOrWhiteSpace(vName))
            {
                model.Name = vName;
            }
            return model;
        }

        public ActionResult ViewPAs(string pa_id)
        {
            if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. Redirect to the login page.
                return RedirectToAction("Index", "Login");
            }


            var model = new PaymentAuthorizationModel();
            model.PaId = pa_id;
            BindPaymentAuthorizationModel(model);
            return View("ViewPaymentAuthorization", model);
        }

        public void BindPaymentAuthorizationModel(PaymentAuthorizationModel model)
        {
            model.DescriptionModel = new PaymentAuthorizationDisbursementDescriptionModel();
            model.AccountingLineModel = new PaymentAuthorizationDisbursementAccountingLineModel();
            model.SummaryModel = new PaymentAuthorizationDisbursementSummaryModel();

            var manager = new TransHistManager();
            manager.FetchPaymentAuthorization(model);
        }

        public ActionResult ExpenseAccrualEDIInformation(string id)
        {
            var model = new ExpenseAccrualEDIInformationModel();
            model.AeId = id;

            var mgr = new TransHistManager();
            model = mgr.FetchExpenseAccrualEDIInformation(model);
            return View(model);
        }

        [HttpPost]
        public ActionResult PaymentAuthorizationAccountingRecords([DataSourceRequest]DataSourceRequest request, PaymentAuthorizationModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchPaymentAuthorizationAccountingTab(search).AsQueryable();
            var result = items.AsQueryable().ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult PaymentAuthorizationSummaryRecords([DataSourceRequest]DataSourceRequest request, PaymentAuthorizationModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchPaymentAuthorizationSummaryTab(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult NovationHistoryVendorRecords([DataSourceRequest]DataSourceRequest request, NovationHistoryModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchNovationHistoryVendorTab(search);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult NovationHistoryDesignatedAgentRecords([DataSourceRequest]DataSourceRequest request, NovationHistoryModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchNovationHistoryDesignatedAgentTab(search);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult InvoiceItemRecords([DataSourceRequest]DataSourceRequest request, InvoiceModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchInvoiceItemsTab(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            if (items.Count() == 0)
            {
                result = new List<InvoiceItemModel>().ToDataSourceResult(request);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult InvoiceAccountingRecords([DataSourceRequest]DataSourceRequest request, InvoiceModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchInvoiceAccountingTab(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            if (items.Count() == 0)
            {
                result = new List<InvoiceAccountingModel>().ToDataSourceResult(request);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult ExpenseAccrualEdiRecords([DataSourceRequest]DataSourceRequest request, ExpenseAccrualEDIInformationModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchExpenseAccrualEdiRecords(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }        

        [HttpPost]
        public ActionResult EDIInvoiceDetailRecords([DataSourceRequest]DataSourceRequest request, EDIInvoiceDetailModel search)
        {
            var mgr = new TransHistManager();
            var items = mgr.FetchEDIInvoiceDetailTab(search.BKEY);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        
        [HttpPost]
        public ActionResult RecurringMasterDocumentRecords([DataSourceRequest]DataSourceRequest request, RecurringMasterDocumentModel search)
        {
            var items = GetDummyRecurringMasterDocumentTemplateRecords(search).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search.Amount))
                items = items.Where(x => x.Amount == search.Amount);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<RecurringMasterDocumentModel> GetDummyRecurringMasterDocumentTemplateRecords(RecurringMasterDocumentModel search)
        {
            var items = new List<RecurringMasterDocumentModel>()
                {
                    new RecurringMasterDocumentModel
                    {
                        Amount= "111"
                    },
                     new RecurringMasterDocumentModel
                    {
                      Amount= "111"
                    },
                    new RecurringMasterDocumentModel
                    {
                     Amount= "111"
                    }
               };
            return items;
        }

        [HttpPost]
        public ActionResult RecurringMasterFrequencyRecords([DataSourceRequest]DataSourceRequest request, RecurringMasterFrequencyModel search)
        {
            var items = GetDummyRecurringMasterFrequencyRecords(search).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search.Frequency))
                items = items.Where(x => x.Frequency == search.Frequency);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<RecurringMasterFrequencyModel> GetDummyRecurringMasterFrequencyRecords(RecurringMasterFrequencyModel search)
        {
            var items = new List<RecurringMasterFrequencyModel>()
                {
                    new RecurringMasterFrequencyModel
                    {
                        Frequency= "111"
                    },
                     new RecurringMasterFrequencyModel
                    {
                      Frequency= "111"
                    },
                    new RecurringMasterFrequencyModel
                    {
                     Frequency= "111"
                    }
               };
            return items;
        }

        //JUN MERGE 10.17
        public ActionResult Match(string act, string pDocNo)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            List<PegasysInvoice> matches = new List<PegasysInvoice>();
            PegasysInvoiceManager pegasysInvoiceManager = new PegasysInvoiceManager();

            if (string.IsNullOrEmpty(pDocNo))
            {
                matches = pegasysInvoiceManager.GetTransHistMatchByAct(act).ToList();
            }
            else
            {
                matches = pegasysInvoiceManager.GetTransHistMatchByPDocNo(pDocNo).ToList();
            }

            return View(matches);
        }

        public ActionResult Misc(string act, string pDocNo)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);

            MiscDocsManager miscDocsManager = new MiscDocsManager();
            List<MiscDocs> listMiscDocs = new List<MiscDocs>();
            if (!string.IsNullOrEmpty(act) && !string.IsNullOrEmpty(pDocNo))
            {
                listMiscDocs = miscDocsManager.GetMiscDocsByActPDocNo(act, pDocNo).ToList();
            }
            else if (!string.IsNullOrEmpty(act))
            {
                listMiscDocs = miscDocsManager.GetMiscDocsByAct(act).ToList();
            }
            else
            {
                listMiscDocs = miscDocsManager.GetMiscDocsByPDocNo(pDocNo).ToList();
            }

            return View("Misc", listMiscDocs);
        }

        public ActionResult OpenItems(string act, string pDocNo)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);
            string searchBy = "One";

            if (!string.IsNullOrEmpty(pDocNo) && !string.IsNullOrEmpty(act) && act.Length == 8)
            {
                searchBy = "Both";
            }

            TransHistOpenItemManager transHistOpenItemManager = new TransHistOpenItemManager();
            List<TransHistOpenItem> openItems = new List<TransHistOpenItem>();

            if (!string.IsNullOrEmpty(pDocNo))
            {
                if (pDocNo.ToUpper().StartsWith("2I"))
                {
                    openItems = transHistOpenItemManager.GetTransHistOpenItemsByPDocNo1(pDocNo).ToList();
                }
                else
                {
                    openItems = transHistOpenItemManager.GetTransHistOpenItemsByPDocNo2(pDocNo).ToList();
                }

                if (openItems.Count() < 1)
                {
                    if (searchBy == "Both")
                    {
                        openItems = transHistOpenItemManager.GetTransHistOpenItemsByACT1(act, pDocNo).ToList();
                        if (openItems.Count() < 1)
                        {
                            openItems = transHistOpenItemManager.GetTransHistOpenItemsByACT2(act).ToList();
                        }
                    }
                }
            }
            else
            {
                openItems = transHistOpenItemManager.GetTransHistOpenItemsByACT2(act).ToList();
            }

            return View(openItems);
        }

        #region Overview

        public ActionResult Overview(string act, string pDocNo)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);
            ViewBag.POBalance = 0.0m;
            ViewBag.OverViewRRBalance = 0.0m;
            ViewBag.InvoiceBalance = 0.0m;

            var pegInvoices = new List<PegasysInvoice>();

            // Looks like we're not doing anything if there's no Pegasys PO.
            var overviewPO = GetOverviewPO(pDocNo, act);
            if (overviewPO == null)
                return View("Overview", pegInvoices);

            pegInvoices = GetOverviewInvoices(pDocNo, act);
            GetOverviewRRs(pDocNo, act, overviewPO.UIDY);

            return View("Overview", pegInvoices);
        }

        private OverviewPO GetOverviewPO(string pDocNo, string act)
        {
            var overviewPO = new OverviewPO();
            if (!string.IsNullOrEmpty(pDocNo))
                overviewPO = GetOverviewPOByPDocNo(pDocNo);
            else if (!string.IsNullOrEmpty(act))
                overviewPO = GetOverviewPOByAct(pDocNo, act);

            if (overviewPO != null)
            {
                ViewBag.POBalance = (decimal)(overviewPO.ORDD_AM - overviewPO.CLSD_AM);
            }

            return overviewPO;
        }

        private OverviewPO GetOverviewPOByPDocNo(string pDocNo)
        {
            var overviewPOManager = new OverviewPOManager();
            var listOverviewPO = overviewPOManager.GetPOByPDocNoFromMf_Io(pDocNo).ToList();
            if (listOverviewPO.Count() == 0)
                listOverviewPO = overviewPOManager.GetPOByPDocNoFromMf_Tg(pDocNo).ToList();
            if (listOverviewPO.Count() == 0)
                listOverviewPO = overviewPOManager.GetPOByPDocNoFromPegasysPo_Frm(pDocNo).ToList();

            return listOverviewPO.FirstOrDefault();
        }

        private OverviewPO GetOverviewPOByAct(string pDocNo, string act)
        {
            var overviewPOManager = new OverviewPOManager();
            var listOverviewPO = new List<OverviewPO>();

            if (!string.IsNullOrEmpty(pDocNo))
                listOverviewPO = overviewPOManager.GetPOByPDocNoFromMf_Io_1BK(pDocNo).ToList();
            else if (!string.IsNullOrEmpty(act))
                listOverviewPO = overviewPOManager.GetPOByActFromMf_Io(act).ToList();
            if (listOverviewPO.Count() > 0)
                return listOverviewPO.FirstOrDefault();

            if (!string.IsNullOrEmpty(pDocNo))
                listOverviewPO = overviewPOManager.GetPOByPDocNoFromMf_Tg_1BK(pDocNo).ToList();
            else
                listOverviewPO = overviewPOManager.GetPOByActFromMf_Tg(act).ToList();
            if (listOverviewPO.Count() > 0)
                return listOverviewPO.FirstOrDefault();

            // Ugh, talk about a misnomer...
            listOverviewPO = overviewPOManager.GetPOByActFromPegasysPo_Frm(act).ToList();

            return listOverviewPO.FirstOrDefault();
        }

        private List<PegasysInvoice> GetOverviewInvoices(string pDocNo, string act)
        {
            var pegInvoices = new List<PegasysInvoice>();
            var pegasysInvoiceManager = new PegasysInvoiceManager();

            if (!string.IsNullOrEmpty(pDocNo))
                pegInvoices = pegasysInvoiceManager.GetPegasysInvoiceByPDocNo(pDocNo).ToList();
            else
                pegInvoices = pegasysInvoiceManager.GetPegasysInvoiceByAct(act).ToList();

            if (pegInvoices != null)
            {
                ViewBag.InvoiceBalance = pegInvoices.Sum(i => i.AMOUNT);
            }

            return pegInvoices;
        }

        private void GetOverviewRRs(string pDocNo, string act, string uidy)
        {
            var overviewRRs = new List<OverviewRR>();
            var overviewRRManager = new OverviewRRManager();

            if (string.IsNullOrEmpty(uidy))
                overviewRRs = overviewRRManager.GetOverviewRRByAct(pDocNo, act).ToList();
            else
            {
                overviewRRs = overviewRRManager.GetOverviewRRByUidy1(uidy).ToList();
                if (overviewRRs.Count() < 1)
                    overviewRRs = overviewRRManager.GetOverviewRRByUidy2(uidy).ToList();
                if (overviewRRs.Count() < 1)
                    overviewRRs = overviewRRManager.GetOverviewRRByUidy3(uidy).ToList();
                if (overviewRRs.Count() < 1)
                    overviewRRs = overviewRRManager.GetOverviewRRByUidy4(uidy).ToList();
            }

            ViewBag.OverviewRR = overviewRRs;
            if (overviewRRs != null)
            {
                ViewBag.OverViewRRBalance = overviewRRs.Sum(i => i.UNCLEARED_AM);
            }

        }

        #endregion Overview

        [HttpGet]
        public ActionResult SummNotes(string act, string pDocNo)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);
            if (!string.IsNullOrEmpty(pDocNo) && pDocNo.Length == 3)
                pDocNo = string.Empty;

            SummaryNoteManager summaryNoteManager = new SummaryNoteManager();
            List<SummaryNote> lstSummaryNote = null;

            if (!string.IsNullOrEmpty(pDocNo))
            {
                lstSummaryNote = summaryNoteManager.GetSummaryNoteByPDocNo(pDocNo).ToList();
                ViewBag.Caption = "TransHist : Summary Notes for PDocNo" + pDocNo;

                if (lstSummaryNote.Count() > 0)
                    return View("SummNotes", lstSummaryNote);

                if (!string.IsNullOrEmpty(act))
                {
                    lstSummaryNote = summaryNoteManager.GetSummaryNoteByACT(act).ToList();
                    ViewBag.Caption = "Transhist : Summary Notes for Act " + act;
                    return View("SummNotes", lstSummaryNote);
                }
            }
            if (!string.IsNullOrEmpty(act) && (act.Length == 8))
            {
                lstSummaryNote = summaryNoteManager.GetSummaryNoteByACT(act).ToList();
                ViewBag.Caption = "Transhist : Summary Notes for Act " + act;
            }
            return View("SummNotes", lstSummaryNote);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SummNotes(string act, string pDocNo, SummaryNote note)
        {
            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);

            SummaryNoteManager summaryNoteManager = new SummaryNoteManager();
            SummaryNote summaryNote = new SummaryNote();

            summaryNote.SN_ID = "SN" + DateTime.Now.Year + DateTime.Now.ToString("MM") + DateTime.Now.ToString("dd");
            summaryNote.SN_ID += summaryNoteManager.GetThenExtVal().PadLeft(4, '0');
            summaryNote.ACT = note.ACT;
            summaryNote.TRANSDATE = DateTime.Now;
            summaryNote.SN_MEMO = note.SN_MEMO;

            if (!string.IsNullOrEmpty(pDocNo) && pDocNo.StartsWith("1B"))
            {
                summaryNote.PDOCNO = "1" + note.ACT;
            }
            else
            {
                summaryNote.PDOCNO = note.PDOCNO;
            }

            summaryNote.PREPCODE = PrepCode;

            summaryNoteManager.InsertSummaryNote(summaryNote);


            List<SummaryNote> listSummartyNote = null;

            if (!string.IsNullOrEmpty(pDocNo))
            {
                listSummartyNote = summaryNoteManager.GetSummaryNoteByPDocNo(pDocNo).ToList();
                if (listSummartyNote.Count() < 1 && !string.IsNullOrEmpty(act))
                {
                    listSummartyNote = summaryNoteManager.GetSummaryNoteByACT(act).ToList();
                }
            }
            else if (!string.IsNullOrEmpty(act) && (act.Length == 8))
            {
                listSummartyNote = summaryNoteManager.GetSummaryNoteByACT(act).ToList();
            }

            return View("SummNotes", listSummartyNote);
        }

        private string FixTextNullAndWhiteSpace(string needToBeFixed)
        {
            return (string.IsNullOrWhiteSpace(needToBeFixed) || (needToBeFixed == "null")) ? string.Empty : needToBeFixed.Trim();
        }

        public ActionResult ViewPO(string po_Id)
        {

            try
            {
                if (string.IsNullOrWhiteSpace(po_Id))
                {
                    ViewBag.Message = "No po_id was passed in";
                    return View("ViewErrorMessage");
                }

                var mgr = new POManager();

                var model = mgr.GetPO(po_Id);
                if (model.Dscr == null)
                {
                    model.Dscr = new ViewPODscModel();
                    model.Dscr.DSCR = "";
                    model.Dscr.CCR = "";
                }
                if (!String.IsNullOrWhiteSpace(model.INBOXUIDY))
                {
                    model.ViewImageButtonEnabled = true;
                }
                if (model == null)
                {
                    ViewBag.Message = "No data was found for the selected po_id: " + po_Id;
                    return View("ViewErrorMessage");
                }
                return View("ViewPO", model);
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/ViewEA:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "ViewEA");
                ViewBag.Message = "System error encountered.";
                // Manually log error to ELMAH
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return View("ViewErrorMessage");
            }


        }

        private string GetDepartmentForViewPO(string po_Id)
        {
            var department = "NA";

            var pegasysPO_FrmManager = new PegasysPO_FrmManager();
            var pegasysPO_Frms = pegasysPO_FrmManager.GetPegaysPO_FrmByPoId(po_Id);
            if (pegasysPO_Frms != null)
                return GetDepartmenFromImageBatch(pegasysPO_Frms.IMAGEBATCH);

            var pegasysPOManager = new PegasysPOManager();
            var pegasysPOs = pegasysPOManager.GetPegaysPOByPoId(po_Id);
            if (pegasysPOs != null)
                department = GetDepartmenFromImageBatch(pegasysPOs.IMAGEBATCH);

            return department;
        }

        private string GetDepartmenFromImageBatch(string imageBatch)
        {
            var department = "PBS";
            if (string.IsNullOrEmpty(imageBatch))
                return department;

            imageBatch = imageBatch.Trim();
            if (imageBatch.StartsWith("P6"))
                department = "FTS";

            return department;
        }

        public ActionResult ViewTj(string pDocNo)
        {
            TransHistTjManager transHistTjManager = new TransHistTjManager();
            var transHistTjs = transHistTjManager.GetTransHistTjs(pDocNo).ToList();
            return View(transHistTjs);
        }

        public ActionResult ViewTjWithAE(string pDocNo, string aeId)
        {
            TransHistTjManager transHistTjManager = new TransHistTjManager();
            var transHistTjs = transHistTjManager.GetTransHistTjs(pDocNo, aeId).ToList();
            return View("ViewTj", transHistTjs);
        }

        public string RREdiById(string id)
        {
            var pegasysRR = GetRREdiListById(id).Single();
            return pegasysRR.IMAGEID;
        }

        public List<PegasysRR> GetRREdiListById(string id)
        {
            var mgrPegasysRR = new PegasysRRManager();
            var lstPegasysRR = mgrPegasysRR.GetRREdiById(id).ToList();
            return lstPegasysRR;
        }

        public ActionResult Contact(string pDocNo, string act)
        {
            var roleModel = RoleList;
            if (roleModel == null || !roleModel.HasMgtReportsRole)
            {
                RedirectToAction("Index", "Home");
            }

            act = FixTextNullAndWhiteSpace(act);
            pDocNo = FixTextNullAndWhiteSpace(pDocNo);

            var contactPegPos = new List<ContactPegPo>();
            var contacts = new List<Contact>();

            if (!string.IsNullOrWhiteSpace(pDocNo) && !pDocNo.ToUpper().StartsWith("VP"))
            {
                contacts = GetContact(pDocNo, act);
                if (contacts.Count() >= 1)
                    return GetContactView(contacts);
            }
            if (contacts.Count() == 0 && act.Length == 8)
            {
                contacts = GetContact(act);
                return GetContactView(contacts);
            }

            return GetContactView(contacts);
        }

        public ActionResult ContactDetails()
        {
            return View();
        }

        private ActionResult GetContactView(List<Contact> contacts)
        {
            if (contacts.Count() > 0)
            {
                var contact = contacts.FirstOrDefault();
                return Json(new
                {
                    success = true,
                    HasRecord = true,
                    Name = contact.Name,
                    Phone = contact.Phone,
                    Fax = contact.Fax,
                    OrgCode = contact.OrgCode,
                    Fund = contact.Fund,
                    Region = contact.Region,
                    BA = contact.BA
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                success = true,
                HasRecord = false,
                Name = string.Empty,
                Phone = string.Empty,
                Fax = string.Empty,
                OrgCode = string.Empty,
                Fund = string.Empty,
                Region = string.Empty,
                BA = string.Empty
            }, JsonRequestBehavior.AllowGet);
        }

        private List<Contact> GetContact(string pDocNo, string act)
        {
            var lstContact = new List<Contact>();

            var contactPegPos = GetPoInfo(pDocNo, act);

            //if (contactPegPos.Count() == 0)
            //    return lstContact;

            lstContact = GetCurMFord(pDocNo, contactPegPos);

            if (lstContact.Count() > 0)
            {

                var contact = lstContact.FirstOrDefault();
                string orgCode = FixTextNullAndWhiteSpace(contact.OrgCode);
                string ba = FixTextNullAndWhiteSpace(contact.BA);
                ba = ba.Right(2);

                lstContact = GetCurContact(orgCode, ba);
                if (lstContact.Count() < 1)
                    lstContact = GetCurContact(orgCode);
            }
            return lstContact;
        }

        private List<Contact> GetContact(string act)
        {
            var lstContact = new List<Contact>();

            string curPDocNo = GetCurPo(act).Trim();
            if (string.IsNullOrWhiteSpace(curPDocNo))
                return lstContact;

            var contactPegPos = GetPoInfo(curPDocNo, act);

            if (contactPegPos.Count() == 0)
                return lstContact;

            lstContact = GetCurMFord(contactPegPos);

            if (lstContact.Count() == 0)
                return lstContact;

            var contact = lstContact.FirstOrDefault();
            string orgCode = FixTextNullAndWhiteSpace(contact.OrgCode);
            string ba = FixTextNullAndWhiteSpace(contact.BA);
            ba = ba.Right(2);

            lstContact = GetCurContact(orgCode, ba);
            if (lstContact.Count() < 1)
                lstContact = GetCurContact(orgCode);

            return lstContact;
        }

        private string GetCurPo(string act)
        {
            var mrg = new ContactPOManager();
            var curPos = mrg.GetContactPOByActFromMf_IoTg(act);
            return curPos.Count() == 0 ? string.Empty : curPos.FirstOrDefault().DOC_NUM;
        }

        private List<Contact> GetCurMFord(string pDocNo, List<ContactPegPo> contactPegPos)
        {
            var rtn = contactPegPos.Where(c =>
                                               !(c.ORGCODE == null || c.ORGCODE.Trim() == string.Empty) ||
                                               !(c.BA_PROG == null || c.BA_PROG.Trim() == string.Empty)
                                   )
                                   .Select(c => new Contact { OrgCode = c.ORGCODE, BA = c.BA_PROG })
                                   .ToList();

            if (rtn.Count() > 0)
                return rtn;

            using (var context = new OracleVitapContext())
            {
                rtn = context.PEGASYSPOACCT_FRM
                                    .Where(po => po.PO_ID == pDocNo)
                                    .OrderBy(po => po.LNUM)
                                    .Select(c => new Contact { OrgCode = c.ORGCODE, BA = c.BA_PROG })
                                    .ToList();
                return rtn;
            }
        }

        private List<Contact> GetCurMFord(List<ContactPegPo> contactPegPos)
        {
            var rtn = contactPegPos.Where(c =>
                                               !(c.ORGCODE == null || c.ORGCODE.Trim() == string.Empty) ||
                                               !(c.BA_PROG == null || c.BA_PROG.Trim() == string.Empty)
                                   )
                                   .Select(c => new Contact { OrgCode = c.ORGCODE, BA = c.BA_PROG })
                                   .ToList();
            return rtn;
        }

        private List<Contact> GetCurContact(string orgCode, string ba)
        {
            using (var context = new OracleVitapContext())
            {
                var rtn = context.CONTACTs
                                .Where(c => c.ORGCODE == orgCode && c.BA == ba)
                                .Select(s => new Contact
                                {
                                    Name = s.CONTACTNAME,
                                    Phone = s.PHONE,
                                    Fax = s.FAX,
                                    OrgCode = s.ORGCODE,
                                    BA = s.BA.ToUpper().StartsWith("AL") ? "ALL" : s.BA.ToUpper(),
                                    Region = s.REGION,
                                    Fund = s.FUND
                                }).ToList();
                return rtn;
            }
        }

        private List<Contact> GetCurContact(string orgCode)
        {
            using (var context = new OracleVitapContext())
            {
                var rtn = context.CONTACTs
                                .Where(c => c.ORGCODE == orgCode)
                                .Select(s => new Contact
                                {
                                    Name = s.CONTACTNAME,
                                    Phone = s.PHONE,
                                    Fax = s.FAX,
                                    OrgCode = s.ORGCODE,
                                    BA = s.BA.ToUpper().StartsWith("AL") ? "ALL" : s.BA.ToUpper(),
                                    Region = s.REGION,
                                    Fund = s.FUND
                                }).ToList();
                return rtn;
            }
        }

        private List<ContactPegPo> GetPoInfo(string pDocNo, string act)
        {
            var contactPegPoManager = new ContactPegPoManager();
            var contactPegPo = new List<ContactPegPo>();

            var contactPOsFromMf_Io = GetContactPOsFromMf_Io(pDocNo, act);
            if (contactPOsFromMf_Io.Count() > 0)
            {
                var uidy = contactPOsFromMf_Io.First().UIDY;
                contactPegPo = contactPegPoManager.GetContactPegPOByUidyMf_Io1(uidy).ToList();
                if (contactPegPo.Count() < 1)
                    contactPegPo = contactPegPoManager.GetContactPegPOByUidyMf_Io2(uidy).ToList();
                return contactPegPo;
            }

            var varContacPOsFromMf_Tg = GetContactPOsFromMf_Tg(pDocNo, act);

            if (varContacPOsFromMf_Tg.Count() > 0)
                contactPegPo = contactPegPoManager.GetContactPegPOByUidyMf_Tg1(varContacPOsFromMf_Tg.First().UIDY).ToList();

            return contactPegPo;
        }

        private List<ContactPO> GetContactPOsFromMf_Io(string pDocNo, string act)
        {
            var contactPOManager = new ContactPOManager();
            List<ContactPO> contactPOsFromMf_Io;

            if (string.IsNullOrEmpty(pDocNo))
                contactPOsFromMf_Io = contactPOManager.GetContactPOByActFromMf_Io(act).ToList();
            else
            {
                int n;
                bool isNumeric = int.TryParse(pDocNo.Substring(1, 1), out n) && int.TryParse(pDocNo.Left(1), out n);
                if (!isNumeric)
                    contactPOsFromMf_Io = contactPOManager.GetContactPOByUidyFromMf_Io(pDocNo).ToList();
                else
                    contactPOsFromMf_Io = contactPOManager.GetContactPOByPDocNoFromMf_Io(pDocNo).ToList();
            }
            return contactPOsFromMf_Io;
        }

        private List<ContactPO> GetContactPOsFromMf_Tg(string pDocNo, string act)
        {
            var contactPOManager = new ContactPOManager();
            List<ContactPO> contactPOsFromMf_Tg;

            if (string.IsNullOrEmpty(pDocNo))
                contactPOsFromMf_Tg = contactPOManager.GetContactPOByActFromMf_Tg(act).ToList();
            else
                contactPOsFromMf_Tg = contactPOManager.GetContactPOByPDocNoFromMf_Tg(pDocNo).ToList();

            return contactPOsFromMf_Tg;
        }

        public ActionResult ViewEDIPO(string Act, string ModNo, string InboxUidy, string Po_Id)
        {
            var poEdiManager = new POEdiManager();
            var pegasysPOList = new List<PoEdiMfIoModel>();
            var inboxHeaderData = new InboxPermPOHModel();
            var inboxDetailData = new List<InboxPermPOAModel>();
            string strMessage = "", strDoc_Num = "", strDoc_Type = "", strDoc_Stus = "";

            if (String.IsNullOrEmpty(InboxUidy))
            {
                //Lookup InboxUidy
                var poManager = new PegasysPOManager();
                var pegasysPo = poManager.GetPegaysPOByPoId(Po_Id);
                if (pegasysPo != null)
                {
                    InboxUidy = pegasysPo.INBOX_UIDY;
                }
            }

            if (String.IsNullOrEmpty(Act))
            {
                strMessage = "EDI Information Not Found. The PO could have been directly keyed into Pegasys";
                ViewBag.Message = strMessage;
                return View("ViewErrorMessage");
            }
            pegasysPOList = poEdiManager.getPOEDIData(Act, Po_Id);

            if (pegasysPOList.Count == 0)
            {
                if (strMessage.Equals(string.Empty))
                {
                    strMessage = "No Pegasys PO data was found";
                }
                ViewBag.Message = strMessage;
                return View("ViewErrorMessage");
            }
            else
            {
                //Mark the Reason label and edit box invisible initially. If Reason is populated, it sets the value and makes it visible below
                strDoc_Num = pegasysPOList[0].Doc_Num;
                strDoc_Type = pegasysPOList[0].Doc_Num.Substring(0, 2);
                strDoc_Stus = pegasysPOList[0].Doc_Stus;
            }

            inboxHeaderData = poEdiManager.getPOEDIHeaderData(Act, ModNo, InboxUidy, strDoc_Num, strDoc_Type, strDoc_Stus);

            if (inboxHeaderData == null)
            {
                inboxHeaderData = new InboxPermPOHModel();

                inboxHeaderData.DataMessage = "No inbox header data found.";

                inboxHeaderData.OfficeAddress = new OfficeAddress();
                inboxHeaderData.OfficeAddress.AddressName = "";
                inboxHeaderData.OfficeAddress.Address1 = "";
                inboxHeaderData.OfficeAddress.Address2 = "";
                inboxHeaderData.OfficeAddress.AddressCSZ = "";

                inboxHeaderData.VendorInfo = new VendorInfo();
                inboxHeaderData.VendorInfo.VendorInfoTIN = "";
                inboxHeaderData.VendorInfo.VendorInfoName = "";
                inboxHeaderData.VendorInfo.VendorInfoAddress = "";

                inboxHeaderData.Doc_Type = Po_Id.ReplaceNull("  ").Left(2);
                inboxHeaderData.Doc_Num = Po_Id;
                return View("ViewEDIPurchaseOrder", inboxHeaderData);
            }

            if (inboxHeaderData.PoTotl.HasValue())
            {
                inboxHeaderData.PoTotl = Decimal.Parse(inboxHeaderData.PoTotl).ToString("N2");
            }

            inboxHeaderData.RemitAddress = poEdiManager.setRemitAddress(inboxHeaderData);
            inboxHeaderData.OfficeAddress = poEdiManager.setOfficeAddress(inboxHeaderData);
            inboxHeaderData.VendorInfo = poEdiManager.setVendorInfo(inboxHeaderData);

            if (inboxHeaderData.BKey.HasValue() && !poEdiManager.BKey.HasValue())
            {
                poEdiManager.BKey = inboxHeaderData.BKey;
            }

            inboxHeaderData.Version = poEdiManager.strHeaderVersion;
            if (String.IsNullOrWhiteSpace(inboxHeaderData.ModNum))
            {
                inboxHeaderData.ModNum = "0000";
            }
            return View("ViewEDIPurchaseOrder", inboxHeaderData);
        }

        [HttpPost]
        public ActionResult EDIPODetailRecords([DataSourceRequest]DataSourceRequest request, InboxPermPOHModel search)
        {
            //Retrieve the Detail data from the permpoa tables (V005 or V006) and display in grid
            string strMessage = "";
            var poEdiManager = new Vitap.Data.Managers.POEdiManager();
            var result = new List<InboxPermPOAModel>();
            if (!search.Version.ReplaceNull("").ToString().Trim().Equals(string.Empty) && !search.BKey.ReplaceNull("").ToString().Trim().Equals(string.Empty))
            {
                result = poEdiManager.DependentValues(search.Version.ToString(), search.BKey.ToString());
            }
            var results = result.ToDataSourceResult(request);

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewEDIRR(string rrId)
        {
            var inboxHeaderData = new InboxPermRRHModel();
            var mgr = new RREdiManager();
            var rtnRRFrm = mgr.GetRRFrmData(rrId);
            var rtnRR = new PEGASYSRR();
            if (rtnRRFrm == null)
            {
                rtnRR = mgr.GetRRData(rrId);
            }
            
            var InboxUidy = "";
            if (rtnRRFrm != null)
            {
                InboxUidy = rtnRRFrm.INBOX_UIDY;
            }
            else if (rtnRR != null)
            {
                InboxUidy = rtnRR.INBOX_UIDY;
            }


            var inboxHeader = new InboxPermRRH();
            if (String.IsNullOrEmpty(InboxUidy))
            {
                inboxHeaderData = new InboxPermRRHModel();
            }
            else
            {
                inboxHeaderData = mgr.GetHeaderInfoByBKey(InboxUidy);
                inboxHeader.HeaderData = inboxHeaderData;
                inboxHeader.RRDetail = mgr.GetDetailData(InboxUidy);
            }

            inboxHeader.RR_ID = rrId;
            
            return View("ViewEDIReceivingReport", inboxHeader);
        }


        [HttpPost]
        public JsonResult RejectRR(string currentAction, string rrId, string notes, string faxNotes)
        {
            string statusMessage = "";

            if (string.IsNullOrWhiteSpace(rrId))
            {
                return Json(new
                {
                    success = false,
                    message = "RejectRR Failed - rrId was not received.",
                    rejectButtonText = string.Empty,
                    rejectButtonAction = string.Empty
                }, JsonRequestBehavior.AllowGet);
            }

            rrId = rrId.Trim();

            try
            {
                // Get this RR. No need to look in Pegasys because Reject button would not be enabled.
                var vmRR = null as PegasysRRModel;
                var mgrRR = new ReceivingReportManager();

                if (Session[SessionKey.Rrchoice] != null && ((PegasysRRModel)Session[SessionKey.Rrchoice]).RR_ID == rrId)
                {
                    vmRR = Session[SessionKey.Rrchoice] as PegasysRRModel;
                }
                else
                {
                    vmRR = mgrRR.GetVitapRR(rrId);
                }
                if (vmRR == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = vmRR.ViewRRRejectButtonAction + " Failed - No Receiving Report was found for rrId = " + rrId + ".",
                        rejectButtonText = string.Empty,
                        rejectButtonAction = string.Empty
                    }, JsonRequestBehavior.AllowGet);
                }

                // Reject this RR.
                vmRR.Notes = notes;
                vmRR.FaxNotes = faxNotes;
                var roleList = Session[SessionKey.RoleModel] as RoleListModel;
                if ((statusMessage = vmRR.RejectRR(roleList.PREPCODE)).Length > 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = statusMessage,
                        rejectButtonText = string.Empty,
                        rejectButtonAction = string.Empty
                    }, JsonRequestBehavior.AllowGet);
                }

                // Reload the updated RR.
                vmRR = mgrRR.GetVitapRR(rrId);
                Session[SessionKey.Rrchoice] = vmRR;

                // Reset page to new available action. Assuming enabled button remains the same.
                if (vmRR.Status == DataEntry.Reject || vmRR.Status == DataEntry.Rejected)
                {
                    vmRR.ViewRRRejectButtonAction = ControllerAction.Exception.UnReject;
                    vmRR.ViewRRRejectButtonText = "Un-Reject RR";
                }
                else
                {
                    vmRR.ViewRRRejectButtonAction = ControllerAction.Exception.Reject;
                    vmRR.ViewRRRejectButtonText = "Reject RR";
                }

                return Json(new
                {
                    success = true,
                    message = "RR has been " + currentAction + "ed.",
                    rejectButtonText = vmRR.ViewRRRejectButtonText,
                    rejectButtonAction = vmRR.ViewRRRejectButtonAction,
                    newStatus = vmRR.Status
                }, JsonRequestBehavior.AllowGet);
            }
            catch (System.Exception ex)
            {
                var msg = "Error in TransHist/RejectRR:\n\n\t" + ex.Message;
                Logging.AddWebError(msg, "TransHist", "RejectRR");
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return Json(new
                {
                    success = false,
                    message = "RejectRR Failed - Data exception.",
                    rejectButtonText = string.Empty,
                    rejectButtonAction = string.Empty
                }, JsonRequestBehavior.AllowGet);
            }
        }
        public ActionResult RejectShowFaxNotesStep2(string inv_Key_Id)
        {
            var model = Session["currentInvoice"] as InvoiceModel;

            //Display the MandMemo screen
            var title = "Email/Fax Notes:";
            var thismemo = model.RejectReason;
            var vmMandMemo = new MandMemoViewModel()
            {
                Origin = Url.Action("viewInvoice", "transHist") + "?inv_Key_Id=" + inv_Key_Id + "&modelStatus=Reject Canceled!",
                Title = title,
                NoteValue = thismemo,
                ReturnAction = "RejectShowFaxNotesStep2Return",
                ReturnController = "TransHist",
                OptionalParam = inv_Key_Id
            };

            return View("MandMemo", vmMandMemo);
        }

        public ActionResult RejectShowFaxNotesStep2Return(MandMemoViewModel vmMandMemo)
        {
            var currentInvoice = Session["currentInvoice"] as InvoiceModel;

            var transHistManager = new TransHistManager();
            transHistManager.RejectInvoice(currentInvoice);

            var item = new NOTIFICATION();
            item.ACT = currentInvoice.ACT;
            item.FAX_NOTES = vmMandMemo.NoteValue;
            item.INV_KEY_ID = currentInvoice.InvKeyId;
            item.DATEQUEUED = DateTime.Now;
            item.PREPCODE = currentInvoice.PrepCode;
            item.STATUS = "Pending";
            item.VENDNAME = currentInvoice.RemitVendorName;
            item.REPORT_ID = "P08";
            item.REPORTFORM = "PegInvReject";

            var mgr = new NotificationsManager();
            mgr.InsertNotification(item);

            return RedirectToAction("ViewInvoice", new { @modelStatus = "Invoice Rejected", inv_Key_Id = currentInvoice.InvKeyId });
        }

        public ActionResult RejectShowAddtionalNotes(string inv_Key_Id)
        {
            var model = Session["currentInvoice"] as InvoiceModel;

            var title = "You can type additional Notes here:";
            var thismemo = model.RejectReason;
            var vmMandMemo = new MandMemoViewModel()
            {
                Origin = Url.Action("viewInvoice", "transHist") + "?inv_Key_Id=" + inv_Key_Id + "&modelStatus=Reject Canceled!",
                Title = title,
                NoteValue = thismemo,
                ReturnAction = "RejectShowAddtionalNotesReturn",
                ReturnController = "TransHist",
                OptionalParam = inv_Key_Id
            };

            return View("MandMemo", vmMandMemo);
        }

        public ActionResult RejectShowAddtionalNotesReturn(MandMemoViewModel vmMandMemo)
        {
            var currentInvoice = Session["currentInvoice"] as InvoiceModel;

            var mgr = new TransHistManager();
            var item = new TRANSHIST();

            mgr.RejectInvoice(currentInvoice);

            item.ACT = currentInvoice.ACT;
            item.PDOCNO = currentInvoice.PegDocNoInv;
            item.INV_KEY_ID = currentInvoice.InvKeyId;
            item.TRANSDATE = DateTime.Now;
            item.PREPCODE = currentInvoice.PrepCode;
            item.CUFF_MEMO = "Reject from view screen - " + vmMandMemo.NoteValue;
            item.ALLPROCESS = "Reject";

            mgr.InsertTransHist(item);

            return RedirectToAction("ViewInvoice", new { @modelStatus = "Invoice rejected", inv_Key_Id = currentInvoice.InvKeyId });
        }

        public ActionResult UnrejectShowAddtionalNotes(string inv_Key_Id)
        {
            var title = "Note:";
            var thismemo = "UnReject Invoice - ";
            var vmMandMemo = new MandMemoViewModel()
            {
                Origin = Url.Action("viewInvoice", "transHist") + "?inv_Key_Id=" + inv_Key_Id + "&modelStatus=UnReject Canceled!",
                Title = title,
                NoteValue = thismemo,
                ReturnAction = "UnrejectShowAddtionalNotesReturn",
                ReturnController = "TransHist",
                OptionalParam = inv_Key_Id
            };

            return View("MandMemo", vmMandMemo);
        }

        public ActionResult UnrejectShowAddtionalNotesReturn(MandMemoViewModel vmMandMemo)
        {
            var currentInvoice = Session["currentInvoice"] as InvoiceModel;

            var mgr = new TransHistManager();
            var item = new TRANSHIST();

            mgr.UnrejectInvoice(currentInvoice);

            item.ACT = currentInvoice.ACT;
            item.PDOCNO = currentInvoice.PegDocNoInv;
            item.INV_KEY_ID = currentInvoice.InvKeyId;
            item.TRANSDATE = DateTime.Now;
            item.PREPCODE = currentInvoice.PrepCode;
            item.CUFF_MEMO = "Unreject from view screen - " + vmMandMemo.NoteValue;
            item.ALLPROCESS = "Uneject";

            mgr.InsertTransHist(item);

            return RedirectToAction("ViewInvoice", new { @modelStatus = "Invoice UnRejected!", inv_Key_Id = currentInvoice.InvKeyId });
        }

        public ActionResult GetAccrualsTocombine(string act, string pdocno)
        {

            var model = new CombineAccrualsManager().GetAccrualsInfo(pdocno, act);

                Session[SessionKey.CombineAccrualsModel] = model;
          

            return View("combineAccruals/CombinedAccruals", model);
        }

        public ActionResult combineAccruals(List<AccrualsGrid> combineAccrualsList)
        {
            var aeList = "";
            var noAEList = "";
            var model = Session[SessionKey.CombineAccrualsModel] as CombineAccrualsModel ?? null;
            decimal? combineAccrualsTotal = 0M;
            combineAccrualsList.ForEach(x => combineAccrualsTotal += x.AMT);

            if (combineAccrualsTotal < 0 && ((model.OriginalAccrualBalance ?? 0) < Math.Abs(Convert.ToDecimal(combineAccrualsTotal))))
            {              
                    return Json(new { success = true, Type = "error", Message = "Original Accrual does not have enough money available for accruals to combine." }, JsonRequestBehavior.AllowGet);
            }
            else if (model.POTabModel.POAmount < combineAccrualsTotal)
            {
                return Json(new { success = true, Type = "error", Message = "PO does not have enough money available for accruals to combine." }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                CombineAccrualsManager mgr = new CombineAccrualsManager();

                combineAccrualsList.ForEach(x => aeList += "','" + x.AE_ID);
                model.AccrualsNotCombined.ForEach(x =>
                {
                    if (!aeList.Contains(x.AE_ID))
                    {
                        noAEList += "','" + x.AE_ID;
                    }

                }
                );
                aeList = aeList.TrimStart('\'', ',') + "'";
                aeList = "'" + aeList;
                noAEList = noAEList.TrimStart('\'', ',') + "'";
                noAEList = "'" + noAEList;
                TempData["aeList"] = aeList;
                TempData["noAEList"] = noAEList;

                var result = mgr.CombineAccruals(aeList, noAEList, model, PrepCode);

                if (result.Count() > 0)
                {
                    if (result.ContainsKey("warning"))
                    {
                       if( result.ContainsKey("recycleAEEX"))
                        {
                            TempData["recycleAEEX"] = true;
                            TempData["recycle"] = true;
                        }
                       else if (result.ContainsKey("recycleAEEXNo"))
                        {
                            TempData["recycleAEEX"] = false;
                            TempData["recycle"] = true;
                        }
                            return Json(new { success = true, Type = "warning", Message = result["warning"] }, JsonRequestBehavior.AllowGet);
                    }
                    else if (result.ContainsKey("error"))
                    {
                        ViewBag.Message = result["error"];
                        return Json(new { success = true, Type = "error", Message = result["error"] }, JsonRequestBehavior.AllowGet);
                    }
                    else if (result.ContainsKey("success"))
                    {                       
                        return Json(new { success = true, Type = "success" }, JsonRequestBehavior.AllowGet);
                    }
                    else 
                    {
                        return Json(new { success = false, Type = "success" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    mgr.CombineAccrualsConfirmed(aeList, noAEList,false,false, model, PrepCode);
                    return Json(new { success = true, Type = "success" }, JsonRequestBehavior.AllowGet);
                }
            }

        }

        public ViewResult combineAccrualsShowError(string msg, string type)
        {
            if (type == "error")
            {
                ViewBag.Message = msg;
                return View("ViewErrorMessage");
            }
            else if (type == "warning")
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Combine Accruals",
                    Question = msg,
                    Origin = "CombineAccruals",
                    Controller = "TransHist",
                    ReturnAction = "CombineAccrualsConfirm",
                    ReturnController = "TransHist"
                };
                return View("ConfirmYesNo", vmConfirm);
            }
            else
            {
                var model = Session[SessionKey.CombineAccrualsModel] as CombineAccrualsModel ?? null;
                return View("combineAccruals/CombinedAccruals", model);
            }
        }

        public RedirectToRouteResult CombineAccrualsConfirm()
        {
            var model = Session[SessionKey.CombineAccrualsModel] as CombineAccrualsModel ?? null;
            var aeList = TempData.Peek("aeList") as string;
            var noAEList = TempData.Peek("noAEList") as string;

            bool isRecycle = TempData.Peek("recycle") as bool? ?? false;
            bool isAEEX = TempData.Peek("recycleAEEX") as bool? ?? false; 


             var msg= new CombineAccrualsManager().CombineAccrualsConfirmed(aeList, noAEList, isRecycle, isAEEX, model, PrepCode);

            if (string.IsNullOrEmpty(msg))
                return RedirectToAction("ListEA", new System.Web.Routing.RouteValueDictionary(
                                                             new { act = model.ACT, pDocNo = model.PDOCNO }));
            else
            {
                ViewBag.Message = msg;
                return RedirectToAction("combineAccrualsShowError", new System.Web.Routing.RouteValueDictionary(
                                                             new { msg = msg, type = "error" }));
            }
        }

    }
}

