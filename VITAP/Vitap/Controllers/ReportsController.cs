using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using VITAP.Models.Reports;
using System;
using VITAP.ViewModels.Reports;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using System.IO;
using System.Text;
using VITAP.Library.Strings;

namespace VITAP.Controllers
{
    public class ReportsController : VitapBaseController
    {
        #region Data Entry Productivity

        [HttpGet]
        public ActionResult DataEntryProductivityReport()
        {
            var model = new DataEntryProductivityReportViewModel();
            model.AssignedService = this.AssignSrv;

            // Default to a yesterday search.
            model.TransactionsForPeriodType = "1";

            return SearchRecords(model);
        }

        [HttpPost]
        public ActionResult DataEntryProductivityReport(DataEntryProductivityReportViewModel model)
        {
            return SearchRecords(model);
        }

        private ActionResult ProductivityResultsToFile(DataEntryProductivityReportViewModel model)
        {
            MemoryStream output = new MemoryStream();
            StreamWriter writer = new StreamWriter(output, Encoding.UTF8);

            // Service-specific formatting.
            string tops = "";
            string nontops = "";
            switch (model.AssignedService)
            {
                case "FTS":
                    tops = "tops";
                    nontops = "nontops";
                    break;
                case "PBS":
                    tops = "const";
                    nontops = "nonconst";
                    break;
                default:
                    tops = "tops_const";
                    nontops = "nontops_const";
                    break;
            }

            // Join indexed and keyed records.
            var leftOuterJoin = from index in model.IndexedRecords
                                //join keyed in model.KeyedRecords
                                //on index.PC equals keyed.PC
                                //into temp
                                //from keyed in temp.DefaultIfEmpty()
                                select new
                                {
                                    NAME = index.Name,
                                    pc = index.PC,
                                    po = index.PO,
                                    rr = index.RR,
                                    nontops = index.NonTops,
                                    vcpo = index.Vcpo,
                                    tops = index.Tops,
                                    misc = index.Misc,
                                    acrn = index.Acrn,
                                    a3881 = index.ThreeEightEightOne,
                                    bid = index.Bid,
                                    cncs = index.Cncs,
                                    jv = index.Jv,
                                    mipr = index.Mipr,
                                    ol = index.Outl,
                                    opac = index.Opac,
                                    pd = index.Pd,
                                    recy = index.RecY,
                                    rwa = index.Rwa,
                                    pdir = index.PDir,
                                    rei = index.Rei,
                                    noc = index.Noc,
                                    ipac = index.Ipac,
                                    itotal = index.Total,
                                    //kpo = (keyed != null ? keyed.PO : "0"),
                                    //krr = (keyed != null ? keyed.RR : "0"),
                                    //knontops = (keyed != null ? keyed.NonTops : "0"),
                                    //kvcpo = (keyed != null ? keyed.Vcpo : "0"),
                                    //ktops = (keyed != null ? keyed.Tops : "0"),
                                    //kmisc = (keyed != null ? keyed.Misc : "0"),
                                    //krwa = (keyed != null ? keyed.Rwa : "0"),
                                    //ktotal = (keyed != null ? keyed.Total : "0"),
                                    //Gtotal = (Int32.Parse(index.Total) + (keyed != null ? Int32.Parse(keyed.Total) : 0)).ToString(),
                                };
            var rightOuterJoin = from keyed in model.KeyedRecords
                                //join index in model.IndexedRecords
                                //on keyed.PC equals index.PC
                                //into temp
                                //from index in temp.DefaultIfEmpty()
                                select new
                                {
                                    NAME = keyed.Name,
                                    pc = keyed.PC,
                                    //po = (index != null ? index.PO : "0"),
                                    //rr = (index != null ? index.RR : "0"),
                                    //nontops = (index != null ? index.NonTops : "0"),
                                    //vcpo = (index != null ? index.Vcpo : "0"),
                                    //tops = (index != null ? index.Tops : "0"),
                                    //misc = (index != null ? index.Misc : "0"),
                                    //acrn = (index != null ? index.Acrn : "0"),
                                    //a3881 = (index != null ? index.ThreeEightEightOne : "0"),
                                    //bid = (index != null ? index.Bid : "0"),
                                    //cncs = (index != null ? index.Cncs : "0"),
                                    //jv = (index != null ? index.Jv : "0"),
                                    //mipr = (index != null ? index.Mipr : "0"),
                                    //ol = (index != null ? index.Outl : "0"),
                                    //opac = (index != null ? index.Opac : "0"),
                                    //pd = (index != null ? index.Pd : "0"),
                                    //recy = (index != null ? index.RecY : "0"),
                                    //rwa = (index != null ? index.Rwa : "0"),
                                    //pdir = (index != null ? index.PDir : "0"),
                                    //rei = (index != null ? index.Rei : "0"),
                                    //noc = (index != null ? index.Noc : "0"),
                                    //ipac = (index != null ? index.Ipac : "0"),
                                    //itotal = (index != null ? index.Total : "0"),
                                    kpo = keyed.PO,
                                    krr = keyed.RR,
                                    knontops = keyed.NonTops,
                                    kvcpo = keyed.Vcpo,
                                    ktops = keyed.Tops,
                                    kmisc = keyed.Misc,
                                    krwa = keyed.Rwa,
                                    ktotal = keyed.Total,
                                    //Gtotal = (Int32.Parse(keyed.Total) + (index != null ? Int32.Parse(index.Total) : 0)).ToString(),
                                };
            //var combinedResults = leftOuterJoin.Union(rightOuterJoin);

            // Indexed
            bool wroteHeader = false;
            foreach (var result in leftOuterJoin)
            {
                var type = result.GetType();
                var props = type.GetProperties();

                if (!wroteHeader)
                {
                    // Replace some column names based on service.
                    var keys = props.Select(x => (x.Name == "nontops" ? nontops : (x.Name == "knontops" ? "k" + nontops : (x.Name == "tops" ? tops : (x.Name == "ktops" ? "k" + tops : x.Name))))).ToArray();
                    writer.WriteLine("Indexed");
                    writer.Write(string.Join(",", keys));
                    writer.WriteLine();
                    wroteHeader = true;
                }
                var values = props.Select(x => x.GetValue(result, null)).ToArray();
                writer.Write(string.Join(",", values));
                writer.WriteLine();
            }
            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine();

            // Keyed
            wroteHeader = false;
            foreach (var result in rightOuterJoin)
            {
                var type = result.GetType();
                var props = type.GetProperties();

                if (!wroteHeader)
                {
                    // Replace some column names based on service.
                    var keys = props.Select(x => (x.Name == "nontops" ? nontops : (x.Name == "knontops" ? "k" + nontops : (x.Name == "tops" ? tops : (x.Name == "ktops" ? "k" + tops : x.Name))))).ToArray();
                    writer.WriteLine("Keyed");
                    writer.Write(string.Join(",", keys));
                    writer.WriteLine();
                    wroteHeader = true;
                }
                var values = props.Select(x => x.GetValue(result, null)).ToArray();
                writer.Write(string.Join(",", values));
                writer.WriteLine();
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/comma-separated-values", "DataEntryProductivityReport.csv");
        }

        private ActionResult SearchRecords(DataEntryProductivityReportViewModel model)
        {
            var mgr = new DataEntryProductivityReportManager(model.AssignedService);

            if (model.TransactionsForPeriodType != "Range")
            {
                model.ToDate = DateTime.Now;
                model.FromDate = model.ToDate.AddDays(-Int32.Parse(model.TransactionsForPeriodType));
            } 
            else 
            {
                // Add one day to the end of the range. Legacy does this.
                model.ToDate = model.ToDate.AddDays(1);
            }
            
            model.IndexedRecords = mgr.GetIndexedRecordsByDateRange(model.FromDate.ToString("MM/dd/yyyy"), model.ToDate.ToString("MM/dd/yyyy"));
            model.KeyedRecords = mgr.GetKeyedRecordsByDateRange(model.FromDate.ToString("MM/dd/yyyy"), model.ToDate.ToString("MM/dd/yyyy"));
            model.GrandTotalIndexed = mgr.indexTotal.ToString();
            model.GrandTotalKeyed = mgr.keyedTotal.ToString();
            model.GrandTotal = (mgr.indexTotal + mgr.keyedTotal).ToString();

            // Did the user ask to save to file?
            if (model.SaveToFile == "true")
            {
                // Return a CSV formatted file.
                return ProductivityResultsToFile(model);
            }
            
            // Fall-through (this was just a search).
            return View(model);
        }

        public ActionResult DataEntryProductivityReportKeyedIndexedDetail()
        {
            var mgr = new DataEntryProductivityReportManager(this.AssignSrv);
            var model = mgr.GetKeyedIndexedDetailReport();

            return View(model);
        }

        #endregion

        public ActionResult ContactReport()
        {
            if (Session[SessionKey.RoleModel] == null)
            {
                // No role model created yet. Redirect to the login page.
                return RedirectToAction("Index", "Login");
            }

            var mgr = new ReportManager();
            var model = new ContactReportViewModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult ContactReport(ContactReportViewModel model)
        {
            var mgr = new ReportManager();
            var items = mgr.FetchContactReport(model).AsQueryable();

            MemoryStream output = new MemoryStream();
            StreamWriter writer = new StreamWriter(output, Encoding.UTF8);
            
            writer.Write("ContactId,Region,Fund,OrgCode,BA,Name,Phone,Fax,Email");
            writer.WriteLine();

            writer.WriteLine();
            foreach (var item in items)
            {
                var format= "{0},\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",\"{8}\"";
                var formatted = string.Format(format, item.ContactId, item.Region, item.Fund, item.OrgCode, item.BA, item.Name, item.Phone, item.Fax, item.Email);
                writer.Write(formatted);
                writer.WriteLine();
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/comma-separated-values", "ContactReport.csv");
        }       

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Search()
        {
            return View();
        }

        #region Aged Exceptions

        [HttpGet]
        public ActionResult AgedExceptions(string ACT)
        {
            var model = new AgedExceptionsViewModel();

            var roleModel = this.RoleList;
            // Only allow those with the mgmt reports role.
            if (roleModel != null && roleModel.HasMgtReportsRole) model.IsManager = true;
            else return RedirectToAction("Index", "Login");
            model.PrepCode = this.PrepCode;

            return View("AgedExceptions", model);
        }

        [HttpGet]
        public ActionResult AgedExceptionsUser(string ACT)
        {
            var model = new AgedExceptionsViewModel();

            var roleModel = this.RoleList;
            model.IsManager = false;
            model.PrepCode = this.PrepCode;

            return View("AgedExceptions", model);
        }

        private ActionResult AgedExceptionsResultsToFile(AgedExceptionsViewModel model)
        {
            MemoryStream output = new MemoryStream();
            StreamWriter writer = new StreamWriter(output, Encoding.UTF8);
            var mgr = new AgedExceptionsManager();
            var results = mgr.GetAgedExceptionsOverThree(model.DateFrom, model.DateTo, model.PrepCode);

            bool wroteHeader = false;
            foreach (var result in results)
            {
                var type = result.GetType();
                var props = type.GetProperties();

                if (!wroteHeader)
                {
                    // Replace some column names based on service.
                    var keys = props.Select(x => x.Name).ToArray();
                    writer.Write(string.Join(";", keys));
                    writer.WriteLine();
                    writer.WriteLine();
                    wroteHeader = true;
                }
                var values = props.Select(x => x.GetValue(result, null)).ToArray();
                writer.Write(string.Join(";", values));
                writer.WriteLine();
            }

            writer.Flush();
            output.Position = 0;

            return File(output, "text/comma-separated-values", "AgedExceptionsUserReport.csv");
        }

        [HttpPost]
        public ActionResult AgedExceptions(AgedExceptionsViewModel model)
        {
            var roleModel = this.RoleList;
            // Only allow those with the mgmt reports role.
            if (roleModel != null && roleModel.HasMgtReportsRole) model.IsManager = true;
            else return RedirectToAction("Index", "Login");

            return AgedExceptionInternal(model);
        }

        [HttpPost]
        public ActionResult AgedExceptionsUser(AgedExceptionsViewModel model)
        {
            model.IsManager = false;
            model.PrepCode = this.PrepCode;

            return AgedExceptionInternal(model);
        }

        private ActionResult AgedExceptionInternal(AgedExceptionsViewModel model)
        {
            // Did the user ask to save to file?
            if (model.SaveToFile == "true")
            {
                // Return a CSV formatted file.
                return AgedExceptionsResultsToFile(model);
            }

            // Fall-through common path: search by date range and PrepCode.
            var mgr = new AgedExceptionsManager();
            model.AgedExceptionTotals = mgr.GetAgedExceptionTotals(model.DateFrom, model.DateTo, model.PrepCode).ToList();
            model.AgedExceptionSummaries = mgr.GetAgedExceptionSummary().ToList();

            // Reset the detail view.
            model.ErrCode = "";
            return View("AgedExceptions", model);
        }

        [HttpPost]
        public ActionResult AgedExceptionsDetail([DataSourceRequest]DataSourceRequest request, AgedExceptionsViewModel search)
        {
            var mgr = new AgedExceptionsManager();
            var items = mgr.FetchAgedExceptionsDetail(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion

        [HttpGet]
        public ActionResult Report_PoListing(string selectedRegion)
        {
            ReportPoListingManager manager = new ReportPoListingManager();
            var model = manager.BuildModel(selectedRegion);
            return View("Report_PoListing", model);
        }

        #region Preparer Code Productivity

        public ActionResult PreparerCodeProductivity()
        {
            return View();
        }

        [HttpPost]
        public ActionResult PreparerCodeProductivity([DataSourceRequest]DataSourceRequest request, PreparerCodeProductivitySearch search)
        {
            var items = GetDummyPrepCodeProductivityData(search).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search.PreparerCode))
                items = items.Where(I => I.PreparerCode == search.PreparerCode);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        private List<PreparerCodeProductivityItem> GetDummyPrepCodeProductivityData(PreparerCodeProductivitySearch search)
        {
            var items = new List<PreparerCodeProductivityItem>()
                {
                    new PreparerCodeProductivityItem
                    {
                        PreparerCode = "2H",
                        ExceptionCode = "P201",
                        Total = 20,
                        Reject= 1,
                        Accept = 5,
                        Skip = 1,
                        NextDay = 1,
                        DoqReq = 2,
                        Change = 5,
                        Other = 3,
                        Cleared = 2
                    },
                     new PreparerCodeProductivityItem
                    {
                        PreparerCode = "2H",
                        ExceptionCode = "R200",
                        Total = 25,
                        Reject= 2,
                        Accept = 12,
                        Skip = 2,
                        NextDay = 2,
                        DoqReq = 3,
                        Change = 4,
                        Other = 3,
                        Cleared = 2
                    },
                    new PreparerCodeProductivityItem
                    {
                        PreparerCode = "3H",
                        ExceptionCode = "P039",
                        Total = 21,
                        Reject= 2,
                        Accept = 5,
                        Skip = 1,
                        NextDay = 1,
                        DoqReq = 2,
                        Change = 5,
                        Other = 3,
                        Cleared = 2
                    },
                   
               };
            return items;
        }

        #endregion Preparer Code Productivity

        [HttpPost]
        public ActionResult ContactReportRecords([DataSourceRequest]DataSourceRequest request, ContactReportViewModel search)
        {
            var mgr = new ReportManager();
            var items = mgr.FetchContactReport(search).AsQueryable();
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AssociateProductivity()
        {
            return View();
        }

        #region Discount Availability and Viability

        [HttpGet]
        public ActionResult DiscountAvailability()
        {
            var model = new DiscountAvailabilityViewModel();
            return View("DiscountAvailability", model);
        }

        [HttpPost]
        public ActionResult GetDiscountAvailabilityAndViabilityData([DataSourceRequest]DataSourceRequest request, DiscountAvailabilitySearch search)
        {
            var mgr = new DiscAvailReportManager();
            var list = mgr.GetDiscountAvailabilityAndViabilityData(search);
            var result = list.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        #endregion Discount Availability and Viability

        //private List<ExceptionsWorkedByPCItem> GetDummyExceptionsWorkedByPCReportData(ExceptionsWorkedByPCSearch search)
        //{
        //    var items = new List<ExceptionsWorkedByPCItem>()
        //        {
        //            new ExceptionsWorkedByPCItem
        //            {
        //                NAME = "RUPA DUTHULUR",
        //            PREPCODE = "2T",
        //            CORSYMB = "",
        //            WGTSCORE = "",
        //            CLEARED = "",
        //            TOTAL_WORKED = "0",
        //            REJECT = "X",
        //            ACCEPT = "a",
        //            SKIPED = "s",
        //            NEXT_DAY = "T",
        //            DOC_REQ = "m",
        //            CHANGED = "c",
        //            OTHER = "o"
        //            },
        //             new ExceptionsWorkedByPCItem
        //            {
        //                NAME = "Test1",
        //                PREPCODE = "2T",
        //                CORSYMB = "abc",
        //                WGTSCORE = "F",
        //                CLEARED = "T",
        //                TOTAL_WORKED = "2",
        //                REJECT = "Y",
        //                ACCEPT = "N",
        //                SKIPED = "T",
        //                NEXT_DAY = "F",
        //                DOC_REQ = "OK",
        //                CHANGED = "Yes",
        //                OTHER = "None"
        //            },
        //            new ExceptionsWorkedByPCItem
        //            {
        //                NAME = "Test1",
        //                PREPCODE = "2T",
        //                CORSYMB = "abc",
        //                WGTSCORE = "F",
        //                CLEARED = "T",
        //                TOTAL_WORKED = "2",
        //                REJECT = "Y",
        //                ACCEPT = "N",
        //                SKIPED = "T",
        //                NEXT_DAY = "F",
        //                DOC_REQ = "OK",
        //                CHANGED = "Yes",
        //                OTHER = "None"
        //            },
        //             new ExceptionsWorkedByPCItem
        //            {
        //                NAME = "Test1",
        //                PREPCODE = "2T",
        //                CORSYMB = "abc",
        //                WGTSCORE = "F",
        //                CLEARED = "T",
        //                TOTAL_WORKED = "2",
        //                REJECT = "Y",
        //                ACCEPT = "N",
        //                SKIPED = "T",
        //                NEXT_DAY = "F",
        //                DOC_REQ = "OK",
        //                CHANGED = "Yes",
        //                OTHER = "None"
        //            },
        //       };
        //    return items;
        //}

        //public ActionResult ExceptionCleared()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public ActionResult ExceptionCleared([DataSourceRequest]DataSourceRequest request, ExceptionClearedSearch search)
        //{
        //    var items = GetDummyExceptionClearedReportData(search).AsQueryable();
        //    //if (!string.IsNullOrWhiteSpace(search.PreparerCode))
        //    //    items = items.Where(I => I.PreparerCode == search.PreparerCode);
        //    var result = items.ToDataSourceResult(request);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        //private List<ExceptionClearedItem> GetDummyExceptionClearedReportData(ExceptionClearedSearch search)
        //{
        //    var items = new List<ExceptionClearedItem>()
        //        {
        //            new ExceptionClearedItem
        //            {
        //                PREPCODE = "2T",
        //                USERNAME = "Test",
        //                ERROR_CODE = "P044",
        //                EXCEPTION_MEMO = "Utilities",
        //                COUNT = 1
        //            },
        //             new ExceptionClearedItem
        //            {
        //                PREPCODE = "2T",
        //                USERNAME = "Test1",
        //                ERROR_CODE = "P044",
        //                EXCEPTION_MEMO = "Utilities",
        //                COUNT = 1
        //            },
        //            new ExceptionClearedItem
        //            {
        //                PREPCODE = "2T",
        //                USERNAME = "Test2",
        //                ERROR_CODE = "P044",
        //                EXCEPTION_MEMO = "Utilities",
        //                COUNT = 1
        //            },
        //             new ExceptionClearedItem
        //            {
        //                PREPCODE = "2T",
        //                USERNAME = "Test3",
        //                ERROR_CODE = "P044",
        //                EXCEPTION_MEMO = "Utilities",
        //                COUNT = 1
        //            },
        //       };
        //    return items;
        //}

        //public ActionResult PegasysDaysOldInvoice()
        //{
        //    return View();
        //}

        //[HttpPost]
        //public ActionResult PegasysDaysOldInvoice([DataSourceRequest]DataSourceRequest request, PegasysDaysOldInvoiceSearch search)
        //{
        //    var items = GetDummyPegasysDaysOldReportData(search).AsQueryable();
        //    //if (!string.IsNullOrWhiteSpace(search.PreparerCode))
        //    //    items = items.Where(I => I.PreparerCode == search.PreparerCode);
        //    var result = items.ToDataSourceResult(request);
        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        //private List<PegasysDaysOldInvoiceItem> GetDummyPegasysDaysOldReportData(PegasysDaysOldInvoiceSearch search)
        //{
        //    var items = new List<PegasysDaysOldInvoiceItem>()
        //        {
        //            new PegasysDaysOldInvoiceItem
        //            {
        //                ACT= "12345678",
        //                VCPO= "das",
        //                PDOCNOPO= "sdfdsfewq",
        //                DAYSOLD= 9,
        //                INV_KEY_ID= "sdfdsf3423",
        //                AMOUNT= 10,
        //                INVOICE= "12345678",
        //                LOGDATE= DateTime.Parse("12/25/2008"),
        //                ORGCODE = "P05",
        //                BA= "A12",
        //                VENDNAME= "Star",
        //                DAILYINT= 5,
        //                CUMINT= 4,
        //                ERR_CODE= "U044",
        //                KEYDATE= "12/2/2004",
        //                CONTRACT= "fsdf"
        //            },
        //             new PegasysDaysOldInvoiceItem
        //            {
        //                ACT= "12345678",
        //                VCPO= "das",
        //                PDOCNOPO= "sdfdsfewq",
        //                DAYSOLD= 9,
        //                INV_KEY_ID= "sdfdsf3423",
        //                AMOUNT= 10,
        //                INVOICE= "12345678",
        //                LOGDATE= DateTime.Parse("12/25/2008"),
        //                ORGCODE = "P05",
        //                BA= "A12",
        //                VENDNAME= "Star",
        //                DAILYINT= 5,
        //                CUMINT= 4,
        //                ERR_CODE= "U044",
        //                KEYDATE= "12/2/2004",
        //                CONTRACT= "fsdf"
        //            },
        //            new PegasysDaysOldInvoiceItem
        //            {
        //                ACT= "12345678",
        //                VCPO= "das",
        //                PDOCNOPO= "sdfdsfewq",
        //                DAYSOLD= 9,
        //                INV_KEY_ID= "sdfdsf3423",
        //                AMOUNT= 10,
        //                INVOICE= "12345678",
        //                LOGDATE= DateTime.Parse("12/25/2008"),
        //                ORGCODE = "P05",
        //                BA= "A12",
        //                VENDNAME= "Star",
        //                DAILYINT= 5,
        //                CUMINT= 4,
        //                ERR_CODE= "U044",
        //                KEYDATE= "12/2/2004",
        //                CONTRACT= "fsdf"
        //            },
        //             new PegasysDaysOldInvoiceItem
        //            {
        //                ACT= "12345678",
        //                VCPO= "das",
        //                PDOCNOPO= "sdfdsfewq",
        //                DAYSOLD= 9,
        //                INV_KEY_ID= "sdfdsf3423",
        //                AMOUNT= 10,
        //                INVOICE= "12345678",
        //                LOGDATE= DateTime.Parse("12/25/2008"),
        //                ORGCODE = "P05",
        //                BA= "A12",
        //                VENDNAME= "Star",
        //                DAILYINT= 5,
        //                CUMINT= 4,
        //                ERR_CODE= "U044",
        //                KEYDATE= "12/2/2004",
        //                CONTRACT= "fsdf"
        //            },
        //       };
        //    return items;
        //}

        //public ActionResult PegasysDocumentsKeyedIndexed()
        //{
        //    return View();
        //}
    }
}