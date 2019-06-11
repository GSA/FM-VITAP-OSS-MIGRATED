using System;
using System.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using System.Web;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Data.Models.Exceptions;
using VITAP.Library;
using VITAP.Library.Strings;
using VITAP.SharedLogic;
using VITAP.Utilities;
using VITAP.Utilities.Attributes;
using VITAP.Data.PegasysEntities;
using VITAP.SharedLogic.Buttons;
using VITAP.Data.Models.Exceptions.V299;
using VITAP.Data.Models.Exceptions.U066;
using VITAP.Data.Models.Exceptions.NotificationExceptions;
using System.Web.Routing;
using Newtonsoft.Json;
using VITAP.Data.Models.Exceptions.M237;
using VITAP.Data.Models.Exceptions.A237;

namespace VITAP.Controllers
{
    public class ExceptionsController : VitapBaseController
    {
        ExceptionListModel roleModel = new ExceptionListModel();

        // GET: Home
        public ActionResult Index()
        {
            // No roles, no access.
            if (Session[SessionKey.RoleModel] == null)
            {
                return View("Index", "Home");
            }

            var model = GetExceptionsViewModel();
            Session[SessionKey.ExceptionsListModel] = model;
            roleModel = model;

            // Clear out any exception-specific state.
            TempData.Remove("P200Search");

            //if (!model.RoleModel.HasExceptionsRole || !model.RoleModel.HasApacctRole)
            //{
            //    return View("NoPermission");
            //}
            //else
            //{
            return View(model);
            //}
        }

        [HttpPost]
        public ActionResult Index(DataSourceRequest request, ExceptionListSearch search)
        {
            if (Session[SessionKey.ExceptionsListModel] == null)
            {
                return View("Index", "Home");
            }
            var exceptionListModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            if (exceptionListModel == null)
            {
                exceptionListModel = GetExceptionsViewModel();
            }
                
            
            search.Role = exceptionListModel.RoleModel.ASSIGN_SRV;
            search.HasVendCoderRole = exceptionListModel.RoleModel.HasVendCoderRole;
            

            VitapExceptionManager manager = new VitapExceptionManager();

            Dictionary<string, string> details = new Dictionary<string, string>();
            details.Add("Page", request.Page.ToString());
            if (request.PageSize > 0)
            {
                details.Add("PageSize", request.PageSize.ToString());
            }
            else
                details.Add("PageSize", "30");

            // Save so when returning from other screens the grid is automatically populated as it was.
            TempData["search"] = search;

            var result = new DataSourceResult()
            {
                Data = manager.GetExceptionListData(details, search),
                Total = manager.GetDataTotal(details, search)
            };

            Session[SessionKey.MainSearch] = search;
            Session[SessionKey.MainRequest] = request;

            return Json(result);
        }

        private ExceptionListModel GetExceptionsViewModel()
        {

            VitapExceptionManager manager = new VitapExceptionManager();
            ExceptionListModel model = new ExceptionListModel();

            ExceptionListSearchCriteria searchCriteria = new ExceptionListSearchCriteria();
            searchCriteria.ExceptionTypes = manager.GetExceptionTypes();
            searchCriteria.SearchByVals = GetSearchByVals();
            searchCriteria.SortByVals = manager.GetSortByVals();
            searchCriteria.SecSortByVals = manager.GetSecSortByVals();
            searchCriteria.DocTypeVals = manager.GetDocTypeVals();
            searchCriteria.Roles = manager.GetRoles();

            // If returning to Exceptions search from someplace like InvEdit then restore the grid.
            if (TempData.Peek("search") != null)
                RestoreSearchParameters(searchCriteria);

            model.RoleModel = Session[SessionKey.RoleModel] as RoleListModel;


            searchCriteria.Role = Session[SessionKey.AssignSrv].ToString();

            searchCriteria.RadioVals = manager.GetRadioVals();
            model.SearchCriteria = searchCriteria;

            return model;
        }

        private void RestoreSearchParameters(ExceptionListSearchCriteria searchCriteria)
        {
            var search = TempData["search"] as ExceptionListSearch;
            searchCriteria.ExceptionType = search.ExceptionType;
            searchCriteria.OrgCode = search.OrgCode;
            searchCriteria.Radio2 = search.Radio2;
            searchCriteria.Role = search.Role;
            searchCriteria.SearchAll = search.SearchAll;
            searchCriteria.SearchBy = search.SearchBy;
            searchCriteria.SecSortBy = search.SecSortBy;
            searchCriteria.ShowDocType = search.ShowDocType;
            searchCriteria.ShowSkip = search.ShowSkip == "true" ? true : false;
            searchCriteria.SortBy = search.SortBy;
            searchCriteria.SearchSource = search.SearchSource;
        }

        private ExceptionListModel GetRolesViewModel()
        {
            VitapExceptionManager manager = new VitapExceptionManager();
            var exceptionListModel = new ExceptionListModel();

            //UserRoles
            var roleModel = new RoleListModel();

            var mgr = new RolesManager();
            var roleList = Session[SessionKey.Roles] as List<string>;
            var AssignedSrv = Session[SessionKey.AssignSrv].ToString();
            var PrepCode = Session[SessionKey.PrepCode].ToString();
            var Symbol = Session[SessionKey.Symbol].ToString();

            exceptionListModel.RoleModel = Session[SessionKey.RoleModel] as RoleListModel;

            return exceptionListModel;
        }

        private ExceptionListModel GetExceptionsViewModel(DataSourceRequest request, ExceptionListSearch search)
        {
            VitapExceptionManager manager = new VitapExceptionManager();
            ExceptionListModel model = new ExceptionListModel();
            Dictionary<string, string> details = new Dictionary<string, string>();
            details.Add("Page", request.Page.ToString());
            if (request.PageSize > 0)
            {
                details.Add("PageSize", request.PageSize.ToString());
            }
            else
                details.Add("PageSize", "30");
            model.Exceptions = manager.GetExceptionListData(details, search);

            var total = model.Exceptions.Count();

            ExceptionListSearchCriteria searchCriteria = new ExceptionListSearchCriteria();
            searchCriteria.ExceptionTypes = manager.GetExceptionTypes();
            searchCriteria.SearchByVals = GetSearchByVals();
            searchCriteria.SortByVals = manager.GetSortByVals();
            searchCriteria.SecSortByVals = manager.GetSecSortByVals();
            searchCriteria.DocTypeVals = manager.GetDocTypeVals();
            searchCriteria.Roles = manager.GetRoles();
            searchCriteria.RadioVals = manager.GetRadioVals();
            model.SearchCriteria = searchCriteria;

            return model;
        }

        public List<SelectItem> GetSearchByVals()
        {
            List<SelectItem> values = new List<SelectItem>();
            //var exceptionListModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            var roleModel = Session[SessionKey.RoleModel] as RoleListModel;

            values.Add(new SelectItem() { Id = "err_code", Text = "Exception Code" });
            values.Add(new SelectItem() { Id = "Act", Text = "Act" });
            values.Add(new SelectItem() { Id = "PDOCNO", Text = "PDOCNO" });
            values.Add(new SelectItem() { Id = "VendName", Text = "VendName" });

            return values;
        }

        public ActionResult DisplayException(string exCode, string exId)
        {

            // Save exception and invoice in TempData for subsequent processing.
            var efExceptionModel = GetExceptionByExId(exId);
            TempData["exception"] = efExceptionModel;

            if (efExceptionModel.OUT != null && efExceptionModel.OUT == "T" && efExceptionModel.PREPCODE != PrepCode)
            {
                //Need to add this in the screen probably and only call this code if they are different:
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "This exception is checked out",
                    Origin = ControllerAction.Exception.DisplayException,
                    ReturnAction = ControllerAction.Exception.DisplayExceptionWithoutCheckoutMessage,
                    ReturnController = "Exceptions",
                    ExId = efExceptionModel.EX_ID,
                    ErrCode = efExceptionModel.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            else if (efExceptionModel.OUT == null || efExceptionModel.OUT != "T")
            {
                ExceptionsManager manager = new ExceptionsManager();
                manager.CheckoutException(efExceptionModel.EX_ID, PrepCode);
            }


            return ReturnViewResult(exCode, exId, efExceptionModel);
        }

        public ActionResult DisplayExceptionWithoutCheckoutMessage(string exCode, string exId)
        {

            // Save exception and invoice in TempData for subsequent processing.
            var efExceptionModel = GetExceptionByExId(exId);
            TempData["exception"] = efExceptionModel;

            return ReturnViewResult(exCode, exId, efExceptionModel);
        }

        private ActionResult ReturnViewResult(string exCode, string exId, EXCEPTION efExceptionModel)
        {
            try
            {
                if ("P001/P002/P004/P023/P024/P060/P061/P201/M230/A230/P230/P231/P232/P234".Split('/').Contains(exCode))
                {
                    return GetMainExceptionsView(efExceptionModel);
                }
                else if ("P200/A200/M200/O200/A230".Split('/').Contains(exCode))
                {
                    return GetP200View(efExceptionModel);
                }
                else if (exCode == "KM7A" || exCode == "KMWA")
                {
                    var mgr = new ExceptionsManager();
                    var KeyedModel = mgr.GetKeyedException(exId);
                    Session[SessionKey.Model] = KeyedModel;
                    return View("KEYED/Exception", KeyedModel);
                }
                else if (exCode == "R200")
                {
                    var mgr = new ExceptionsManager();
                    var R200Model = mgr.GetR200Exception(exId);
                    if (R200Model.PegInv == null)
                    {
                        ViewBag.Message = "The required Pegasys Invoice data is missing...Cannot proceed further";
                        return View("ViewErrorMessage");
                    }
                    Session[SessionKey.R200Model] = R200Model;
                    Session[SessionKey.X200] = R200Model.R200Recs;
                    return View("R200/Exception", R200Model);
                }
                else if (exCode == "M224")
                {
                    M224Manager manager = new M224Manager();
                    var exception = TempData["exception"] as EXCEPTION;
                    if (exception == null || exception.EX_ID != exId)
                    {
                        exception = GetExceptionByExId(exId);
                    }
                    // Preserve for subsequent postbacks
                    TempData["exception"] = exception;
                    var model = manager.BuildModel(exception);

                    TempData["M224Model"] = model;
                    return View("M303/Exception", model);
                }
                else if (exCode == "M237")
                {
                    M237Manager manager = new M237Manager();
                    var exception = TempData["exception"] as EXCEPTION;
                    if (exception == null || exception.EX_ID != exId)
                    {
                        exception = GetExceptionByExId(exId);
                    }
                    // Preserve for subsequent postbacks
                    TempData["exception"] = exception;
                    var model = manager.BuildModel(exception);

                    return View("M237/Exception", model);
                }
                //Test A237,A224,A226,poedit,rredit mockup
                else if (exCode == "A237")
                {
                    A237Manager manager = new A237Manager();
                    var exception = TempData["exception"] as EXCEPTION;
                    if (exception == null || exception.EX_ID != exId)
                    {
                        exception = GetExceptionByExId(exId);
                    }
                    // Preserve for subsequent postbacks
                    TempData["exception"] = exception;
                    var model = manager.BuildModel(exception);

                    return View("A237/Exception", model);
                }
                else if (exCode == "A224")
                {
                    var manager = new A224Manager();
                    var model = manager.BuildModel(exCode, exId);

                    return View("A224/Exception", model);
                }

                else if (exCode == "A226")
                {
                    var manager = new A226Manager();
                    var prepcode = Session[SessionKey.PrepCode].ToString();
                    var model = manager.BuildModel(exCode, exId, prepcode);

                    return View("A226/Exception", model);
                }
                else if (exCode == "M303")
                {
                    M303manager manager = new M303manager();
                    var exception = TempData["exception"] as EXCEPTION;
                    if (exception == null || exception.EX_ID != exId)
                    {
                        exception = GetExceptionByExId(exId);
                    }
                    // Preserve for subsequent postbacks
                    TempData["exception"] = exception;
                    var model = manager.BuildModel(exception);

                    TempData["M303Model"] = model;
                    return View("M303/Exception", model);
                }
                else if (exCode == "O305")
                {
                    O305Manager manager = new O305Manager();
                    var exception = TempData["exception"] as EXCEPTION;
                    if (exception == null || exception.EX_ID != exId)
                    {
                        exception = GetExceptionByExId(exId);
                    }
                    // Preserve for subsequent postbacks
                    TempData["exception"] = exception;
                    var model = manager.BuildModel(exception);

                    return View("O305/Exception", model);
                }
                else if (exCode == "P039")
                {
                    P039Manager manager = new P039Manager();
                    var p039Model = manager.BuildModel(exCode, exId);

                    return View("P039/Exception", p039Model);
                }
                else if ("P040/P041/P042".Split('/').Contains(exCode))
                {
                    return GetNotificationExceptionsView(exCode, exId);
                }
                else if (exCode == "P140")
                {
                    var mgr = new ExceptionsManager();
                    var model = mgr.GetExceptionP140(exId);
                    Session[SessionKey.P140Model] = model;

                    return View("P140/Exception", model);
                }
                else if (exCode == ExceptionCode.U054 || exCode == ExceptionCode.V055)
                {
                    return GetInvEditView();
                }
                else if (exCode == ExceptionCode.E052 || exCode == ExceptionCode.U043
                    || exCode == ExceptionCode.U049 || exCode == ExceptionCode.U044)
                {

                    return GetUserExceptionView(false, exId, null);
                }
                else if (exCode == "P202")
                {
                    P202Manager manager = new P202Manager();
                    var P202model = manager.BuildModel(exCode, exId);
                    return View("P202/Exception", P202model);
                }
                else if (exCode == "U066")
                {
                    var manager = new U066Manager();
                    var U066model = manager.BuildModel(exCode, exId);
                    return View("U066/Exception", U066model);
                }
                else if (exCode == "U065")
                {
                    var exception = new ExceptionsManager().GetExceptionByExId(exId);
                    return RedirectToAction("Edit", "Contacts", new { openOption = "exception", act = exception.ACT, exceptionId = exId, errorCode = exCode, assignedService = this.AssignSrv });
                }
                else if ("V299/V216/V300/V215".Split('/').Contains(exCode))
                {
                    if (!UserHasRole(VitapRoles.VendorCoder))
                    {
                        return View("NoPermission");
                    }

                    var manager = new V299Manager();
                    var model = manager.BuildModel(exCode, exId, this.PrepCode);
                    return View("V299/Exception", model);
                }

                //var roleModel = GetRolesViewModel();
                var defaultModel = GetExceptionsViewModel();
                return View("Index", defaultModel);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "ReturnViewResult");
            }
        }

        [HttpPost]
        public ActionResult P140Records([DataSourceRequest]DataSourceRequest request, P140Record search)
        {
            var items = GetDummyP140ExceptionRecords(search).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search.PEG))
                items = items.Where(x => x.PEG == search.PEG);
            var result = items.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult V299Search(V299SearchRequest searchRequest)
        {
            V299Manager manager = new V299Manager();
            var searchResults = manager.GetSearchResults(searchRequest);


            return Json(searchResults);
        }

        [HttpPost]
        public ActionResult U066Search(U066SearchRequest searchRequest)
        {
            U066Manager manager = new U066Manager();
            var searchResults = manager.GetSearchResults(searchRequest.searchText, searchRequest.searchType, searchRequest.orgCode, searchRequest.pDocNo);
            return Json(searchResults);
        }

        #region Input
        public ActionResult Input(string inputText, string inputLabel)
        {
            var InputModel = new InputViewModel();
            InputModel.InputTxt = inputText;
            InputModel.Label = inputLabel;

            return View("Other/Input", InputModel);
        }
        #endregion

        #region User Exception

        private ViewResult GetUserExceptionView(bool clearException = false)
        {
            var mgr = new ExceptionsManager();
            var exception = clearException
                ? TempData["exception"] as EXCEPTION
                : TempData.Peek("exception") as EXCEPTION;

            var vmException = UserExceptionViewModel.MapToViewModel(exception);
            var ActGridData = mgr.UserGridData(vmException.Act, vmException.PDocNo);
            vmException.E052Recs = ActGridData;

            SetUserExceptionPOData(vmException, mgr);
            SetUserExceptionRRData(vmException, mgr);
            SetUserExceptionInvoiceData(vmException, mgr);

            if (!(vmException.PoExists || vmException.RRExists || vmException.InvExists))
            {
                vmException.CorrectDe1Tip = "No data to change";
            }
            else { vmException.CorrectDe1Tip = ""; }

            return View("UserException/Exception", vmException);
        }

        private void SetUserExceptionPOData(UserExceptionViewModel vmException, ExceptionsManager mgr)
        {
            if (vmException.PoId == null)
            {
                vmException.PoImageBtnTip = "No PO.";
                return;
            }

            var PegPoFrmData = mgr.PegPoFrmData(vmException.PoId);
            //if (PegPoFrmData == null) {
            //    PegPoData = mgr.PegPoData(vmException.PoId);
            //}
            if (PegPoFrmData == null)
            {
                vmException.PoImageBtnTip = "No record in PO.";
                return;
            }

            vmException.PoExists = true;
            vmException.RRExists = false;
            vmException.InvExists = false;

            if (vmException.PoId.Contains("&"))
            {
                vmException.ModNumStr = vmException.PoId.Right(4);
            }
            else
            {
                vmException.ModNumStr = "";
            }

            if (vmException.r_pegasys)
            {
                if (PegPoFrmData.EDI_IND == "T")
                {
                    vmException.PoImageExists = false;
                }
                else
                {
                    vmException.PoImageExists = true;
                }
            }
            else
            {
                if (PegPoFrmData.EDI_IND != null)
                {
                    vmException.PoImageExists = false;
                }
                else
                {
                    vmException.PoImageExists = true;
                }
            }
            vmException.EdiInd = PegPoFrmData.EDI_IND;
            vmException.PrepCode = PegPoFrmData.PREPCODE;
            vmException.PoImgageId = PegPoFrmData.IMAGEID;
        }

        private void SetUserExceptionRRData(UserExceptionViewModel vmException, ExceptionsManager mgr)
        {
            if (vmException.RRId == null)
            {
                vmException.RRImageBtnTip = "No RR.";
                return;
            }

            var PegRRFrmData = mgr.PegRRFrmData(vmException.RRId);
            if (PegRRFrmData == null)
            {
                vmException.RRImageBtnTip = "No record in RR.";
                return;
            }

            vmException.RRExists = true;
            vmException.PoExists = false;
            vmException.InvExists = false;

            vmException.PDocNo = PegRRFrmData.PDOCNOPO;

            if (PegRRFrmData.EDI_IND == "T")
            {
                vmException.RRImageExists = false;
            }
            else if (PegRRFrmData.IMAGEID != null)
            {
                vmException.RRImageExists = true;
                vmException.RRImageId = PegRRFrmData.IMAGEID;
            }
            vmException.RRImageBtnTip = "Pegasys RR.";
        }

        private void SetUserExceptionInvoiceData(UserExceptionViewModel vmException, ExceptionsManager mgr)
        {
            if (vmException.InvKeyId == null)
            {
                vmException.InvImageBtnTip = "No Inv.";
                return;
            }
            var PegInvData = mgr.PegInvData(vmException.InvKeyId);
            if (PegInvData == null)
            {
                vmException.InvImageBtnTip = "No record in Inv.";
                return;
            }

            if (vmException.ErrorCode == "U044")
            {
                var memo2String = "\r\n {0} Invoice: {1} Amount: {2}";
                vmException.Memo2 = string.Format(memo2String, vmException.Memo2, PegInvData.INVOICE, PegInvData.AMOUNT);
            }

            vmException.InvExists = true;
            vmException.PoExists = false;
            vmException.RRExists = false;

            vmException.PDocNo = PegInvData.PDOCNOPO;
            if (PegInvData.EDI_IND == "T")
            {
                vmException.InvImageExists = false;
            }
            else if (PegInvData.IMAGEID != null)
            {
                vmException.InvImageExists = true;
                vmException.InvImageId = PegInvData.IMAGEID;
            }
        }

        #endregion User Exception

        #region Buttons
        /// <summary>
        /// This is the starting point for the Accept button
        /// </summary>
        /// <param name="PDocNo"></param>
        /// <param name="rrchoice"></param>
        /// <param name="invKeyId"></param>
        /// <param name="pofrmquery"></param>
        /// <param name="Search"></param>
        /// <param name="Address"></param>
        /// <param name="Misc"></param>
        /// <param name="bDunsMatched"></param>
        /// <param name="newInvKeyId"></param>
        /// <returns></returns>
        public ActionResult Accept(string vendorName, string customerName, string vendorCode, string vendorAddress, string Addr1, string Addr2,
            string Addr3, string City, string State, string Zip, string Email, string Phone, string Fax, string searchVendorName, string searchCustomerName,
            string searchVendorCode, string searchVendorAddress, string searchAddr1, string searchAddr2, string searchAddr3, string searchCity, string searchState, string
            searchZip, string searchEmail, string searchPhone, string searchFax, string orgCode, string BA, string Duns, string dunsPlus4, bool? bDunsMatched, string newInvKeyId)
        {
            roleModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            var prepCode = roleModel.RoleModel.PREPCODE;

            var Misc = new MiscValuesModel { ORGCODE = orgCode.ReplaceNull(""), BA = BA.ReplaceNull(""), DUNS = Duns.ReplaceNull(""), DUNSPLUS4 = dunsPlus4.ReplaceNull("") };
            var Search = new AddressValuesModel
            {
                VENDORNAME = searchVendorName.ReplaceNull(""),
                CUSTOMERNAME = searchCustomerName.ReplaceNull(""),
                VENDORCODE = searchVendorCode.ReplaceNull(""),
                VENDORADDRESS = searchVendorAddress.ReplaceNull(""),
                ADDR1 = searchAddr1.ReplaceNull(""),
                ADDR2 = searchAddr2.ReplaceNull(""),
                ADDR3 = searchAddr3.ReplaceNull(""),
                CITY = searchCity.ReplaceNull(""),
                STATE = searchState.ReplaceNull(""),
                ZIP = searchZip.ReplaceNull(""),
                EMAIL = searchEmail.ReplaceNull(""),
                PHONE = searchPhone.ReplaceNull(""),
                FAX = searchFax.ReplaceNull("")
            };
            var Address = new AddressValuesModel
            {
                VENDORNAME = vendorName.ReplaceNull(""),
                CUSTOMERNAME = customerName.ReplaceNull(""),
                VENDORCODE = vendorCode.ReplaceNull(""),
                VENDORADDRESS = vendorAddress.ReplaceNull(""),
                ADDR1 = Addr1.ReplaceNull(""),
                ADDR2 = Addr2.ReplaceNull(""),
                ADDR3 = Addr3.ReplaceNull(""),
                CITY = City.ReplaceNull(""),
                STATE = State.ReplaceNull(""),
                ZIP = Zip.ReplaceNull(""),
                EMAIL = Email.ReplaceNull(""),
                PHONE = Phone.ReplaceNull(""),
                FAX = Fax.ReplaceNull("")
            };
            if (bDunsMatched == null) { bDunsMatched = false; }

            var helper = new AcceptButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            string PDocNo = "", invKeyId = "";
            var pofrmquery = new PEGASYSPO_FRM();
            var rrchoice = new List<RRCHOICE>();

            if (exception.ERR_CODE.Left(1) == "K")
            {
                var Model = Session[SessionKey.Model] as ExceptionKeyedViewModel;
                PDocNo = Model.Pdocnopo;
                invKeyId = Model.InvKeyID;
            }

            var Continue = helper.Initialize(exception, PDocNo, prepCode, bDunsMatched.Value);

            if (Continue == true)
            {
                if (exception.ERR_CODE.Left(1) == "K")
                {
                    string theMsg = helper.ExceptionK(newInvKeyId, exception.INV_KEY_ID, roles.PREPCODE);
                    if (!String.IsNullOrWhiteSpace(theMsg))
                    {
                        var vmConfirm = new MessageDisplay()
                        {
                            Title = "Pegasys Key Exception Error",
                            Question = theMsg,
                            Origin = ControllerAction.Exception.Accept,
                            Controller = "Exceptions",
                            ReturnAction = ControllerAction.Exception.DisplayException,
                            ReturnController = "Exceptions",
                            ExId = exception.EX_ID,
                            ErrCode = exception.ERR_CODE,
                        };

                        return View("ConfirmOk", vmConfirm);
                    }
                    return View("keyed/exception", Session[SessionKey.Model] as ExceptionKeyedViewModel);
                }

                Session[SessionKey.Exception] = exception;
                Session[SessionKey.PDocNo] = PDocNo;
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.Misc] = Misc;
                Session[SessionKey.Search] = Search;
                Session[SessionKey.Address] = Address;
                Session[SessionKey.InvKeyId] = invKeyId;
                Session[SessionKey.NewInvKeyId] = newInvKeyId;
                Session[SessionKey.Pofrmquery] = pofrmquery;
                Session[SessionKey.Rrchoice] = rrchoice;
                Session[SessionKey.DunsMatched] = bDunsMatched;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO, ControllerAction.Exception.Accept,
                    ControllerAction.Exception.AcceptNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        public ActionResult Accept(string pDocNo, string orgCode, string BA, string Duns, string dunsPlus4, bool? bDunsMatched)
        {
            roleModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            var prepCode = roleModel.RoleModel.PREPCODE ?? "";

            var Misc = new MiscValuesModel { ORGCODE = orgCode.ReplaceNull(""), BA = BA.ReplaceNull(""), DUNS = Duns.ReplaceNull(""), DUNSPLUS4 = dunsPlus4.ReplaceNull("") };
            if (bDunsMatched == null) { bDunsMatched = false; }

            var helper = new AcceptButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var pofrmquery = new PEGASYSPO_FRM();
            var rrchoice = new List<RRCHOICE>();

            helper.Initialize(exception, pDocNo, prepCode, bDunsMatched.Value);

            Session[SessionKey.Exception] = exception;
            Session[SessionKey.PDocNo] = pDocNo;
            Session[SessionKey.Helper] = helper;
            Session[SessionKey.Misc] = Misc;
            Session[SessionKey.InvKeyId] = exception.INV_KEY_ID;
            Session[SessionKey.Pofrmquery] = pofrmquery;
            Session[SessionKey.Rrchoice] = rrchoice;
            Session[SessionKey.DunsMatched] = bDunsMatched;

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO, ControllerAction.Exception.Accept,
                ControllerAction.Exception.AcceptNotes, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);
        }

        public ActionResult AcceptMain(string pDocNo)
        {
            return Accept(pDocNo);
        }

        public ActionResult AcceptV299(string invKeyId, string searchVendorName, string searchCustomerName,
            string searchVendorCode, string searchVendorAddress, string searchAddr1, string searchAddr2, string searchAddr3, string searchCity, string searchState, string
            searchZip)
        {
            return Accept(null, null, null, null, null, null, null, null, null, null, null, null, null, searchVendorName, searchCustomerName, searchVendorCode, searchVendorAddress,
               searchAddr1, searchAddr2,
               searchAddr3, searchCity, searchState, searchZip, null, null, null, null, null, null, null, null, invKeyId);
        }

        public ActionResult AcceptU066(string orgCode, string BA, string newInvKeyId)
        {
            return Accept(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
               null, null, null, null, null, null, null, orgCode, BA, null, null, null, newInvKeyId);
        }

        public ActionResult Accept(string pDocNo)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;

            var Misc = new MiscValuesModel { ORGCODE = exception.ORGCODE.ReplaceNull(""), BA = exception.BA.ReplaceNull(""), DUNS = "", DUNSPLUS4 = "" };
            Session[SessionKey.Misc] = Misc;

            // Not sure if this is needed/valid here.
            var isKeyedException = (exception.ERR_CODE.Left(1) == "K");
            if (isKeyedException)
                return AcceptKeyedException(exception.INV_KEY_ID);

            roleModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            //var prepCode = roleModel.RoleModel.PREPCODE;
            var prepCode = PrepCode;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var helper = new AcceptButton();
            helper.Initialize(exception, pDocNo, prepCode);

            Session[SessionKey.Exception] = exception;
            Session[SessionKey.PDocNo] = pDocNo;
            Session[SessionKey.Helper] = helper;
            Session[SessionKey.InvKeyId] = exception.INV_KEY_ID;
            //Session[SessionKey.NewInvKeyIdl] = newInvKeyId;

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO, ControllerAction.Exception.Accept,
                ControllerAction.Exception.AcceptNotes, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);
        }

        public ActionResult AcceptKeyedException(string newInvKeyId)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var isKeyedException = (exception.ERR_CODE.Left(1) == "K");
            if (!isKeyedException)
                return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });

            roleModel = Session[SessionKey.ExceptionsListModel] as ExceptionListModel;
            var prepCode = roleModel.RoleModel.PREPCODE;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;
            string PDocNo = "", invKeyId = "";
            var Model = Session[SessionKey.Model] as ExceptionKeyedViewModel;
            PDocNo = Model.Pdocnopo;
            invKeyId = Model.InvKeyID;

            var helper = new AcceptButton();
            helper.Initialize(exception, PDocNo, prepCode);
            string theMsg = helper.ExceptionK(newInvKeyId, exception.INV_KEY_ID, roles.PREPCODE);
            if (string.IsNullOrWhiteSpace(theMsg))
            {
                var request = Session[SessionKey.MainRequest] as DataSourceRequest;
                var search = Session[SessionKey.MainSearch] as ExceptionListModel;
                return RedirectToAction("Index", new { request, search });
            }

            var vmConfirm = new MessageDisplay()
            {
                Title = "Pegasys Key Exception Error",
                Question = theMsg,
                Origin = ControllerAction.Exception.Accept,
                Controller = "Exceptions",
                ReturnAction = ControllerAction.Exception.DisplayException,
                ReturnController = "Exceptions",
                ExId = exception.EX_ID,
                ErrCode = exception.ERR_CODE,
            };

            return View("ConfirmOk", vmConfirm);

        }

        /// <summary>
        /// This is called from the notes screen and not directly
        /// Still need to handle the messages and redirect in this segment
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult AcceptNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH")
                return View("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel);

            var roles = Session[SessionKey.RoleModel] as RoleListModel;
            var prepCode = roles.PREPCODE;
            var exception = TempData.Peek("exception") as EXCEPTION;
            Session[SessionKey.VmNotes] = vmNotes;

            var viewResult = AcceptNoteViewResult(exception, vmNotes);
            if (viewResult != null)
                return viewResult;

            var vmConfirm2 = new MessageDisplay()
            {
                Title = "",
                Response = true,
            };
            return RedirectToAction("AcceptMessage", new { vmConfirm2 });
        }

        #region Accept Notes

        [ValidateInput(false)]
        private ViewResult AcceptNoteViewResult(EXCEPTION exception, NotesViewModel vmNotes)
        {
            //Check data and display messages before proceeding to get user input on how to proceed
            var Misc = Session[SessionKey.Misc] as MiscValuesModel;
            if (exception.ERR_CODE.InList("V216, V215") && Misc.DUNS.Length == 0)
            {
                //If user says No exit out
                return AcceptNoteV216(exception.EX_ID, exception.ERR_CODE);
            }

            if (exception.ERR_CODE == "V215")
            {
                return AcceptNoteV215(exception.EX_ID, exception.ERR_CODE, vmNotes.returnVal10);
            }

            return null;
        }

        private ViewResult AcceptNoteV216(string exceptionId, string errCode)
        {
            var vmConfirm = new MessageDisplay()
            {
                Title = "Exception V216",
                Question = "CCR Flag Option has not been selected. Are you sure?",
                Origin = ControllerAction.Exception.AcceptNotes,
                Controller = "Exceptions",
                ReturnAction = ControllerAction.Exception.AcceptMessage,
                ReturnController = "Exceptions",
                ExId = exceptionId,
                ErrCode = errCode,
            };

            return View("ConfirmYesNo", vmConfirm);
        }

        private ViewResult AcceptNoteV215(string exceptionId, string errCode, string returnVal10)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (exception.ERR_CODE == "V215")
            {
                //Get the Duns List
                var helper = new AcceptButton();
                var DunsList = helper.DunsList(returnVal10);
                if (DunsList.Count > 0 && DunsList[0].DUNS_NUM.Trim().Length == 0)
                {

                    //Display message and get user input whether to continue or not
                    var vmConfirm = new MessageDisplay()
                    {
                        Title = "Close-Out Mod?",
                        Question = "Is this a 'Close-Out' Mod that really does not need to have a DUNS number?",
                        Origin = ControllerAction.Exception.AcceptMessage,
                        Controller = "Exceptions",
                        ReturnAction = ControllerAction.Exception.AcceptMessage,
                        ReturnController = "Exceptions",
                        ExId = exception.EX_ID,
                        ErrCode = exception.ERR_CODE,
                    };
                    return View("ConfirmYesNo", vmConfirm);
                }
            }
            return null;
        }

        #endregion Accept Notes

        #region Accept Message

        /// <summary>
        /// This is called from the message pop up window and not directly
        /// </summary>
        /// <param name="vmConfirm"></param>
        /// <returns></returns>
        public ActionResult AcceptMessage(MessageDisplay vmConfirm)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            if (vmConfirm.Title == "Close-Out Mod?" && vmConfirm.Response == false)
                return AcceptMessageCloseOutMod(vmConfirm, exception);

            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var helper = Session[SessionKey.Helper] as AcceptButton;
            helper.SetNotes(vmNotes);

            var routeResult = AcceptMessageFinishCode1(helper, exception);

            if (routeResult != null)
                return routeResult;

            HandleParametersByException(helper, exception);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        private ViewResult AcceptMessageCloseOutMod(MessageDisplay vmConfirm, EXCEPTION exception)
        {
            vmConfirm = new MessageDisplay()
            {
                Title = "Exceptions V215",
                Question = "The selected vendor does NOT have a DUNS Number. Please add the DUNS in Pegasys then click ACCEPT again and or select a different vendor.",
                Origin = ControllerAction.Exception.AcceptMessage,
                Controller = "Exceptions",
                ReturnAction = ControllerAction.Exception.DisplayException,
                ReturnController = "Exceptions",
                ExId = exception.EX_ID,
                ErrCode = exception.ERR_CODE,
            };

            return View("ConfirmOk", vmConfirm);
        }

        private ActionResult AcceptMessageFinishCode1(AcceptButton helper, EXCEPTION exception)
        {
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var Misc = Session[SessionKey.Misc] as MiscValuesModel;

            var theMsg = helper.FinishCode1(Search, Address, Misc);

            if (!String.IsNullOrWhiteSpace(theMsg))
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception",
                    Question = theMsg,
                    Origin = ControllerAction.Exception.Index,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", new RouteValueDictionary(vmConfirm));
            }
            return null;
        }

        private void HandleParametersByException(AcceptButton helper, EXCEPTION exception)
        {
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var Misc = Session[SessionKey.Misc] as MiscValuesModel;
            var invKeyId = Session[SessionKey.InvKeyId] == null ? "" : Session[SessionKey.InvKeyId] as string;
            var newInvKeyId = Session[SessionKey.NewInvKeyId] == null ? "" : Session[SessionKey.NewInvKeyId] as string;
            var PDocNo = Session[SessionKey.PDocNo] == null ? "" : Session[SessionKey.PDocNo] as string;
            var bDunsMatched = Session[SessionKey.DunsMatched] as bool?;
            switch (exception.ERR_CODE)
            {
                case "U063":
                    //Recycles the Exception
                    helper.ExceptionE063_Recycle();
                    break;

                case "U065":
                    //Recycles the Exception
                    helper.ExceptionE065_Recycle(Misc);
                    break;

                case "P001":
                    helper.ExceptionP001();
                    break;

                case "P002":
                case "P004":
                    helper.ExceptionP002(Search, Address);
                    break;

                case "P023":
                    helper.ExceptionP023();
                    break;

                case "P024":
                    helper.ExceptionP024(Search, Address);
                    break;

                case "P033":
                case "P034":
                    helper.ExceptionP033();
                    break;

                case "P039":
                    //helper.ExceptionP039();

                    var p039Model = Session[SessionKey.P039Model] as ExceptionP039Model;
                    var mgr = new P039Manager();
                    mgr.P039Accept(p039Model.PDOCNO, p039Model.PrepCode, p039Model.ExId, p039Model.InvKeyId, p039Model.YesRRArray);

                    break;

                case "P060":
                case "P061":
                    helper.ExceptionP060();
                    break;

                case "P140":
                    var rrchoice = Session[SessionKey.Rrchoice] as List<RRCHOICE>;
                    helper.ExceptionP140(rrchoice);
                    break;

                case "P201":
                    helper.ExceptionP201(Search, Address);
                    break;

                case "P202":
                    helper.ExceptionP202();
                    break;

                case "U043":
                case "U044":
                    helper.ExceptionU044();
                    break;

                case "U066":
                    helper.ExceptionU066();
                    break;

                case "U084":
                    var pofrmquery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
                    helper.ExceptionU084(pofrmquery);
                    break;

                case "V215":
                case "V216":
                case "V299":
                case "V300":
                    pofrmquery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
                    helper.ExceptionV299(invKeyId, pofrmquery, Misc, Search, Address);
                    break;

                case "V295":
                    helper.ExceptionV295(Search, Address);
                    break;

                case "A224":
                    helper.ExceptionA224();
                    break;

                case "A226":
                    helper.ExceptionA226();
                    break;

                default:
                    if (exception.ERR_CODE.Right(3) == "046")
                    {
                        helper.ExceptionException046();
                        break;
                    }
                    break;
            }
            helper.FinishCode2(Misc);
        }

        #endregion Accept Message

        /// <summary>
        /// Change button for PegasysMainExceptions
        /// </summary>
        /// <param name="POFrmQuery"></param>
        /// <param name="PDocNo"></param>
        /// <returns></returns>
        public ActionResult Change(string PO_ID, string RR_ID, string PDocNo)
        {
            var helper = new ChangeButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            PEGASYSPO_FRM POFrmQuery = null; //Need to write code to get this
            PegasysPO_FrmManager poManager = new PegasysPO_FrmManager();
            POFrmQuery = poManager.GetPegaysPO_FrmByPoId(PO_ID);

            if (POFrmQuery == null)
            {
                //Try to look up through pegasysrr_frm.pdocnopo
                POFrmQuery = poManager.GetPegaysPO_FrmByExceptionsRRID(RR_ID);
            }


            var Continue = helper.Initialize(exception, PDocNo, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.PDocNo] = PDocNo;
                Session[SessionKey.Pofrmquery] = POFrmQuery;
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.Change, ControllerAction.Exception.ChangeNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }
            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }
        public ActionResult ChangeA237(List<A237GridModel> ChangeRecList)
        {
            foreach (var item in ChangeRecList)
            {
                if (item.POLineNo == null)
                {

                    // A237 Fix by JunL 12-06-18
                    // The A237 in .NET isn't producing an error that should be there when changing only one line among multiple that need changed. 
                    return Json(new
                    {
                        success = true,
                        IsAllPOLineNoHaveValues = false,
                        Notes = "Cannot accept the Changes because one or more lines do not have the Refd_lnum entered."
                    }, JsonRequestBehavior.AllowGet);
                }
            }

            var helper = new ChangeButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            Session[SessionKey.Helper] = helper;
            Session[SessionKey.PDocNo] = exception.PDOCNO;

            Session[SessionKey.A237Model] = ChangeRecList;

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.Change, ControllerAction.Exception.ChangeNotes, "Exceptions", string.Empty);

            TempData["ChangeA237vmNotes"] = vmNotes;

            return Json(new
            {
                success = true,
                IsAllPOLineNoHaveValues = true,
                Notes = vmNotes }, 
                JsonRequestBehavior.AllowGet);
        }
        public ActionResult ChangeA237Notes()
        {
            return View("other/notes", TempData.Peek("ChangeA237vmNotes") as NotesViewModel);

        }

        [ValidateInput(false)]
        public ActionResult ChangeNotes(NotesViewModel vmNotes)
        {
            string PDocNo;
            PEGASYSPO_FRM POFrmQuery = new PEGASYSPO_FRM();

            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as ChangeButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (Session[SessionKey.PDocNo] != null)
                PDocNo = Session[SessionKey.PDocNo].ToString();

            if (Session[SessionKey.Pofrmquery] != null)
                POFrmQuery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;

            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            if (exception.ERR_CODE == "U084" && !String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception U084",
                    Question = "PegasysPO_frm Update Failed!!",
                    Origin = ControllerAction.Exception.Change,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };
                return View("ConfirmOk", vmConfirm);
            }



            helper.SetNotes(vmNotes);

            if (exception.ERR_CODE == "M237")
            {
                UpdateM237Exception(vmNotes.returnVal3, exception.RESPONSENOTES);
            }
           else  if (exception.ERR_CODE == "A237")
            {
                var changeRecList = Session[SessionKey.A237Model] as List<A237GridModel>;
                new A237Manager().Change(changeRecList, exception.EX_ID, exception.RR_ID, vmNotes.returnVal7, PrepCode, vmNotes.returnVal3);
                Session.Remove(SessionKey.A237Model);
            }
            else if (POFrmQuery != null)
            {
                if (!helper.FinishCode(POFrmQuery))
                {
                    var vmConfirm = new MessageDisplay()
                    {
                        Title = "",
                        Question = "Unexplained ERROR - Change not successful!",
                        Origin = ControllerAction.Exception.Change,
                        Controller = "Exceptions",
                        ReturnAction = ControllerAction.Exception.DisplayException,
                        ReturnController = "Exceptions",
                        ExId = exception.EX_ID,
                        ErrCode = exception.ERR_CODE,
                    };
                    return View("ConfirmOk", vmConfirm);
                }

            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Change Act for M200/P200 exceptions - this needs to update on the screen with AJAX, but use Message, notes and Input display screens as well
        /// </summary>
        /// <param name="newAct"></param>
        /// <returns></returns>
        public ActionResult ChangeAct(string newAct)
        {
            if (string.IsNullOrWhiteSpace(newAct))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.Write("A new Act Number is required.\nPlease enter it in the text box.");
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var helper = new ChangeActButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.Initialize(exception, PrepCode, newAct);

            return new EmptyResult();
        }

        /// <summary>
        /// Called from the ConfirmYesNo screen
        /// </summary>
        /// <param name="POFrmQuery"></param>
        /// <param name="PDocNo"></param>
        /// <returns></returns>
        public ActionResult ChangeActMessage(MessageDisplay vmConfirm)
        {
            var helper = Session[SessionKey.Helper] as ChangeActButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var POFrmQuery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
            var PDocNo = Session[SessionKey.PDocNo].ToString();

            if (vmConfirm.Response == false) { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var vmInput = new InputViewModel()
            {
                Label = "Enter the New Act Number: ",
                ReturnAction = ControllerAction.Exception.AcceptMessage,
                ReturnController = "Exceptions",
            };

            return View("Other/Input", vmInput);
        }

        public ActionResult ChangeActInput(InputViewModel vmInput)
        {
            var helper = Session[SessionKey.Helper] as ChangeActButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var POFrmQuery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
            string PDocNo = Session[SessionKey.PDocNo].ToString();

            var strACT = vmInput.InputTxt.Trim();
            Session[SessionKey.StrAct] = strACT;

            if (String.IsNullOrWhiteSpace(strACT))
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exceptions " + exception.ERR_CODE,
                    Question = "CCR Flag Option has not been selected. Are you sure?",
                    Origin = ControllerAction.Exception.ChangeActInput,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.ChangeActStep2,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };
                return View("ConfirmOk", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult ChangeActStep2(MessageDisplay vmConfirm)
        {
            var helper = Session[SessionKey.Helper] as ChangeActButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var POFrmQuery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
            var PDocNo = Session[SessionKey.PDocNo].ToString();
            var strACT = Session[SessionKey.StrAct].ToString();

            if (strACT == exception.ACT)
            {
                vmConfirm = new MessageDisplay()
                {
                    Title = "Exceptions " + exception.ERR_CODE,
                    Question = "No Changes Made.",
                    Origin = ControllerAction.Exception.ChangeAct,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.ChangeActStep3,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };
                return View("ConfirmOk", vmConfirm);
            }
            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vmConfirm"></param>
        /// <returns></returns>
        public ActionResult ChangeActStep3(MessageDisplay vmConfirm)
        {
            var helper = Session[SessionKey.Helper] as ChangeActButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var PDocNo = Session[SessionKey.PDocNo].ToString();
            var strACT = Session[SessionKey.StrAct].ToString();

            var Continue = helper.Initialize(exception, PrepCode, strACT);

            if (Continue == true)
            {
                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.ChangeAct, ControllerAction.Exception.Index, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }
            //Can't really do this because it needs to update the screen without refreshing it
            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult ChangeActFinish(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            //Need to enable the PO Request button on the screen when it returns
            //THISFORM.cmdPoRequest1.ENABLED = true;

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Change Pdocno for M200/P200 exceptions - this needs to update on the screen with AJAX, but needs to display message and Input screens also
        /// </summary>
        /// <param name="newPDocNo"></param>
        /// <returns></returns>
        public ActionResult ChangePdocno(string newPDocNo)
        {
            if (string.IsNullOrWhiteSpace(newPDocNo))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.Write("A new PDocNo is required.\nPlease enter it in the text box.");
                return Json(null, JsonRequestBehavior.AllowGet);
            }
            var helper = new ChangePdocnoButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.Initialize(exception, exception.PDOCNO, PrepCode);
            helper.FinishCode(newPDocNo);

            return new EmptyResult();
        }

        /// <summary>
        /// Called from the ConfirmyesNo screen
        /// </summary>
        /// <param name="vmConfirm"></param>
        /// <returns></returns>
        public ActionResult ChangePdocnoMessage(MessageDisplay vmConfirm)
        {
            if (vmConfirm.Response == false) { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            //Need to open the Input screen for user input
            var vmInput = new InputViewModel()
            {
                Label = "Enter the New PO Doc Num: ",
                InputTxt = "",
                Origin = ControllerAction.Exception.ChangePdocno,
                ReturnAction = ControllerAction.Exception.ChangePdocnoInput,
                ReturnController = "Exceptions",
            };
            return View("Other/Input", vmInput);
        }

        /// <summary>
        /// Called from the Input Screen
        /// </summary>
        /// <param name="vmInput"></param>
        /// <returns></returns>
        public ActionResult ChangePdocnoInput(InputViewModel vmInput)
        {
            var helper = Session[SessionKey.Helper] as ChangePdocnoButton;
            var PDocNo = Session[SessionKey.PDocNo].ToString();

            var strNewPdocno = vmInput.InputTxt.Trim();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            if (String.IsNullOrWhiteSpace(strNewPdocno))
            {
                return RedirectToAction("Index", new { request, search });
            }

            if (strNewPdocno != PDocNo)
            {
                helper.FinishCode(strNewPdocno);
            }

            //Need to update the PdocNo to the new one and enable the PO Request button on the screen
            //THISFORM.r_pdocno = strNewPdocno
            //THISFORM.cmdporequest1.ENABLED = true

            return RedirectToAction("Index", new { request, search });
        }


        /// <summary>
        /// CorrectDe button for PegasysMainExceptions, PegVendorExceptions screens - Opens the Notes screen
        /// </summary>
        /// <param name="InvQuery"></param>
        /// <param name="POFrmQuery"></param>
        /// <param name="Search"></param>
        /// <param name="PDocNo"></param>
        /// <param name="sNotesType"></param>
        /// <param name="POExists"></param>
        /// <param name="InvExists"></param>
        /// <param name="RRExists"></param>
        /// <param name="cImageID"></param>
        /// <returns></returns>
        public ActionResult CorrectDE(PEGASYSINVOICE InvQuery, PEGASYSPO_FRM POFrmQuery, AddressValuesModel Search, string PDocNo, string sNotesType,
                bool POExists, bool InvExists, bool RRExists, string cImageID)
        {
            var helper = new CorrectDEButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, PDocNo, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.InvQuery] = InvQuery;
                Session[SessionKey.Pofrmquery] = POFrmQuery;
                Session[SessionKey.Search] = Search;
                Session[SessionKey.PDocNo] = PDocNo;
                Session[SessionKey.POExists] = POExists;
                Session[SessionKey.InvExists] = InvExists;
                Session[SessionKey.RRExists] = RRExists;
                Session[SessionKey.SNotesType] = sNotesType ?? "";

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.CorrectDE, ControllerAction.Exception.CorrectDENotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            //If not continued it should go back to the calling exception screen
            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PO_ID"></param>
        /// <param name="RR_ID"></param>
        /// <param name="PDocNo"></param>
        /// <returns></returns>

        public ActionResult ChangeM237(List<AEaccountingModel> matchLineRecs)
        {
            var helper = new ChangeButton();

            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;
            Session[SessionKey.Helper] = helper;
            Session[SessionKey.PDocNo] = exception.PDOCNO;

            TempData[SessionKey.M237Model] = matchLineRecs;


            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.Change, ControllerAction.Exception.ChangeNotes, "Exceptions", string.Empty);

            TempData["Change237vmNotes"] = vmNotes;

            return Json(new { success = true, Notes = vmNotes }, JsonRequestBehavior.AllowGet);

        }

        public ActionResult ChangeM237Notes()
        {
            return View("other/notes", TempData.Peek("Change237vmNotes") as NotesViewModel);

        }


        /// <summary>
        /// This is called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult CorrectDENotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as CorrectDEButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var InvQuery = Session[SessionKey.InvQuery] as PEGASYSINVOICE;
            var POFrmQuery = Session[SessionKey.Pofrmquery] as PEGASYSPO_FRM;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var POExists = Session[SessionKey.POExists] as bool?;
            var InvExists = Session[SessionKey.InvExists] as bool?;
            var RRExists = Session[SessionKey.RRExists] as bool?;
            var sNotesType = Session[SessionKey.SNotesType] != null ? Session[SessionKey.SNotesType].ToString() : null;
            var PDocNo = Session[SessionKey.PDocNo].ToString();

            helper.Initialize(exception, PDocNo, roles.PREPCODE);
            helper.SetNotes(vmNotes);
            helper.FinishCode(InvQuery, POFrmQuery, Search, sNotesType, POExists.Value, InvExists.Value, RRExists.Value);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// CorrectDE button for User exception screen - Opens up the notes and input screens screen - Labeled Change or Review on the buttons)
        /// </summary>
        /// <param name="invKeyId"></param>
        /// <param name="InvExists"></param>
        /// <returns></returns>
        public ActionResult CorrectDEUser(string invKeyId, bool InvExists)
        {
            var helper = new CorrectDEUserButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var rtnInv = helper.GetPegasysInvoiceByKeyId(invKeyId);
            Session[SessionKey.InvKeyId] = invKeyId;
            Session[SessionKey.InvExists] = InvExists;

            bool Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.CorrectDE, ControllerAction.Exception.CorrectDEUserNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        /// <summary>
        /// Get Notes for the CorrectDE button
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult CorrectDEUserNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var InvExists = (Session[SessionKey.InvExists] as bool?).Value;

            if (InvExists)
            {
                Session[SessionKey.VmNotes] = vmNotes;
                //InvEdit did have r_OK, r_Back2U044 and r_Invoice.TEXT values along with a m_pUpdateInv() method call but doesn't now
                var invoice = GetInvEditViewModel();
                TempData["vmInvoice"] = invoice;
                vmNotes.returnValZ = "";
                return View("Other/InvEdit", invoice);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult CorrectDEUserInvEdit(InvEditViewModel invoice)
        {
            var invKeyId = Session[SessionKey.InvKeyId].ToString();
            var InvExists = (Session[SessionKey.InvExists] as bool?).Value;
            var sNotesType = "";

            var helper = Session[SessionKey.Helper] as CorrectDEUserButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var rtnInv = helper.GetPegasysInvoiceByKeyId(invKeyId);
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            helper.Initialize(exception, roles.PREPCODE);
            Session[SessionKey.Helper] = helper;
            Session[SessionKey.RtnInv] = rtnInv;

            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;

            if (!InvExists)
            {
                //Need to Disable the button
            }
            else
            {
                sNotesType = "";

                if (exception.ERR_CODE == "U049")
                {
                    if (invoice.ButtonPushed == "Back2U044") { sNotesType = "RESET"; }
                }
                else if (exception.ERR_CODE == "U044")
                {
                    sNotesType = "U044CRCT";
                }

                //Called the m_TempClick method on the User Exception screen before
                CorrectDETemp(helper, exception, vmNotes, sNotesType, InvExists, rtnInv);

                if (vmNotes.returnVal1 == "FINISH")
                {
                    //Calls the m_pUpdateInv() method from the InvEdit screen
                    helper.UpdateInvoice(invoice);
                }
            }
            helper.SetNotes(vmNotes);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        [ValidateInput(false)]
        //Part of the CorrectDEUser Button Code
        private ActionResult CorrectDETemp(CorrectDEUserButton helper, EXCEPTION exception, NotesViewModel notes, string sNotesType, bool InvExists, PEGASYSINVOICE InvQuery)
        {
            //Sets initial values for Notes screen
            notes.returnVal1 = "NONE";
            notes.returnVal7 = exception.FAXNOTES;

            Session[SessionKey.InvExists] = InvExists;
            Session[SessionKey.Exception] = exception;
            Session[SessionKey.InvQuery] = InvQuery;
            Session[SessionKey.Helper] = helper;

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.CorrectDE, ControllerAction.Exception.CorrectDETemp, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);
        }

        /// <summary>
        /// Called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult CorrectDETempNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            if (vmNotes.returnVal1 != "FINISH")
            {
                return RedirectToAction("Index", new { request, search });
            }

            var invoice = GetInvEditViewModel();
            TempData["vmInvoice"] = invoice;
            vmNotes.returnValZ = "";
            return View("Other/InvEdit", invoice);
            var NotesTypeTemp = "";
            var InvExists = (Session[SessionKey.InvExists] as bool?).Value;
            var exception = Session[SessionKey.Exception] as EXCEPTION;
            var InvQuery = Session[SessionKey.InvQuery] as PEGASYSINVOICE;
            var helper = Session[SessionKey.Helper] as CorrectDEUserButton;

            if (InvExists)
            {
                //Disable button - Look to do this in the screen instead of here
            }
            else
            {
                if (InvExists)
                {
                    if (exception.ERR_CODE == "U049")
                    {
                        //r_Back2U044 does not exist as a returned value from the InvEdit screen
                        if (invoice.ButtonPushed == "Back2U044") // When Back to U044 button clicked in InvEdits
                        {
                            NotesTypeTemp = "RESET";
                        }
                    }
                    else if (exception.ERR_CODE == "U044")
                    {
                        if (invoice.InvoiceNumber.Trim() == InvQuery.INVOICE.Trim())
                        {
                            //If accepted with no change to Invoice number
                            NotesTypeTemp = "U044CRCT";
                        }
                        else
                        {
                            //If Invoice number changed, then need Manager's permission
                            NotesTypeTemp = "U049CRCT";
                        }
                    }

                    var helper2 = Session[SessionKey.Helper] as CorrectDEButton;
                    var Search = new AddressValuesModel();
                    var roles = Session[SessionKey.RoleModel] as RoleListModel;
                    Search.VENDORNAME = InvQuery.VENDNAME;
                    var PDocNo = InvQuery.PDOCNOPO;
                    var POFrmQuery = helper.GetPegasysPOFrmById(PDocNo);
                    helper2.Initialize(exception, PDocNo, roles.PREPCODE);
                    helper2.FinishCode(InvQuery, POFrmQuery, Search, NotesTypeTemp, false, true, false);
                }
            }
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Goes to the transhist List screens depending on values set in the exceptions table - hits the first one to be populated
        /// NotificationExceptions, UserExceptions screens contain the button
        /// </summary>
        /// <returns></returns>
        public ActionResult List()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (!String.IsNullOrWhiteSpace(exception.RR_ID))
            {
                var Id = exception.RR_ID;
                var Recur_ID = null as string;
                return RedirectToAction("ListRR", "TransHist", new { Id, exception.RR_ID, Recur_ID, exception.ACT, exception.PDOCNO });
            }
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                var Id = exception.INV_KEY_ID;
                return RedirectToAction("ListInv", "TransHist", new { Id, exception.INV_KEY_ID, exception.ACT, exception.PDOCNO });
            }
            if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                var PO4List = exception.PO_ID;
                if (exception.PO_ID.Contains("&"))
                {
                    PO4List = exception.PO_ID.Left(exception.PO_ID.IndexOf('&'));
                }
                var Id = exception.PO_ID;
                return RedirectToAction("ListPO", "TransHist", new { Id, exception.PO_ID, exception.ACT, exception.PDOCNO });
            }
            if (!String.IsNullOrWhiteSpace(exception.AE_ID))
            {
                var Id = exception.AE_ID;
                return RedirectToAction("ListInv", "TransHist", new { Id, exception.AE_ID, exception.ACT, exception.PDOCNO });
            }
            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Goes to the List RR screen from NotificationExceptions, P039Exception, PegException202 screens 
        /// </summary>
        /// <returns></returns>
        public ActionResult ListRR()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var Id = exception.RR_ID;
            var Recur_ID = null as string;

            return View("ListRR", "TransHist", new { Id, exception.RR_ID, Recur_ID, exception.ACT, exception.PDOCNO });
        }

        /// <summary>
        /// Not A Mod button for PegException_200, PegasysMainExceptions, R200Exception screens - Displays notes and message screens
        /// </summary>
        /// <param name="Pdocno"></param>
        /// <param name="prepCode"></param>
        /// <param name="Edi_Ind"></param>
        /// <returns></returns>
        public ActionResult NotAMod(string Pdocno, string Edi_Ind)
        {
            var helper = new NotAModButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, Pdocno, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.EdiInd] = Edi_Ind;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.NotAMod, ControllerAction.Exception.NotAModNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        /// <summary>
        /// Called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult NotAModNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as NotAModButton;
            var Edi_Ind = Session[SessionKey.EdiInd].ToString();

            helper.SetNotes(vmNotes);
            var exception = TempData.Peek("exception") as EXCEPTION;
            var Count = helper.FinishCode1();
            if (Count > 1)
            {
                //Needs to redirect to the exception screen 
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception",
                    Question = "There is already a PO for this Document Number. Your request cannot be processed.",
                    Origin = ControllerAction.Exception.NotAMod,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.Index,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            helper.FinishCode2(Edi_Ind);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Not This One button for P039Exception, P140Exception, PegasysMainExceptions screens - displays notes screen
        /// </summary>
        /// <param name="TheQueue"></param>
        /// <param name="RRChoice"></param>
        /// <param name="Pdocno"></param>
        /// <returns></returns>
        public ActionResult NotThisOne(string Pdocno)
        {
            var helper = new NotThisOneButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            ExceptionP140ViewModel p140 = new ExceptionP140ViewModel();
            if (exception.ERR_CODE == "P140")
            {
                p140 = Session[SessionKey.P140Model] as ExceptionP140ViewModel;
            }
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, Pdocno, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.TheQueue] = null;
                //Session[SessionKey.TheQueue] = TheQueue;
                Session[SessionKey.Rrchoice] = p140.RRChoice;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.NotThisOne, ControllerAction.Exception.NotThisOneNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult NotThisOneNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as NotThisOneButton;
            var TheQueue = Session[SessionKey.TheQueue] as List<NoRRArray>;
            var RRChoice = Session[SessionKey.Rrchoice] as List<RRCHOICE>;

            helper.SetNotes(vmNotes);
            helper.FinishCode(TheQueue, RRChoice);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// PO Match button for R200Exception screen - Displays notes screen
        /// </summary>
        /// <param name="Pdocno"></param>
        /// <param name="NewAct"></param>
        /// <param name="NewPDocNo"></param>
        /// <param name="Vcpo"></param>
        /// <param name="StartDate"></param>
        /// <param name="X200"></param>
        /// <param name="vpo"></param>
        /// <returns></returns>
        public ActionResult POMatch(string Pdocno, string NewAct, string NewPDocNo, string Vcpo)
        {
            var mgr = new PegasysPO_FrmManager();
            var helper = new POMatchButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            var vpo = mgr.GetPegasysPOFrmByKey(exception.PO_ID);

            if (Vcpo != null && Vcpo != "T")
            {
                var pegasyspoRecord = new PegasysPOManager().GetPegaysPOByPoId(NewPDocNo);

                if (pegasyspoRecord != null && pegasyspoRecord.VCPO == "T")
                {
                    Vcpo = "T";
                }
            }

            var Continue = helper.Initialize(exception, Pdocno, NewPDocNo, PrepCode);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.PDocNo] = Pdocno.ReplaceNull("");
                Session[SessionKey.NewAct] = NewAct.ReplaceNull("");
                Session[SessionKey.NewPDocNo] = NewPDocNo.ReplaceNull("");
                Session[SessionKey.Vcpo] = Vcpo;
                Session[SessionKey.Vpo] = vpo;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.POMatch, ControllerAction.Exception.POMatchNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        /// <summary>
        /// Called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult POMatchNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as POMatchButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var Pdocno = Session[SessionKey.PDocNo].ToString();
            var NewAct = Session[SessionKey.NewAct].ToString();
            var NewPDocNo = Session[SessionKey.NewPDocNo].ToString();
            var Vcpo = Session[SessionKey.Vcpo].ToString();
            var vpo = Session[SessionKey.Vpo] as PEGASYSPO_FRM;
            DateTime? StartDate = null;
            if (exception.ERR_CODE == "R200")
            {
                var R200Model = Session[SessionKey.R200Model] as ExceptionR200ViewModel;
                StartDate = R200Model.SVC_PERD_STRT;
            }

            if (vmNotes.returnVal1 == "FINISH" || exception.ERR_CODE.Substring(0, 1) == "K")
            {
                {
                    helper.SetNotes(vmNotes);
                    helper.FinishCode(NewAct, NewPDocNo, Vcpo, StartDate, vpo);
                }
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// PO Match button for PegException_200 (Override) screen - displays notes and message screens
        /// </summary>
        /// <param name="Pdocno"></param>
        /// <param name="prepCode"></param>
        /// <param name="NewAct"></param>
        /// <param name="NewPDocNo"></param>
        /// <param name="Vcpo"></param>
        /// <param name="StartDate"></param>
        /// <param name="X200"></param>
        /// <param name="vpo"></param>
        /// <returns></returns>
        public ActionResult POMatchP200(string Pdocno, string NewAct, string NewPDocNo, string Vcpo, DateTime? StartDate, string X200Uidy, string X200Dscr)
        {
            var helper = new POMatchP200Button();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var mgr = new PegasysPO_FrmManager();
            var vpo = mgr.GetPegasysPOFrmByKey(exception.PO_ID);
            var Continue = helper.Initialize(exception, Pdocno, roles.PREPCODE, NewPDocNo);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.PDocNo] = Pdocno.ReplaceNull("");
                Session[SessionKey.NewAct] = NewAct.ReplaceNull("");
                Session[SessionKey.NewPDocNo] = NewPDocNo.ReplaceNull("");
                Session[SessionKey.Vcpo] = Vcpo;
                Session[SessionKey.StartDate] = StartDate;
                Session[SessionKey.Vpo] = vpo;
                Session[SessionKey.X200Uidy] = X200Uidy;
                Session[SessionKey.X200Dscr] = X200Dscr;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.POMatch, ControllerAction.Exception.POMatchP200Notes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult POMatchP200Notes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as POMatchP200Button;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var Pdocno = Session[SessionKey.PDocNo].ToString();
            var NewAct = Session[SessionKey.NewAct].ToString();
            var NewPDocNo = Session[SessionKey.NewPDocNo].ToString();
            var Vcpo = Session[SessionKey.Vcpo].ToString();
            var StartDate = Session[SessionKey.StartDate] as DateTime?;
            var X200Uidy = Session[SessionKey.X200Uidy].ToString();
            var X200Dscr = Session[SessionKey.X200Dscr].ToString();
            var vpo = Session[SessionKey.Vpo] as PEGASYSPO_FRM;

            helper.SetNotes(vmNotes);
            var ItemizedPO = helper.FinishCode1(NewPDocNo, X200Dscr, X200Uidy);
            var rtnPO = helper.FinishCodeP2001B(NewPDocNo, X200Dscr);

            if (rtnPO != null)
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "1B or RO",
                    Question = "This Variable Contract PO has been converted to an 'RO' type.  Are you sure this " +
                        "invoice should match the 1B instead of an RO?",
                    Origin = ControllerAction.Exception.POMatchP200,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.POMatchP200Message,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vmConfirm"></param>
        /// <returns></returns>
        public ActionResult POMatchP200Message(MessageDisplay vmConfirm)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            if (vmConfirm.Response == false)
            {
                vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "PO Match Cancelled",
                    Origin = ControllerAction.Exception.POMatchP200,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.Index,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }

            var X200Uidy = Session[SessionKey.X200Uidy].ToString();
            var X200Dscr = Session[SessionKey.X200Dscr].ToString();
            var NewPDocNo = Session[SessionKey.NewPDocNo].ToString();

            if (exception.ERR_CODE == "P200" && NewPDocNo.Left(2) == "2B" && X200Dscr.ToString().ToLower().Contains("corrected to close for gsa preferred"))
            {
                vmConfirm = new MessageDisplay()
                {
                    Title = "Pegasys Exception",
                    Question = "The Pegasys PO Description indicates that this Order was converted to GSA Preferred." +
                    " This invoice should probably match a 2I order. Are you sure you" +
                    "want to match this invoice to the 2B?",
                    Origin = ControllerAction.Exception.POMatchP200Message,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.POMatchP200Step2,
                    ReturnController = "Exceptions",
                };

                return View("ConfirmYesNo", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// This is called from the second ConfirmYesNo Message
        /// </summary>
        /// <param name="vmConfirm"></param>
        /// <returns></returns>
        public ActionResult POMatchP200Step2(MessageDisplay vmConfirm)
        {
            var X200Dscr = Session[SessionKey.X200Dscr].ToString();
            if (X200Dscr.ToUpper().Contains("DO NOT USE"))
            {
                var exception = TempData.Peek("exception") as EXCEPTION;
                vmConfirm = new MessageDisplay()
                {
                    Title = "Pegasys Exception",
                    Question = "The Pegasys PO Description says 'DO NOT USE'. Are You sure you " +
                        "want to match the document to this PO?",
                    Origin = ControllerAction.Exception.POMatchP200Step2,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.POMatchP200Step3,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }
            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult POMatchP200Step3(MessageDisplay vmConfirm)
        {
            var helper = Session[SessionKey.Helper] as POMatchP200Button;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var Pdocno = Session[SessionKey.PDocNo].ToString();
            var NewAct = Session[SessionKey.NewAct].ToString();
            var NewPDocNo = Session[SessionKey.NewPDocNo].ToString();
            var Vcpo = Session[SessionKey.Vcpo].ToString();
            var StartDate = Session[SessionKey.StartDate] as DateTime?;
            var vpo = Session[SessionKey.Vpo] as PEGASYSPO_FRM;

            return View("POMatch", new { Pdocno, NewAct, NewPDocNo, Vcpo, StartDate });
        }

        /// <summary>
        /// PO Mod button for PegasysMainExceptions, PegVendorExceptions screens - Displays notes and message screens
        /// </summary>
        /// <param name="Invoice"></param>
        /// <param name="InvAmount"></param>
        /// <param name="POAmount"></param>
        /// <param name="VendName"></param>
        /// <param name="Search"></param>
        /// <param name="Address"></param>
        /// <returns></returns>
        public ActionResult POMod(string Invoice, double? InvAmount, double? POAmount, string VendName)
        {
            var helper = new POModButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.Invoice] = Invoice;
                Session[SessionKey.InvAmount] = InvAmount;
                Session[SessionKey.POAmount] = POAmount;
                Session[SessionKey.VendName] = VendName;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.POMod, ControllerAction.Exception.POModNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult POModNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as POModButton;
            var Invoice = Session[SessionKey.Invoice].ToString();
            double InvAmount = 0;
            if (Session[SessionKey.InvAmount] != null)
            {
                double.TryParse(Session[SessionKey.InvAmount].ToString(), out InvAmount);
            }

            double POAmount = 0;
            if (Session[SessionKey.POAmount] != null)
            {
                double.TryParse(Session[SessionKey.POAmount].ToString(), out POAmount);
            }

            var VendName = Session[SessionKey.VendName].ToString();

            vmNotes.FaxNotes = vmNotes.returnVal7;
            helper.SetNotes(vmNotes);

            var Success = helper.FinishCode(Invoice, InvAmount, POAmount, VendName, PrepCode);
            if (!Success)
            {
                var exception = TempData.Peek("exception") as EXCEPTION;
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "Unexplained ERROR - Mod request not created!",
                    Origin = ControllerAction.Exception.POMod,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                View("ConfirmOk", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// PO Request button for PegException_200 and R200Exception screens - Displays notes screen
        /// </summary>
        /// <returns></returns>
        public ActionResult PORequest(string pDocNo)
        {
            var helper = new PORequestButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            var Continue = helper.Initialize(exception, PrepCode, pDocNo);

            if (Continue == true)
            {
                Session["helper"] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.PORequest, ControllerAction.Exception.PORequestNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult PORequestNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as PORequestButton;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            helper.SetNotes(vmNotes);
            //helper.FinishCode(roles.PREPCODE);
            helper.FinishCode(PrepCode);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Print button for UserExceptions screen - Displays notes screen
        /// </summary>
        /// <returns></returns>
        public ActionResult Print()
        {
            var helper = new PrintButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.Print, ControllerAction.Exception.PrintNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult PrintNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as PrintButton;
            helper.SetNotes(vmNotes);
            helper.FinishCode();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Processed button for PegasysMainExceptions screen - Displays notes and message screens
        /// </summary>
        /// <returns></returns>
        public ActionResult Processed()
        {
            var helper = new ProcessedButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;
            Session[SessionKey.Helper] = helper;

            var Message = helper.Initialize(exception, roles.PREPCODE);

            if (!String.IsNullOrWhiteSpace(Message))
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Information",
                    Question = Message,
                    Origin = ControllerAction.Exception.Processed,
                    Controller = "Exceptions",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.Processed, ControllerAction.Exception.ProcessedNotes, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);
        }

        /// <summary>
        /// Called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult ProcessedNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as ProcessedButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.SetNotes(vmNotes);
            var Message = helper.FinishCode();

            if (!String.IsNullOrWhiteSpace(Message))
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Information",
                    Question = Message,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Needs to return X200 list to the screen
        /// </summary>
        /// <param name="FieldName"></param>
        /// <param name="SearchValue"></param>
        /// <returns></returns>
        public ActionResult R200Search(DataSourceRequest request, R200Search r200Search)
        {
            var helper = new R200SearchButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var model = Session[SessionKey.R200Model] as ExceptionR200ViewModel;
            var X200 = new List<X200Model>();
            if (r200Search.SearchValue == null)
            {
                var resultFailed = X200.ToDataSourceResult(request);
                return Json(resultFailed, JsonRequestBehavior.AllowGet);
            }
            X200 = helper.Initialize(r200Search.FieldName.ToUpper(), r200Search.SearchValue, exception.PO_ID);

            Session[SessionKey.X200] = X200;

            //If X200 is populated, enable the POMatch button and also enable and populate the grid on the screen.
            model.R200Recs = X200;
            var results = X200.ToDataSourceResult(request);

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Recycle button for NotificationExceptions, PegasysMainExceptions screens - Displays notes and message screens
        /// </summary>
        /// <param name="Invoice"></param>
        /// <param name="InvAmount"></param>
        /// <param name="POAmount"></param>
        /// <param name="VendName"></param>
        /// <param name="Search"></param>
        /// <param name="Address"></param>
        /// <returns></returns>
        public ActionResult Recycle(String ExId)
        {
            var helper = new RecycleButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            if (exception == null)
            {
                ExceptionsManager exManager = new ExceptionsManager();
                exception = exManager.GetExceptionByExId(ExId);
            }

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.Recycle, ControllerAction.Exception.RecycleNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult RecycleNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as RecycleButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.SetNotes(vmNotes);
            var rtnRRInv = helper.FinishCode1();

            if (rtnRRInv != null && rtnRRInv.Count > 0)
            {
                Session[SessionKey.RtnRRInv] = rtnRRInv;

                var vmConfirm = new MessageDisplay()
                {
                    Title = "Clear",
                    Question = "Do you want to clear Mismatched RRs and start over matching?",
                    ReturnAction = ControllerAction.Exception.RecycleMessage,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }
            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult RecycleMessage(MessageDisplay vmConfirm)
        {
            var helper = Session[SessionKey.Helper] as RecycleButton;

            if (vmConfirm.Response)
            {
                var rtnRRInv = Session[SessionKey.RtnRRInv] as List<MATCHRRINV>;
                helper.DeleteRRInv(rtnRRInv);
            }

            helper.FinishCode2();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// ReferencingInvoices used for the R200 screen - Displays message or R200InvList screens
        /// </summary>
        /// <param name="Pdocno"></param>
        /// <returns></returns>
        public ActionResult ReferencingInvoices(string Pdocno)
        {
            var helper = new ReferencingInvoicesButton();
            var rtnMFIIInv = helper.Intialize(Pdocno);
            if (rtnMFIIInv.Count > 0)
            {
                //This needs to be addressed and the screen created
                //It opens and displays a grid with the list above
                var r200Inv = new R200ListInv();
                r200Inv.Title = rtnMFIIInv.Count.ToString() + " invoices found for RO " + Pdocno.Trim();
                r200Inv.R200List = rtnMFIIInv;
                return View("R200/R200InvList", r200Inv);
            }
            else
            {
                var exception = TempData.Peek("exception") as EXCEPTION;
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "No Invoice's Found for RO " + Pdocno + "...",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);

            }
        }

        /// <summary>
        /// Reject button for P039Exception, P140Exception, PegException_200, PegasysMainExceptions, PegVendorExceptions (override), 
        /// R200Exception, U066Exception, UserExceptions screens - Displays notes, mandmemo and message screens
        /// </summary>
        /// <param name="PDocNo"></param>
        /// <returns></returns>
        public ActionResult Reject(string PDocNo)
        {
            var helper = new RejectButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var theMsg = helper.Initialize(exception, PDocNo, PrepCode);

            if (!String.IsNullOrWhiteSpace(theMsg))
            {
                //Return to exception screen
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception",
                    Question = theMsg,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            else
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.PDocNo] = PDocNo;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.Reject, ControllerAction.Exception.RejectNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }
        }

        [ValidateInput(false)]
        public ActionResult RejectNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as RejectButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            Session[SessionKey.VmNotes] = vmNotes;

            //Display the MandMemo screen
            var title = "Faxnotes:";
            var thismemo = "";
            var vmMandMemo = new MandMemoViewModel()
            {
                Origin = "Reject",
                Title = title,
                NoteValue = thismemo,
                ReturnAction = ControllerAction.Exception.RejectMandMemo,
                ReturnController = "Exceptions",
            };

            vmMandMemo.NoteValue = vmNotes.returnVal7;
            helper.SetNotes(vmNotes);
            Session[SessionKey.Helper] = helper;

            RejectMandMemo(vmMandMemo);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult RejectMandMemo(MandMemoViewModel vmMandMemo)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            Session[SessionKey.VmMandMemo] = vmMandMemo;

            if (exception.ERR_CODE.InList("U043,U044,U084"))
            {
                if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                {
                    RejectUserInvoiceExceptions();
                }
                else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
                {
                    RejectUserRRExceptions();
                }
                else if (String.IsNullOrWhiteSpace(exception.PO_ID))
                {
                    RejectUserPOExceptions();
                }
            }
            else if (exception.ERR_CODE == "U049")
            {
                RejectU049Exceptions();
            }
            else
            {
                RejectOtherExceptions();
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Reject User Invoice Exceptions
        /// </summary>
        /// <returns></returns>
        public ActionResult RejectUserInvoiceExceptions()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var helper = Session[SessionKey.Helper] as RejectButton;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var vmMandMemo = Session[SessionKey.VmMandMemo] as MandMemoViewModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var mgr = new PegasysRRManager();
            var RRFrmQuery = mgr.GetPegasysRRByKey(exception.RR_ID);
            var mgr2 = new PegasysPO_FrmManager();
            var POFrmQuery = mgr2.GetPegasysPOFrmByKey(exception.PO_ID);
            var mgr3 = new PegasysInvoiceManager();
            var InvQuery = mgr3.GetPegasysInvoiceByKeyId(exception.INV_KEY_ID);

            var Ex_Date = exception.EX_DATE;

            if (!vmNotes.returnVal2.Contains("DUPLICATE"))
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    if (vmMandMemo.NoteValue == "CANCEL")
                    {
                        //Returns to the exception screen 
                        var vmConfirm = new MessageDisplay()
                        {
                            Title = "",
                            Question = "Only Notification cancelled - Invoice will still be rejected!",
                            ReturnAction = ControllerAction.Exception.DisplayException,
                            ReturnController = "Exceptions",
                            ExId = exception.EX_ID,
                            ErrCode = exception.ERR_CODE,
                        };

                        return View("ConfirmOk", vmConfirm);
                    }
                }
            }

            helper.SetNotes(vmNotes);
            helper.FinishCode(exception, Search, Address, InvQuery, RRFrmQuery, POFrmQuery, PrepCode);
            helper.HandleInvoice(Search, vmMandMemo.NoteValue);


            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Reject User RR Exceptions
        /// </summary>
        /// <returns></returns>
        public ActionResult RejectUserRRExceptions()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var helper = Session[SessionKey.Helper] as RejectButton;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var mgr = new PegasysRRManager();
            var RRFrmQuery = mgr.GetPegasysRRByKey(exception.RR_ID);
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var vmMandMemo = Session[SessionKey.VmMandMemo] as MandMemoViewModel;

            if (!vmNotes.returnVal2.Contains("DUPLICATE"))
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    if (vmMandMemo.NoteValue == "CANCEL")
                    {
                        var vmConfirm = new MessageDisplay()
                        {
                            Title = "",
                            Question = "Only Notification cancelled - RR will still be rejected!",
                            ReturnAction = ControllerAction.Exception.DisplayException,
                            ReturnController = "Exceptions",
                            ExId = exception.EX_ID,
                            ErrCode = exception.ERR_CODE,
                        };

                        View("ConfirmOk", vmConfirm);
                    }
                }
            }

            helper.SetNotes(vmNotes);
            helper.FinishCode(exception, Search, null, null, RRFrmQuery, null, PrepCode);
            helper.HandleRR(Search, RRFrmQuery, vmMandMemo.NoteValue);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Reject User PO Exceptions
        /// </summary>
        /// <returns></returns>
        public ActionResult RejectUserPOExceptions()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var helper = Session[SessionKey.Helper] as RejectButton;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var mgr = new PegasysPO_FrmManager();
            var POFrmQuery = mgr.GetPegasysPOFrmByKey(exception.PO_ID);
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var vmMandMemo = Session[SessionKey.VmMandMemo] as MandMemoViewModel;

            if (!vmNotes.returnVal2.Contains("DUPLICATE"))
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    if (vmMandMemo.NoteValue == "CANCEL")
                    {
                        var vmConfirm = new MessageDisplay()
                        {
                            Title = "",
                            Question = "Only Notification cancelled - PO will still be rejected!",
                            ReturnAction = ControllerAction.Exception.DisplayException,
                            ReturnController = "Exceptions",
                            ExId = exception.EX_ID,
                            ErrCode = exception.ERR_CODE,
                        };

                        return View("ConfirmOk", vmConfirm);
                    }
                }
            }
            helper.SetNotes(vmNotes);
            helper.HandlePO(Search, Address, POFrmQuery, vmMandMemo.NoteValue);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Reject U049 exceptions
        /// </summary>
        /// <returns></returns>
        public ActionResult RejectU049Exceptions()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var helper = Session[SessionKey.Helper] as RejectButton;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var mgr = new PegasysRRManager();
            var RRFrmQuery = mgr.GetPegasysRRByKey(exception.RR_ID);
            var mgr2 = new PegasysPO_FrmManager();
            var POFrmQuery = mgr2.GetPegasysPOFrmByKey(exception.PO_ID);
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var vmMandMemo = Session[SessionKey.VmMandMemo] as MandMemoViewModel;

            var DateQueued = DateTime.Now;
            exception.PO_ID = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO;

            helper.ExceptionU049(exception, Search, Address, RRFrmQuery, POFrmQuery, vmMandMemo.NoteValue);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Reject other exceptions
        /// </summary>
        /// <returns></returns>
        public ActionResult RejectOtherExceptions()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var helper = Session[SessionKey.Helper] as RejectButton;
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var Search = Session[SessionKey.Search] as AddressValuesModel;
            var Address = Session[SessionKey.Address] as AddressValuesModel;
            var mgr = new PegasysRRManager();
            var RRFrmQuery = mgr.GetPegasysRRByKey(exception.RR_ID);
            var mgr2 = new PegasysPO_FrmManager();
            var POFrmQuery = mgr2.GetPegasysPOFrmByKey(exception.PO_ID);
            var mgr3 = new PegasysInvoiceManager();
            var InvQuery = mgr3.GetPegasysInvoiceByKeyId(exception.INV_KEY_ID);

            helper.SetNotes(vmNotes);
            helper.FinishCode(exception, Search, Address, InvQuery, RRFrmQuery, POFrmQuery, PrepCode);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult RejectM237Exception(string PDocNo)
        {
            var helper = new RejectButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            Session[SessionKey.Helper] = helper;
            Session[SessionKey.PDocNo] = PDocNo;

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.Reject, ControllerAction.Exception.RejectNotes, "Exceptions", string.Empty);

            return View("other/notes", vmNotes);

        }


        /// <summary>
        /// Reprocess button for PegasysMainExceptions screen - Displays notes and message screens
        /// </summary>
        /// <param name="InvQuery"></param>
        /// <param name="RRQuery"></param>
        /// <returns></returns>
        public ActionResult Reprocess(string pDocNoPO)
        {
            var helper = new ReprocessButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var theMsg = helper.Initialize(exception, pDocNoPO, roles.PREPCODE);
            Session[SessionKey.Helper] = helper;

            var vmConfirm = new MessageDisplay()
            {
                Title = "Reprocess?",
                Question = "",
                ReturnAction = ControllerAction.Exception.DisplayException,
                ReturnController = "Exceptions",
                ExId = exception.EX_ID,
                ErrCode = exception.ERR_CODE,
            };

            if (!String.IsNullOrWhiteSpace(theMsg) && theMsg.Contains("Is this what you want?"))
            {
                //If Yes continue processing, otherwise go back to the exception screen
                vmConfirm = new MessageDisplay()
                {
                    Title = "Reprocess?",
                    Question = theMsg,
                    ReturnAction = ControllerAction.Exception.ReprocessMessage1,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }
            else
            {
                if (!String.IsNullOrWhiteSpace(theMsg))
                {
                    //Redirect to the exception screen
                    vmConfirm = new MessageDisplay()
                    {
                        Title = "Exception",
                        Question = theMsg,
                        ReturnAction = ControllerAction.Exception.ReprocessMessage1,
                        ReturnController = "Exceptions",
                        ExId = exception.EX_ID,
                        ErrCode = exception.ERR_CODE,
                    };

                    return View("ConfirmOk", vmConfirm);
                }
            }

            return ReprocessMessage1(vmConfirm);
        }

        public ActionResult ReprocessMessage1(MessageDisplay vmConfirm)
        {
            //Exits out if No is clicked
            if (!vmConfirm.Response) { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            return RedirectToAction("ReprocessMessage1Hop");
        }

        //This is kind of dumb, but we can't pass query string params from the yes/no confirm page through to notes without breaking things
        public ActionResult ReprocessMessage1Hop()
        {
            var helper = Session[SessionKey.Helper] as ReprocessButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.Reprocess, ControllerAction.Exception.ReprocessNotes, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);
        }

        [ValidateInput(false)]
        public ActionResult ReprocessNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as ReprocessButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.SetNotes(vmNotes);
            if (helper.ReprocessPegInvoice())
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Reprocess",
                    Question = "Do you want to send back to try to match a different Order?",
                    ReturnAction = ControllerAction.Exception.ReprocessMessage2,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }
            else
            {
                var NewStatus = "REPROCESS";
                helper.ReprocessSetStatus(NewStatus);
            }

            helper.FinishCode();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult ReprocessMessage2(MessageDisplay vmConfirm)
        {
            //if (vmConfirm.Response == false) { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as ReprocessButton;
            var RRQuery = Session[SessionKey.RRQuery] as PEGASYSRR_FRM;
            var InvQuery = Session[SessionKey.InvQuery] as PEGASYSINVOICE;

            var NewStatus = "REPROCESS";

            if (vmConfirm.Response)
            {
                NewStatus = "REPROCESS2";
            }
            else
            {
                NewStatus = "REPROCESS";
            }
            helper.ReprocessSetStatus(NewStatus);
            helper.FinishCode();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Route Button for NotificationExceptions, PegException_200, PegasysMainExceptions, R200Exception, U066Exception screens - Displays notes and message screens
        /// </summary>
        /// <returns></returns>
        public ActionResult Route(string ExId)
        {
            var helper = new RouteButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (exception == null)
            {
                ExceptionsManager exManager = new ExceptionsManager();
                exception = exManager.GetExceptionByExId(ExId);
            }

            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var theMsg = helper.Initialize(exception, PrepCode);

            if (!String.IsNullOrWhiteSpace(theMsg))
            {
                //Return to exception screen
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception",
                    Question = theMsg,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            else
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.Route, ControllerAction.Exception.RouteNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }
        }

        [ValidateInput(false)]
        public ActionResult RouteNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as RouteButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            helper.SetNotes(vmNotes);
            helper.FinishCode();
            switch (exception.ERR_CODE.Right(3))
            {
                case "200":
                case "066":
                    var vmConfirm = new MessageDisplay()
                    {
                        Title = "",
                        Question = "You routed the document to " + vmNotes.returnVal4,
                        ReturnAction = ControllerAction.Exception.Index,
                        ReturnController = "Exceptions",
                        ExId = exception.EX_ID,
                        ErrCode = exception.ERR_CODE,
                    };

                    return View("ConfirmOk", vmConfirm);

                case "230":
                case "231":
                    if (exception.ERR_CODE == "P231" || exception.ERR_CODE.Right(3) == "230")
                    {
                        vmConfirm = new MessageDisplay()
                        {
                            Title = "",
                            Question = "You routed the document to " + vmNotes.returnVal4,
                            ReturnAction = ControllerAction.Exception.Index,
                            ReturnController = "Exceptions",
                            ExId = exception.EX_ID,
                            ErrCode = exception.ERR_CODE,
                        };

                        return View("ConfirmOk", vmConfirm);
                    }
                    break;
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// RRRequest button for P039Exception, PegasysMainExceptions screens - Displays notes screen
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult RRRequest()
        {
            var helper = new RRRequestButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.RRRequest, ControllerAction.Exception.RRRequestNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult RRRequestNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as RRRequestButton;

            var noRRList = Session[SessionKey.TheQueue] as List<NoRRArray>;

            helper.SetNotes(vmNotes);
            helper.FinishCode(noRRList);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Send Asap button for Notifications screen - Displays notes and message screens
        /// </summary>
        /// <param name="faxNotes"></param>
        /// <param name="faxNotes2"></param>
        /// <param name="newAddrName"></param>
        /// <returns></returns>
        public ActionResult SendAsap(string faxNotes, string faxNotes2, string newAddrName)
        {
            var helper = new SendAsapButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.FaxNotes] = faxNotes;
                Session[SessionKey.FaxNotes2] = faxNotes2;
                Session[SessionKey.NewAddrName] = newAddrName;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.SendAsap, ControllerAction.Exception.SendAsapNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [ValidateInput(false)]
        public ActionResult SendAsapNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var exception = TempData.Peek("exception") as EXCEPTION;
            var faxNotes = Session[SessionKey.FaxNotes].ToString();
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();
            Session[SessionKey.VmNotes] = vmNotes;

            if (faxNotes != faxNotes2)
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Change Fax Note",
                    Question = "Change Notification Fax Note?",
                    ReturnAction = ControllerAction.Exception.SendAsapMessage,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult SendAsapMessage(MessageDisplay vmConfirm)
        {
            if (vmConfirm.Response == false) { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as SendAsapButton;
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var faxNotes = Session[SessionKey.FaxNotes].ToString();
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();
            var newAddrName = Session[SessionKey.NewAddrName].ToString();

            if (exception.ERR_CODE == "P040" && exception.ERR_CODE == "P041" || exception.ERR_CODE == "P042")
            {
                SendAsapP04X();
            }

            helper.SetNotes(vmNotes);
            helper.FinishCode1(faxNotes, faxNotes2, newAddrName);

            helper.FinishCode2(faxNotes, faxNotes2, newAddrName, vmConfirm.Response);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult SendAsapP04X()
        {
            var faxNotes = Session[SessionKey.FaxNotes].ToString();
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();
            var newAddrName = Session[SessionKey.NewAddrName].ToString();
            var exception = TempData.Peek("exception") as EXCEPTION;

            var mgr = new NotificationsManager();
            var rtnNot = mgr.GetNotificationByExId(exception.EX_ID);
            Session[SessionKey.Notification] = rtnNot;

            if (rtnNot[0].ADDNAME != newAddrName)
            {
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Update names",
                    Question = "Additional Contact Names have been changed.  Do you want to update?",
                    ReturnAction = ControllerAction.Exception.SendAsapP04XMessage1,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult SendAsapP04XMessage1(MessageDisplay vmConfirm)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            if (vmConfirm.Response)
            {
                var newAddrName = Session[SessionKey.NewAddrName].ToString();
                var helper = Session[SessionKey.Helper] as SendAsapButton;
                var roles = Session[SessionKey.RoleModel] as RoleListModel;

                helper.FinishCodeP04X1(newAddrName, exception.ACT, roles.PREPCODE);
            }

            var faxNotes = Session[SessionKey.FaxNotes].ToString();
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();

            if (String.IsNullOrWhiteSpace(faxNotes) && String.IsNullOrWhiteSpace(faxNotes2))
            {
                //Need user input to determine whether to continue. If answer is Yes, exit out to let the user add fax note
                var vmConfirm2 = new MessageDisplay()
                {
                    Title = "Fax Note missing",
                    Question = "The fax note is empty. Do you need to add it?",
                    Origin = ControllerAction.Exception.SendAsap,
                    ReturnAction = ControllerAction.Exception.SendAsapMessage,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm2);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult SendAsapP04XMessage2(MessageDisplay vmConfirm)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            if (vmConfirm.Response == true)
            {
                //if answer is not Yes, exit out with the following:
                vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "No notification sent!",
                    Origin = ControllerAction.Exception.SendAsap,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }

            var helper = Session[SessionKey.Helper] as SendAsapButton;
            var vmNotes = Session[SessionKey.VmNotes] as NotesViewModel;
            var faxNotes = Session[SessionKey.FaxNotes].ToString();
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();
            var newAddrName = Session[SessionKey.NewAddrName].ToString();

            if (faxNotes != faxNotes2)
            {
                var vmConfirm2 = new MessageDisplay()
                {
                    Title = "Fax Change",
                    Question = "Change Notification Fax Note?",
                    Origin = ControllerAction.Exception.SendAsap,
                    ReturnAction = ControllerAction.Exception.SendAsapMessage,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm2);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        public ActionResult SendAsapP04XMessage3(MessageDisplay vmConfirm)
        {
            var faxNotes2 = Session[SessionKey.FaxNotes2].ToString();
            var helper = Session[SessionKey.Helper] as SendAsapButton;
            var rtnNot = Session[SessionKey.Notification] as List<NOTIFICATION>;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            if (vmConfirm.Response == true)
            {
                helper.FinishCodeP04X1(faxNotes2, rtnNot[0].ACT, roles.PREPCODE);
            }
            else
            {
                helper.FinishCodeP04X3(faxNotes2, rtnNot[0].ACT);
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// ShowUsedInvoices button for UserExceptions screen - Displays pick list screen
        /// </summary>
        /// <param name="InvQuery"></param>
        /// <returns></returns>
        public ActionResult ShowUsedInvoices(PEGASYSINVOICE InvQuery)
        {
            var helper = new ShowUsedInvoicesButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE, InvQuery);

            var rtnInv = helper.FinishCode(InvQuery);

            //This needs to be able to pass in the list (rtnInv) above and isn't set up that way currently.            
            GetPicklistView(rtnInv, InvQuery.ACT, InvQuery.PDOCNOPO, InvQuery.VEND_CD);

            //DO FORM picklist WITH 'Invoice numbers already used', 'XXXXXXXX', false;

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Skip button for Contact, InvEdit, P039Exception, P140Exception, PegasysMainExceptions, PegException_200, PegKeyException, PegVendorExceptions, 
        /// R200Exception, U066Exception, UserExceptions screens - Displays notes and message screens
        /// </summary>
        /// <param name="exceptionId"></param>
        /// <param name="PDocNo"></param>
        /// <returns></returns>
        public ActionResult Skip(string exceptionId, string PDocNo)
        {
            exceptionId = exceptionId.Replace("&amp;", "&");
            var helper = new SkipButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            if (exception == null)
            {
                var mgr = new ExceptionsManager();
                exception = mgr.GetExceptionByExId(exceptionId);
            }
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var theMsg = helper.Initialize(exception, PDocNo, exceptionId, roles.PREPCODE);

            if (!String.IsNullOrWhiteSpace(theMsg))
            {
                //Go back to the exception screen
                var vmConfirm = new MessageDisplay()
                {
                    Title = "Exception",
                    Question = theMsg,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            else
            {
                Session[SessionKey.Helper] = helper;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exceptionId, PDocNo,
                    ControllerAction.Exception.Skip, ControllerAction.Exception.SkipNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }
        }

        [ValidateInput(false)]
        public ActionResult SkipNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return View("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as SkipButton;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            helper.SetNotes(vmNotes);
            helper.FinishCode(roles.PREPCODE);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// Transhist button to open the transhist screen and populate with data - This may be eliminated since you can go directly there from the screen
        /// </summary>
        /// <returns></returns>
        public ActionResult TransHist()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            //string searchFor, string searchIn, string searchShow, string searchFY, string id, string act, string pDocNo, string searchedBy
            if (String.IsNullOrWhiteSpace(exception.ACT))
            {
                string searchFor = exception.PDOCNO, searchIn = "PDOCNO", searchShow = null, searchFY = null, id = null, searchedBy = null;
                return RedirectToAction("Index", "TransHist", new { searchFor, searchIn, searchShow, searchFY, id, exception.ACT, exception.PDOCNO, searchedBy });
            }
            else
            {
                string searchFor = exception.ACT, searchIn = "ACT", searchShow = null, searchFY = null, id = null, searchedBy = null;
                return RedirectToAction("Index", "TransHist", new { searchFor, searchIn, searchShow, searchFY, id, exception.ACT, exception.PDOCNO, searchedBy });
            }
        }

        /// <summary>
        /// UpdateAddName for NotificationExceptions screen - stays on exception screen - Displays message screen
        /// </summary>
        /// <param name="notification"></param>
        /// <param name="NewAddName"></param>
        /// <returns></returns>
        public ActionResult UpdateAddName(NOTIFICATION notification, string NewAddName)
        {
            var helper = new UpdateAddNameButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (notification.ADDNAME != NewAddName)
            {
                //Need to add this in the screen probably and only call this code if they are different:
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "Additional Contact Names have not been changed...",
                    Origin = ControllerAction.Exception.UpdateAddName,
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
            else
            {
                bool Continue = helper.Initialize(notification, NewAddName);

                //Need to figure out how to display the message below:
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "Additional Contact Names have been changed...",
                    Origin = ControllerAction.Exception.UpdateAddName,
                    ReturnAction = ControllerAction.Exception.Index,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmOk", vmConfirm);
            }
        }

        /// <summary>
        /// UnMatch button for PegKeyException
        /// Calls the Notes screen
        /// </summary>
        /// <param name="exceptionId"></param>
        /// <param name="invKeyId"></param>
        /// <returns></returns>
        public ActionResult UnMatch(string exceptionId, string invKeyId)
        {
            exceptionId = exceptionId.Replace("&amp;", "&");
            var helper = new UnMatchButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.InvKeyId] = invKeyId;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exceptionId, exception.PDOCNO,
                    ControllerAction.Exception.UnMatch, ControllerAction.Exception.UnMatchNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        /// <summary>
        /// Called from the Notes screen
        /// </summary>
        /// <param name="vmNotes"></param>
        /// <returns></returns>
        [ValidateInput(false)]
        public ActionResult UnMatchNotes(NotesViewModel vmNotes)
        {
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as UnMatchButton;
            var invKeyId = Session[SessionKey.InvKeyId].ToString();

            helper.SetNotes(vmNotes);
            helper.FinishCode(vmNotes.ExId, invKeyId);

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /// <summary>
        /// VendorExceptionSearch button for PegVendorException screen - Needs to stay on the exeption screen
        /// </summary>
        /// <param name="SearchType1"></param>
        /// <param name="SearchType2"></param>
        /// <param name="SearchValue1"></param>
        /// <param name="SearchValue2"></param>
        /// <param name="SearchVendCode"></param>
        /// <param name="SearchAddr_Cd"></param>
        /// <returns></returns>
        public ActionResult VendorExceptionSearch(string SearchType1, string SearchType2, string SearchValue1, string SearchValue2,
            string SearchVendCode, string SearchAddr_Cd)
        {
            var helper = new VendorExceptionSearchButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            //Probably need to do this in the screen before calling the button
            if (String.IsNullOrWhiteSpace(SearchValue1) && String.IsNullOrWhiteSpace(SearchValue2))
            {
                //Returns to exception screen
                var vmConfirm = new MessageDisplay()
                {
                    Title = "",
                    Question = "Please enter something to search.",
                    ReturnAction = ControllerAction.Exception.DisplayException,
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };
                return View("ConfirmOk", vmConfirm);
            }

            var vendorModel = helper.Initialize(exception, PrepCode, SearchType1, SearchType2, SearchValue1, SearchValue2, SearchVendCode, SearchAddr_Cd);

            //Need to return the data back to the screen, display it and handle the enabling of the Accept button in the screen
            if (vendorModel != null)
            {
                //Populate the Vendor Search Grid
                //THISFORM.pfrStuff.Page1.grdVendSearch.RECORDSOURCE = "VendSearch";
            }
            //Refreshes the Search address data
            //THISFORM.pfrStuff.Page1.cnrSearchAddr.REFRESH;

            if (!(String.IsNullOrWhiteSpace(SearchVendCode) || String.IsNullOrWhiteSpace(SearchAddr_Cd)))
            {
                //Enables Accept button
                //THISFORM.cmdAccept1.ENABLED = true;
            }
            else
            {
                //Disable Accept button
                //THISFORM.cmdAccept1.ENABLED = false;
            }

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }

        /*
        public ActionResult NextDay(string ex_id, string prepcode, string po_id, string err_code)
        {
            NextDayButton button = new NextDayButton();
            button.FinishCode(ex_id, prepcode, po_id, err_code);

            return Json("Success", JsonRequestBehavior.AllowGet);
        }
        */


        public ActionResult NextDay(string ex_id, string prepcode, string po_id, string err_code)
        {
            var helper = new NextDayButton();

            var exception = TempData.Peek("exception") as EXCEPTION;
            helper.Initialize(exception, prepcode);

            Session[SessionKey.Helper] = helper;

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                ControllerAction.Exception.NextDay, ControllerAction.Exception.NextDayNotes, "Exceptions", dailyInterestAmount);

            return View("other/notes", vmNotes);

        }

        [ValidateInput(false)]
        public ActionResult NextDayNotes(NotesViewModel vmNotes)
        {
            //If Finish button was not pushed, exit now
            if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

            var helper = Session[SessionKey.Helper] as NextDayButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            Session[SessionKey.VmNotes] = vmNotes;

            helper.SetNotes(vmNotes);
            helper.FinishCode();

            var request = Session[SessionKey.MainRequest] as DataSourceRequest;
            var search = Session[SessionKey.MainSearch] as ExceptionListModel;
            return RedirectToAction("Index", new { request, search });
        }


        public ActionResult MessageYesNoFromTranshist(MessageDisplay vmConfirm)
        {
            return View("ConfirmYesNo", vmConfirm);
        }

        public ActionResult MessageOkFromTranshist(MessageDisplay vmConfirm)
        {
            return View("ConfirmYesNo", vmConfirm);
        }

        public ActionResult MandMemoFromTranshist(MandMemoViewModel vmMandMemo)
        {
            return View("MandMemo", vmMandMemo);
        }

        #endregion Buttons

        #region InvEdit

        public ActionResult InvEdit()
        {
            return GetInvEditView();
        }

        [ActionName("InvEdit"), SubmitButton(Name = "btnNext")]
        public ActionResult NextInvEdit(InvEditViewModel vm)
        {
            try
            {
                if (Session[SessionKey.U049Review] == null || (bool)(Session[SessionKey.U049Review]) == false)
                {
                    var efException = TempData["exception"] as EXCEPTION;
                    InvEditBtnNextUpdates(vm, efException);

                    return CancelInvEdit(vm.ExceptionCode, vm.ExceptionId);

                }
                else
                {
                    var efException = TempData["exception"] as EXCEPTION;

                    Session[SessionKey.InvEditViewModel] = vm;
                    Session[SessionKey.Exception] = efException;

                    string _sNotesType = "";
                    if (efException.ERR_CODE == "U044")
                    {
                        var changes = GetInvoiceChanges(vm, false);
                        if (changes.ContainsKey("INVOICE"))
                        {
                            _sNotesType = "U049CRCT";
                        }
                        else
                        {
                            _sNotesType = "U044CRCT";
                        }
                    }
                    else
                    {
                        _sNotesType = "CORRECT";
                    }

                    return GetNotesView(efException.ERR_CODE, _sNotesType, efException.ACT, efException.EX_ID, efException.PDOCNO, ControllerAction.Exception.FinishU049Notes, "Exceptions");
                }
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "NextInvEdit");
            }
        }

        private void InvEditBtnNextUpdates(InvEditViewModel vm, EXCEPTION efException, bool U049 = false)
        {
            UpdateInvoice(vm, true);

            // Invoices on U049 exceptions don't go to KEYED status.
            if (!U049)
            {
                var mgrInvoice = new PegasysInvoiceManager();
                var efInvoice = mgrInvoice.GetPegasysInvoiceByKeyId(vm.InvoiceKeyId);
                efInvoice.INV_STATUS = "KEYED";
                efInvoice.PREVALIDATION_FL = "F";
                efInvoice.ERR_CODE = null;
                mgrInvoice.UpdatePegasysInvoice(efInvoice);
            }

            var mgrException = new ExceptionsManager();

            efException.ERR_RESPONSE = "C";
            efException.CLEARED_DATE = DateTime.Now;
            mgrException.UpdateException(efException);
        }


        [ActionName("InvEdit"), SubmitButton(Name = "btnAccept")]
        public ActionResult AcceptInvEdit(InvEditViewModel vm)
        {
            try
            {
                UpdateInvoice(vm, false);
                var efException = TempData["exception"] as EXCEPTION;
                UpdateExceptionOnAccept(efException);
                if (!string.IsNullOrWhiteSpace(efException.NOT_KEY_ID))
                    UpdateNotificationOnAccept(efException.NOT_KEY_ID);

                return CancelInvEdit(vm.ExceptionCode, vm.ExceptionId);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "AcceptInvEdit");
            }
        }

        [ActionName("InvEdit"), SubmitButton(Name = "btnBackToU044")]
        public ActionResult BackInvEdit(InvEditViewModel vm)
        {
            Session[SessionKey.InvEditViewModel] = vm;

            return RedirectToAction(ControllerAction.Exception.BackToU044, "Exceptions", new { exceptionCode = vm.ExceptionCode });
        }

        private void UpdateExceptionOnAccept(EXCEPTION efException)
        {
            efException.PREPCODE = this.PrepCode;
            efException.CLEARED_DATE = DateTime.Now;
            efException.ERR_RESPONSE = "A";
            efException.OUT = "F";
            efException.RESPONSENOTES += "Add Invoice Address Accepted\n";

            var mgr = new ExceptionsManager();
            mgr.UpdateException(efException);
        }

        private void UpdateNotificationOnAccept(string id)
        {
            var mgr = new NotificationsManager();
            var note = mgr.GetNotificationById(id);
            // Need some logging here...
            if (note == null)
                return;
            note.STATUS = "Pending";
            mgr.UpdateNotification(note);
        }

        private void UpdateInvoice(InvEditViewModel vmNewInvoice, bool updateException)
        {
            var pegInvoice = GetPegasysInvoiceByExId(vmNewInvoice.ExceptionId);
            var changes = GetInvoiceChanges(vmNewInvoice, true);
            if (changes.Count == 0)
                return;
            ProcessInvoiceChanges(pegInvoice, changes, updateException);
        }

        private void ProcessInvoiceChanges(PEGASYSINVOICE efInvoice, Dictionary<string, object> changes, bool updateException)
        {
            if (changes.ContainsKey("ACT"))
                ProcessChangedAct(efInvoice, changes, updateException);

            var mgrTH = new TransHistManager();
            // Use reflection to get invoice field (property) info.
            var properties = typeof(PEGASYSINVOICE).GetProperties();

            foreach (var change in changes)
            {
                var key = change.Key;
                var val = change.Value;
                var property = properties.FirstOrDefault(p => p.Name == key);

                // Create the transhist for each change
                var memo = string.Format("{0}: {1} changed to {2}", key, property.GetValue(efInvoice), val);
                var th = CreateTransHistForChange(efInvoice, memo, property.Name);
                mgrTH.InsertTransHist(th);

                // Now set the value for the invoice field that changed.
                property.SetValue(efInvoice, val);
            }

            // Update the invoice
            var mgrPegInv = new PegasysInvoiceManager();
            mgrPegInv.UpdatePegasysInvoice(efInvoice);
        }

        private void ProcessChangedAct(PEGASYSINVOICE invOld, Dictionary<string, object> changes, bool updateException)
        {
            var mgr = new TransHistManager();
            var newAct = changes["ACT"] as string;

            // Create transhist for old ACT
            var memo = string.Format("ACT: {0} changed to {1}", invOld.ACT, newAct);
            var thOld = CreateTransHistForChange(invOld, memo, "ACT");
            mgr.InsertTransHist(thOld);

            // Update exception ResponseNotes with memo for all Inv_Key_Id where err_respone = 'M'
            if (updateException)
            {
                var mgrExceptions = new ExceptionsManager();
                mgrExceptions.UpdateExceptionForInvEditNext(invOld.INV_KEY_ID, PrepCode, memo);
            }

            // Create transhist for new ACT
            memo = string.Format("ACT: {0} changed from {1}", newAct, invOld.ACT);
            var thNew = CreateTransHistForChange(invOld, memo, "ACT");
            mgr.InsertTransHist(thNew);

            // Update the ACT field in the EF model.
            invOld.ACT = newAct;

            // RemoveACT from changes since it's already been processed.
            changes.Remove("ACT");
        }

        private TRANSHIST CreateTransHistForChange(PEGASYSINVOICE inv, string memo, string propertyName)
        {
            var th = new TRANSHIST()
            {
                // Changed the trigger to so that when TH_ID = "THID" then construct a new id.
                //TH_ID = "THID",
                TH_ID = null,
                ACT = inv.ACT,
                PDOCNO = inv.PDOCNOPO,
                INV_KEY_ID = inv.INV_KEY_ID,
                ERR_CODE = "D/E",
                TRANSDATE = DateTime.Now,
                PREPCODE = this.PrepCode,
                CUFF_MEMO = memo,
                ALLPROCESS = propertyName,
                CLEARED_DATE = DateTime.Now,
            };
            return th;
        }

        private Dictionary<string, object> GetInvoiceChanges(InvEditViewModel vmNewInvoice, bool updateTempData)
        {
            var efInv = new PegasysInvoice();
            var changes = new Dictionary<string, object>();
            var oldInvoice = TempData["vmInvoice"] as InvEditViewModel;
            TempData["vmInvoice"] = (updateTempData ? vmNewInvoice : oldInvoice);

            if (oldInvoice.InvoiceNumber != vmNewInvoice.InvoiceNumber)
                changes.Add("INVOICE", vmNewInvoice.InvoiceNumber);
            if (oldInvoice.InvoiceDate != vmNewInvoice.InvoiceDate)
                changes.Add("INVDATE", vmNewInvoice.InvoiceDate.Value);
            if (oldInvoice.InvoiceReceivedDate != vmNewInvoice.InvoiceReceivedDate)
                changes.Add("INVRECDATE", vmNewInvoice.InvoiceReceivedDate.Value);
            if (oldInvoice.PurchaseOrderNumber != vmNewInvoice.PurchaseOrderNumber)
                changes.Add("PONUMBER", vmNewInvoice.PurchaseOrderNumber);
            if (oldInvoice.ContractNumber != vmNewInvoice.ContractNumber)
                changes.Add("CONTRACT", vmNewInvoice.ContractNumber);
            if (oldInvoice.AccountNumber != vmNewInvoice.AccountNumber)
                changes.Add("ACCOUNTNO", vmNewInvoice.AccountNumber);
            if (oldInvoice.VendorName != vmNewInvoice.VendorName)
                changes.Add("VENDNAME", vmNewInvoice.VendorName);
            if (oldInvoice.VendorAddr1 != vmNewInvoice.VendorAddr1)
                changes.Add("ADDR_L1", vmNewInvoice.VendorAddr1);
            if (oldInvoice.VendorAddr2 != vmNewInvoice.VendorAddr2)
                changes.Add("ADDR_L2", vmNewInvoice.VendorAddr2);
            if (oldInvoice.VendorAddr3 != vmNewInvoice.VendorAddr3)
                changes.Add("ADDR_L3", vmNewInvoice.VendorAddr3);
            if (oldInvoice.RemitCity != vmNewInvoice.RemitCity)
                changes.Add("ADDR_CITY", vmNewInvoice.RemitCity);
            if (oldInvoice.RemitState != vmNewInvoice.RemitState)
                changes.Add("ADDR_STATE", vmNewInvoice.RemitState);
            if (oldInvoice.RemitZip != vmNewInvoice.RemitZip)
                changes.Add("ADDR_ZPCD", vmNewInvoice.RemitZip);
            if (oldInvoice.DiscountDays != vmNewInvoice.DiscountDays)
                changes.Add("DISCDAYS", vmNewInvoice.DiscountDays);
            if (oldInvoice.DiscountPercent != vmNewInvoice.DiscountPercent)
                changes.Add("DISCPERCENT", vmNewInvoice.DiscountPercent);
            if (oldInvoice.NetDays != vmNewInvoice.NetDays)
                changes.Add("NETDAYS", vmNewInvoice.NetDays);
            if (oldInvoice.TaxAmount != vmNewInvoice.TaxAmount)
                changes.Add("TAXAMOUNT", vmNewInvoice.TaxAmount);
            if (oldInvoice.ShippingCharges != vmNewInvoice.ShippingCharges)
                changes.Add("SHIPAMOUNT", vmNewInvoice.ShippingCharges);
            if (oldInvoice.MiscCharge != vmNewInvoice.MiscCharge)
                changes.Add("MISC_CHARGES", vmNewInvoice.MiscCharge);
            if (oldInvoice.InvoiceAmount != vmNewInvoice.InvoiceAmount)
                changes.Add("AMOUNT", vmNewInvoice.InvoiceAmount);
            if (oldInvoice.Act != vmNewInvoice.Act)
                changes.Add("ACT", vmNewInvoice.Act);

            return changes;
        }

        public ActionResult SkipInvEdit(string exceptionId)
        {
            try
            {
                var helper = new ExceptionHelper();
                var exception = TempData["exception"] as EXCEPTION;
                var roles = Session[SessionKey.RoleModel] as RoleListModel;

                TempData["exception"] = helper.Skip(exception, roles.PREPCODE);

                return CancelInvEdit(exception.ERR_CODE, exceptionId);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "SkipInvEdit");
            }
        }

        public ActionResult BackToU044(string exceptionCode)
        {
            try
            {
                NotesViewModel vmNotes = null;

                // Ultimately, go back to starting point
                var vmInvoice = TempData.Peek("vmInvoice") as InvEditViewModel;

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                if (vmInvoice != null)
                {
                    vmNotes = new NotesViewModel(null, exceptionCode, vmInvoice.Act, vmInvoice.ExceptionId, vmInvoice.PegasysDocumentNumber, "RESET",
                        ControllerAction.Exception.BackToU044Notes, "Exceptions", dailyInterestAmount);
                }
                else
                {
                    vmNotes = new NotesViewModel(null, exceptionCode, "", "", "", "RESET",
                        ControllerAction.Exception.BackToU044Notes, "Exceptions", dailyInterestAmount);
                }


                return View("Other/Notes", vmNotes);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "BackToU044");
            }
        }

        public ActionResult CancelInvEdit(string exceptionCode, string exceptionId)
        {
            try
            {
                new ExceptionsManager().CheckinException(exceptionId);

                TempData.Remove("vmInvoice");
                TempData.Remove("vmInvoiceUpdated");

                return RedirectToAction("Index");
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "CancelInvEdit");
            }
        }

        // Called within the controller from ReturnViewResult for U054 & V055.
        // Called from InvEdit for U043 & U049 when the Review button is clicked
        private ViewResult GetInvEditView()
        {
            try
            {
                var invoice = GetInvEditViewModel();
                TempData["vmInvoice"] = invoice;
                return View("Other/InvEdit", invoice);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "GetInvEditView");
            }
        }

        private InvEditViewModel GetInvEditViewModel()
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var pegInvoice = GetPegasysInvoiceByExId(exception.EX_ID);
            var invEdit = InvEditViewModel.MapToViewModel(pegInvoice);
            invEdit.ExceptionId = exception.EX_ID;
            invEdit.ExceptionCode = exception.ERR_CODE;
            return invEdit;
        }

        private void UpdateU049ForU044()
        {
            var exception = TempData["exception"] as EXCEPTION;
            var fieldsToUpdate = new List<string>
                {
                    "ERR_RESPONSE",
                    "ALLPROCESS",
                    //"PREPCODE",
                    "CLEARED_DATE"
                };
            exception.ERR_RESPONSE = "C";
            exception.ALLPROCESS = "Back to U044 per Manager.";
            //exception.PREPCODE = PrepCode;
            exception.CLEARED_DATE = DateTime.Now;

            // Restore updated exception in TempData
            TempData["exception"] = exception;

            var mgr = new ExceptionsManager();
            mgr.UpdateException(exception, fieldsToUpdate);
        }

        private void CreateU044()
        {
            var exU049 = TempData.Peek("exception") as EXCEPTION;
            var exU044 = new EXCEPTION()
            {
                EX_ID = "EX", // Doesn't matter as long as it's not null; trigger will set this.
                ACT = exU049.ACT,
                PDOCNO = exU049.PDOCNO,
                INV_KEY_ID = exU049.INV_KEY_ID,
                ERR_CODE = "U044",
                UPDSTATUS = "T",
                PEGSYSTEM = exU049.PEGSYSTEM,
                EX_MEMO = "Re - establish U044 per Manager.",

                // Stuff that always seems to be populated for a U044, but wasn't specified.
                EX_DATE = DateTime.Now,
                PO_ID = exU049.PO_ID,
                BA = exU049.BA,
                EX_FUND = exU049.EX_FUND,
                OUT = exU049.OUT,
                ORGCODE = exU049.ORGCODE,
                PODOCTYPE = exU049.PODOCTYPE,
                VENDNAME = exU049.VENDNAME,
            };
            var mgr = new ExceptionsManager();
            mgr.InsertException(exU044);
        }

        public ViewResult GetUserExceptionView(bool clearException, string ExId, NotesResult Result)
        {
            var mgr = new ExceptionsManager();
            var exception = TempData["exception"] as EXCEPTION;
            if (exception == null || exception.EX_ID != ExId)
            {
                exception = GetExceptionByExId(ExId);
            }
            // Preserve for subsequent postbacks
            TempData["exception"] = exception;

            var vmException = new UserExceptionViewModel()
            {
                r_pegasys = true,
                Act = exception.ACT,
                ErrorCode = exception.ERR_CODE,
                ExId = exception.EX_ID,
                Memo = exception.EX_MEMO,
                Memo2 = exception.EX_MEMO2,
                PoId = exception.PO_ID,
                RRId = exception.RR_ID,
                InvKeyId = exception.INV_KEY_ID,
                AeId = exception.AE_ID,
                PDocNo = exception.PDOCNO,
                NoteResult = Result
            };

            if (exception.RR_ID != null)
            {
                vmException.Id = exception.RR_ID;
            }
            else if (exception.PO_ID != null)
            {
                vmException.Id = exception.PO_ID;
            }
            else if (exception.INV_KEY_ID != null)
            {
                vmException.Id = exception.INV_KEY_ID;
            }

            var ActGridData = mgr.UserGridData(vmException.Act, vmException.PDocNo);
            vmException.E052Recs = ActGridData;

            #region PO Data
            if (vmException.PoId != null)
            {
                var PegPoFrmData = mgr.PegPoFrmData(vmException.PoId);
                //if (PegPoFrmData == null) {
                //    PegPoData = mgr.PegPoData(vmException.PoId);
                //}
                if (PegPoFrmData != null)
                {
                    vmException.PoExists = true;
                    vmException.RRExists = false;
                    vmException.InvExists = false;

                    if (vmException.PoId.Contains("&"))
                    {
                        vmException.ModNumStr = vmException.PoId.Right(4);
                    }
                    else
                    {
                        vmException.ModNumStr = "";
                    }

                    if (vmException.r_pegasys)
                    {
                        if (PegPoFrmData.EDI_IND == "T")
                        {
                            vmException.PoImageExists = false;
                        }
                        else
                        {
                            vmException.PoImageExists = true;
                        }
                    }
                    else
                    {
                        if (PegPoFrmData.EDI_IND != null)
                        {
                            vmException.PoImageExists = false;
                        }
                        else
                        {
                            vmException.PoImageExists = true;
                        }
                    }
                    vmException.EdiInd = PegPoFrmData.EDI_IND;
                    vmException.PrepCode = PegPoFrmData.PREPCODE;
                    vmException.PoImgageId = PegPoFrmData.IMAGEID;
                }
                else
                {
                    vmException.PoImageBtnTip = "No record in PO.";
                }
            }
            else
            {
                vmException.PoImageBtnTip = "No PO.";
            }
            #endregion

            #region RR Data
            if (vmException.RRId != null)
            {
                var PegRRFrmData = mgr.PegRRFrmData(vmException.RRId);

                if (PegRRFrmData != null)
                {
                    vmException.RRExists = true;
                    vmException.PoExists = false;
                    vmException.InvExists = false;

                    vmException.PDocNo = PegRRFrmData.PDOCNOPO;

                    if (PegRRFrmData.EDI_IND == "T")
                    {
                        vmException.RRImageExists = false;
                    }
                    else if (PegRRFrmData.IMAGEID != null)
                    {
                        vmException.RRImageExists = true;
                        vmException.RRImageId = PegRRFrmData.IMAGEID;
                    }
                    vmException.RRImageBtnTip = "Pagasys RR.";
                }
                else
                {
                    vmException.RRImageBtnTip = "No record in RR.";
                }
            }
            else
            {
                vmException.RRImageBtnTip = "No RR.";
            }
            #endregion

            #region Invoice Data
            if (vmException.InvKeyId != null)
            {
                var PegInvData = mgr.PegInvData(vmException.InvKeyId);

                if (PegInvData != null)
                {
                    if (vmException.ErrorCode == "U044" && vmException.Memo2 != null)
                    {
                        var memo2String = "\r\n {0} Invoice: {1} Amount: {2}";
                        vmException.Memo2 = string.Format(memo2String, vmException.Memo2, PegInvData.INVOICE, PegInvData.AMOUNT);
                    }

                    vmException.InvExists = true;
                    vmException.PoExists = false;
                    vmException.RRExists = false;

                    vmException.PDocNo = PegInvData.PDOCNOPO;
                    vmException.VendCd = PegInvData.VEND_CD;
                    if (PegInvData.EDI_IND == "T")
                    {
                        vmException.InvImageExists = false;
                    }
                    else if (PegInvData.IMAGEID != null)
                    {
                        vmException.InvImageExists = true;
                        vmException.InvImageId = PegInvData.IMAGEID;
                    }
                }
                else
                {
                    vmException.InvImageBtnTip = "No record in Inv.";
                }
            }
            else
            {
                vmException.InvImageBtnTip = "No Inv.";
            }
            #endregion
            if (!(vmException.PoExists && vmException.PoExists && vmException.InvExists))
            {
                vmException.CorrectDe1Tip = "No data to change";
            }
            else { vmException.CorrectDe1Tip = ""; }

            Session[SessionKey.U049Review] = false;

            return View("UserException/Exception", vmException);
        }


        private EXCEPTION GetExceptionByExId(string exId)
        {
            var mgr = new ExceptionsManager();
            var exception = mgr.GetExceptionByExId(exId);

            return exception;
        }

        private TRANSHIST GetTransHistByInvKeyId(string InvKeyId)
        {
            var mgr = new ExceptionsManager();
            var transhist = mgr.GetTransHistByInvKeyId(InvKeyId);

            return transhist;
        }

        private PEGASYSINVOICE GetPegasysInvoiceByExId(string exId)
        {
            var mgr = new PegasysInvoiceManager();
            var invoice = mgr.GetPegasysInvoiceByExId(exId);

            return invoice;
        }

        #endregion InvEdit      

        #region RREdit
        public ActionResult RREdit()
        {
            return GetRREditView();
        }

        [ActionName("RREdit"), SubmitButton(Name = "btnNext")]
        public ActionResult NextRREdit(RREditModel rm)
        {
            try
            {

                var efException = TempData["exception"] as EXCEPTION;

                Session[SessionKey.RREditModel] = rm;
                Session[SessionKey.Exception] = efException;

                string _sNotesType = "CORRECT";

                return GetNotesView(efException.ERR_CODE, _sNotesType, efException.ACT, efException.EX_ID, efException.PDOCNO, ControllerAction.Exception.FinishU044RREditNotes, "Exceptions");

            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "NextRREdit");
            }
        }

        public ActionResult CancelRREdit(string exceptionId)
        {
            try
            {
                new ExceptionsManager().CheckinException(exceptionId);

                TempData.Remove("rrEdit");

                return RedirectToAction("Index");
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "CancelRREdit");
            }
        }

        private ViewResult GetRREditView()
        {
            try
            {
                PegasysRREditManager mgr = new PegasysRREditManager();
                var exception = TempData.Peek("exception") as EXCEPTION;

                var rrEdit = mgr.GetRREditModel(exception.ERR_CODE, exception.EX_ID);
                TempData["rrEdit"] = rrEdit;
                return View("Other/RREdit", rrEdit);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "GetRREditView");
            }
        }

        public void UpdateRREdit(RREditModel model, EXCEPTION efException)
        {
            PegasysRREditManager mgr = new PegasysRREditManager();
            //var exception = TempData.Peek("exception") as EXCEPTION;

            var prepcode = Session[SessionKey.PrepCode].ToString();
            mgr.UpdateRR(prepcode, efException.ERR_CODE, efException.RR_ID, model);
        }
        #endregion

        #region Notes

        public ActionResult Notes(string origin)
        {
            var vmInvoice = TempData.Peek("vmInvoice") as InvEditViewModel;

            if (vmInvoice == null)
            {
                vmInvoice = new InvEditViewModel();
            }

            var vm = new NotesViewModel()
            {
                Origin = origin,
                Amount = !vmInvoice.InvoiceAmount.HasValue ? string.Empty
                    : vmInvoice.InvoiceAmount.Value.ToString(),
                ExceptionCode = vmInvoice.ExceptionCode,
            };
            return View("Other/Notes", vm);
        }

        public ActionResult FinishNotesVM(NotesViewModel vm)
        {
            if (vm.Origin == ControllerAction.Exception.BackToU044)
            {
                UpdateU049ForU044();
                CreateU044();
                return CancelInvEdit(vm.ExceptionCode, vm.ExId);
            }
            TempData.Clear();

            NotesResult ReturnObj = new NotesResult();
            ReturnObj.ErrCode = vm.ErrCode;
            ReturnObj.ExId = vm.ReturnExId;
            ReturnObj.Act = vm.Act;
            ReturnObj.PDocNo = vm.PDocNo;
            ReturnObj.ReturnVal1 = vm.returnVal1;
            ReturnObj.ReturnVal2 = vm.returnVal2;
            ReturnObj.ReturnVal3 = vm.returnVal3;
            ReturnObj.ReturnVal4 = vm.returnVal4;
            ReturnObj.ReturnVal5 = vm.returnVal5;
            ReturnObj.ReturnVal6 = vm.returnVal6;
            ReturnObj.ReturnVal7 = vm.returnVal7;
            ReturnObj.ReturnVal8 = vm.returnVal8;
            ReturnObj.ReturnVal9 = vm.returnVal9;
            ReturnObj.ReturnVal10 = vm.returnVal10;
            ReturnObj.ReturnValX = vm.returnValX;
            ReturnObj.ReturnValY = vm.returnValY;
            ReturnObj.ReturnValZ = vm.returnValZ;

            return RedirectToAction(vm.ReturnAction, vm.ReturnController, ReturnObj);
            //return RedirectToAction("Index");
        }

        [ValidateInput(false)]
        public ActionResult FinishNotes(string err_Code, string returnExId, string Act, string pDocNo, string returnVal1,
            string returnVal2, string returnVal3, string returnVal4, string returnVal5, string returnVal6, string returnVal7,
            string returnVal8, string returnVal9, string returnVal10, string returnValX, string returnValY, string returnValZ,
            string ReturnAction, string ReturnController)
        {
            var vm = new NotesViewModel()
            {
                ErrCode = err_Code,
                ExId = returnExId,
                Act = Act,
                PDocNo = pDocNo,
                returnVal1 = returnVal1,
                returnVal2 = returnVal2,
                returnVal3 = returnVal3,
                returnVal4 = returnVal4,
                returnVal5 = returnVal5,
                returnVal6 = returnVal6,
                returnVal7 = returnVal7,
                returnVal8 = returnVal8,
                returnVal9 = returnVal9,
                returnVal10 = returnVal10,
                returnValX = int.Parse(returnValX),
                returnValY = int.Parse(returnValY),
                returnValZ = returnValZ,
                ReturnAction = ReturnAction,
                ReturnController = ReturnController,
            };

            if (String.IsNullOrWhiteSpace(vm.ReturnController))
                vm.ReturnController = "Exceptions";

            if (returnVal1 == "CANCEL")
            {
                return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = err_Code, exId = returnExId.Replace("&amp;", "&") });
            }
            return RedirectToAction(vm.ReturnAction==ControllerAction.Exception.EARequest? ControllerAction.Exception.EARequestNotes:vm.ReturnAction, vm.ReturnController, vm);
        }

        [ValidateInput(false)]
        public ActionResult BackToU044Notes(NotesViewModel vm)
        {
            var exception = new ExceptionsManager().GetExceptionByExId(vm.ExId);
            var buttonCode = new CorrectDEButton();
            buttonCode.Initialize(exception, vm.PDocNo, this.PrepCode);
            buttonCode.SetNotes(vm);
            buttonCode.ExceptionU049("RESET");

            var invEditViewModel = Session[SessionKey.InvEditViewModel] as InvEditViewModel;
            UpdateInvoice(invEditViewModel, true);


            return RedirectToAction(ControllerAction.Exception.Index, "Exceptions");
        }

        [ValidateInput(false)]
        public ActionResult FinishU049Notes(NotesViewModel vm)
        {
            var invEditViewModel = Session[SessionKey.InvEditViewModel] as InvEditViewModel;
            var exception = Session[SessionKey.Exception] as EXCEPTION;

            bool POExists = GetPOExists(exception.PO_ID);
            bool InvExists = GetInvExists(exception.INV_KEY_ID);
            bool RRExists = GetRRExists(exception.RR_ID);

            var buttonCode = new CorrectDEButton();
            buttonCode.Initialize(exception, vm.PDocNo, this.PrepCode);
            buttonCode.SetNotes(vm);

            string _sNotesType = "";
            if (exception.ERR_CODE == "U044")
            {
                var changes = GetInvoiceChanges(invEditViewModel, false);
                if (changes.ContainsKey("INVOICE"))
                {
                    _sNotesType = "U049CRCT";
                }
                else
                {
                    _sNotesType = "U044CRCT";
                }
            }
            else
            {
                _sNotesType = "CORRECT";
            }

            buttonCode.FinishCode(null, null, null, _sNotesType, POExists, InvExists, RRExists);

            InvEditBtnNextUpdates(invEditViewModel, exception, (_sNotesType == "U049CRCT" ? true : false));


            return RedirectToAction(ControllerAction.Exception.Index, "Exceptions");
        }

        [ValidateInput(false)]
        public ActionResult FinishU044RREditNotes(NotesViewModel vm)
        {
            var RREditModel = Session[SessionKey.RREditModel] as RREditModel;
            var exception = Session[SessionKey.Exception] as EXCEPTION;

            bool POExists = GetPOExists(exception.PO_ID);
            bool InvExists = GetInvExists(exception.INV_KEY_ID);
            bool RRExists = GetRRExists(exception.RR_ID);

            var buttonCode = new CorrectDEButton();
            buttonCode.Initialize(exception, vm.PDocNo, this.PrepCode);
            buttonCode.SetNotes(vm);


            string _sNotesType = "CORRECT";


            buttonCode.FinishCode(null, null, null, _sNotesType, POExists, InvExists, RRExists);

            UpdateRREdit(RREditModel, exception);


            return RedirectToAction(ControllerAction.Exception.Index, "Exceptions");
        }

        private bool GetRRExists(string RR_ID)
        {
            bool RrExists = false;

            if (RR_ID != null)
            {
                var exceptionsManager = new ExceptionsManager();
                var RrFrmData = exceptionsManager.PegRRFrmData(RR_ID);
                if (RrFrmData == null)
                {
                    var RrData = exceptionsManager.PegRRData(RR_ID);
                    if (RrData != null)
                    {
                        RrExists = true;
                    }
                }
                else
                {
                    RrExists = true;
                }
            }

            return RrExists;
        }

        private bool GetInvExists(string INV_KEY_ID)
        {
            bool invExists = false;

            if (!String.IsNullOrWhiteSpace(INV_KEY_ID))
            {
                var mgr = new ExceptionsManager();
                var PegInvData = mgr.PegInvData(INV_KEY_ID);

                if (PegInvData != null)
                {
                    invExists = true;
                }
            }

            return invExists;
        }

        private bool GetPOExists(string PO_ID)
        {
            bool PoExists = false;

            if (PO_ID != null)
            {
                var exceptionsManager = new ExceptionsManager();
                var PegPoFrmData = exceptionsManager.PegPoFrmData(PO_ID);
                if (PegPoFrmData == null)
                {
                    var PegPoData = exceptionsManager.PegPoData(PO_ID);
                    if (PegPoData == null)
                    {
                        //No po
                    }
                    else
                    {
                        PoExists = true;
                    }
                }
                else
                {
                    PoExists = true;
                }
            }

            return PoExists;
        }

        public ActionResult FinishMandMemo(string noteValue, string returnAction, string returnController, string optionalParam)
        {
            var vm = new MandMemoViewModel();
            vm.NoteValue = noteValue;
            vm.ReturnAction = returnAction;
            vm.ReturnController = returnController;
            vm.OptionalParam = optionalParam;
            return RedirectToAction(vm.ReturnAction, vm.ReturnController, vm);
        }

        public ActionResult Message(MessageDisplay vmMessage)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (vmMessage.ReturnAction == ControllerAction.Exception.DisplayException && vmMessage.ErrCode == "P200")
            {
                return RedirectToAction(ControllerAction.Exception.Index);
            }
            else if (vmMessage.ReturnAction == ControllerAction.Exception.DisplayException)
            {
                return RedirectToAction(vmMessage.ReturnAction, vmMessage.ReturnController, new { exCode = vmMessage.ErrCode, exId = vmMessage.ExId.Replace("&amp;", "&") });
            }
            else if (vmMessage.Response == false)
            {
                if (("M224/M303/M237").Split('/').Contains(vmMessage.ErrCode))
                {
                    if(exception!=null && exception.OUT!=null && exception.OUT=="T")
                    {
                        return RedirectToAction(ControllerAction.Exception.DisplayExceptionWithoutCheckoutMessage, "Exceptions", new { exCode = vmMessage.ErrCode, exId = vmMessage.ExId.Replace("&amp;", "&") });
                    }
                    else
                    return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = vmMessage.ErrCode, exId = vmMessage.ExId.Replace("&amp;", "&") });
                }
                else
                    return RedirectToAction(vmMessage.ReturnAction, vmMessage.ReturnController, new { exCode = vmMessage.ErrCode, exId = vmMessage.ExId.Replace("&amp;", "&") });
            }

            return RedirectToAction(vmMessage.ReturnAction, vmMessage.ReturnController, vmMessage);

        }

        [ValidateInput(false)]
        public ActionResult CancelNotes(NotesViewModel vm)
        {
            NotesResult ReturnObj = new NotesResult();
            ReturnObj.ErrCode = vm.ErrCode;
            ReturnObj.ExId = vm.ReturnExId;
            ReturnObj.Act = vm.Act;
            ReturnObj.PDocNo = vm.PDocNo;
            ReturnObj.ReturnVal1 = "BACK";
            ReturnObj.ReturnVal7 = "";

            return RedirectToAction(vm.ReturnAction, vm.ReturnController, ReturnObj);
        }

        [HttpPost]
        public ActionResult GetNoteContactData(string PoId, string OrgCode, string Ba)
        {
            var mgr = new ExceptionsManager();
            CONTACT ReturnData = new CONTACT();

            if (!String.IsNullOrWhiteSpace(OrgCode) && !String.IsNullOrWhiteSpace(Ba))
            {
                ReturnData = mgr.getNotesContactData(PoId, OrgCode, Ba);
            }

            return Json(new { success = true, ContactName = ReturnData.CONTACTNAME, ContactPhone = ReturnData.PHONE }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetNotesView(string ErrCode, string ButtonType, string Act, string ExId, string PDocNo, string ReturnAction, string ReturnController)
        {
            var exception = GetExceptionByExId(ExId);

            String dailyInterestAmount = "";
            if (Session[SessionKey.DailyInterestAmount] != null)
            {
                dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
            }

            var NotesModel = new NotesViewModel(exception, ErrCode, Act, ExId, PDocNo, ButtonType, ReturnAction, ReturnController, dailyInterestAmount);

            return View("Other/Notes", NotesModel);
        }


        public ActionResult GetPicklistView(List<PEGASYSINVOICE> rtnInv, string Act, string Pdocnopo, string VendCd)
        {
            var mgr = new ExceptionsManager();
            var model = mgr.GetPicklistData(Pdocnopo, VendCd);
            return View("UserException/Picklist", model);
        }

        [HttpPost]
        public ActionResult Picklist_Read([DataSourceRequest]DataSourceRequest request, PickListViewModel p)
        {
            var mgr = new ExceptionsManager();

            var data = mgr.GetPicklistData(p.PDocNo, p.Vend_Cd);
            var result = new DataSourceResult()
            {
                Data = data
            };

            return Json(result);
        }

        public ActionResult GetFaxNotesView(string exId)
        {
            var FaxNotesModel = new FaxNotesViewModel();
            var exception = GetExceptionByExId(exId);

            if (exception.FAXNOTES != null)
            {
                FaxNotesModel.FaxNotes = exception.FAXNOTES;
            }

            return View("Other/FaxNotes", FaxNotesModel);
        }
        #endregion Notes

        #region Image Management

        public ActionResult InvoiceTiffViewer(string Type, string TypeId)
        {
            var invoice = TempData.Peek("vmInvoice") as InvEditViewModel;
            return TiffViewResult(Type, TypeId, invoice.ImagePath);
        }

        public ActionResult UserTiffViewer(string Type, string TypeId)
        {
            string FilePath = GetTiffFilePath(Type, TypeId);
            FilePath = CleanImageFilePath(FilePath);
            return TiffViewResult(Type, TypeId, FilePath);
        }

        public ActionResult OtherTiffViewer(string Type, string TypeId)
        {
            var path = CleanImageFilePath(TypeId);
            return TiffViewResult(Type, path, path);
        }

        public ActionResult TiffViewResult(string Type, string TypeId, string FilePath)
        {
            var model = new UserExceptionViewModel();
            model.TiffType = Type;
            model.TiffTypeId = TypeId;
            model.TiffFilePath = FilePath;

            string extension = Path.GetExtension(FilePath);
            extension = string.IsNullOrWhiteSpace(extension) ? "" : extension.ToLower();

            if (extension == ".tiff" || extension == ".tif")
            {
                TIF TheFile = new TIF(FilePath);
                model.TotalTIFPgs = TheFile.PageCount;
                TheFile.Dispose();

                return View("UserException/TiffViewerModal", model);
            }
            else
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(FilePath);
                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = Path.GetFileName(FilePath),

                    // always prompt the user for downloading, set to true if you want 
                    // the browser to try to show the file inline
                    Inline = false,
                };
                Response.AppendHeader("Content-Disposition", cd.ToString());
                return File(fileBytes, MimeMapping.GetMimeMapping(FilePath));
            }
        }

        public string GetTiffFilePath(string Type, string TypeId)
        {
            string FilePath = "";
            var mgr = new ExceptionsManager();
            switch (Type)
            {
                case "po":
                    var PoData = mgr.PegPoFrmData(TypeId);
                    if (PoData != null && !string.IsNullOrWhiteSpace(PoData.IMAGEID))
                        FilePath = PoData.IMAGEID;
                    else
                    {
                        var po = mgr.PegPoData(TypeId);
                        if (po != null)
                        {
                            FilePath = po.IMAGEID;
                        }
                    }
                    break;
                case "rr":
                    var RrData = mgr.PegRRFrmData(TypeId);
                    if (RrData != null && !string.IsNullOrWhiteSpace(RrData.IMAGEID))
                        FilePath = RrData.IMAGEID;
                    else
                    {
                        var rr = mgr.PegRRData(TypeId);
                        if (rr != null)
                        {
                            FilePath = rr.IMAGEID;
                        }
                    }
                    break;
                case "inv":
                    var InvData = mgr.PegInvData(TypeId);
                    FilePath = InvData.IMAGEID;
                    break;
            }

            if (String.IsNullOrEmpty(FilePath))
            {
                FilePath = GetImagePath("dummy.tif");
            }

            return FilePath;
        }

        #region Test Image View?

        public FileResult ViewImage(string pDocNo)
        {
            try
            {
                //var fname = GetImagePath(pDocNo);
                var fname = GetImagePath("6inv1363in056-mk20111228021.tif");
                return File(fname, "image/tiff");
            }
            catch (System.Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return null;
            }
        }

        public FileResult ViewImagebyImageID(string docId)
        {
            try
            {
                //var fname = GetImagePath(pDocNo);
                string ImagePath = GSA.R7BD.Utility.Utilities.GetImageNowPath(docId);
                if (ImagePath == null || !System.IO.File.Exists(ImagePath))
                {
                    return File("~/Images/dummy.tif", "image/tiff");
                }
                else
                {
                    return File(ImagePath, "image/tiff");
                }
            }
            catch (System.Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return null;
            }
        }

        public void ViewTiffAtPath(string FilePath, string Pg)
        {
            string unsized = Request.QueryString["unsized"];
            System.Drawing.Image TheImg;
            if (String.IsNullOrEmpty(FilePath))
            {
                FilePath = GetImagePath("dummy.tif");
            }
            if (Request.QueryString["unsized"] == "1")
            {
                TheImg = new TIF(FilePath).GetTiffImageThumbUnsized(System.Convert.ToInt16(Pg));
            }
            else
            {
                TheImg = new TIF(FilePath).GetTiffImageThumb(System.Convert.ToInt16(Pg), System.Convert.ToInt16(Request.QueryString["Height"]), System.Convert.ToInt16(Request.QueryString["Width"]));
            }

            if (TheImg != null)
            {
                switch (Request.QueryString["Rotate"])
                {
                    case "90":
                        TheImg.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                        break;
                    case "180":
                        TheImg.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone);
                        break;
                    case "270":
                        TheImg.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
                        break;
                }

                Response.ContentType = "image/jpeg";
                TheImg.Save(Response.OutputStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                TheImg.Dispose();
            }
            else
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }

        public void ViewTiff(string FileName, string Pg)
        {
            // getFilePath
            if (!String.IsNullOrEmpty(FileName))
            {
                string FilePath = GetImagePath(FileName);
                ViewTiffAtPath(FilePath, Pg);
            }
        }

        public void ViewUserTiff(string Type, string TypeId, string Pg)
        {
            var mgr = new ExceptionsManager();
            var FilePath = GetTiffFilePath(Type, TypeId);

            FilePath = CleanImageFilePath(FilePath);
            ViewTiffAtPath(FilePath, Pg);
        }

        public String CleanImageFilePath(String imageID)
        {
            string imageIDrive = GSA.R7BD.Utility.Utilities.getDrivePathByLetter("IMAGEIDRIVEMAPPINGDIR:");
            string imageVDrive = GSA.R7BD.Utility.Utilities.getDrivePathByLetter("IMAGEVDRIVEMAPPINGDIR:");
            string fileName;

            if (imageID.ToLower().IndexOf("i:") >= 0)
            {
                fileName = imageID.ToLower().Replace("i:\\", @imageIDrive);
            }
            else if (imageID.ToLower().IndexOf("v:") >= 0)
            {
                fileName = imageID.ToLower().Replace("v:\\", @imageVDrive);
            }
            // Use StartsWith() instead of SubString(index, index)
            else if (imageID.ToLower().StartsWith(@"\\e07bds-san\"))
            {
                fileName = imageID.ToLower();
            }
            else if (imageID.ToLower().StartsWith(@"\\e07bds-san.r7bc.int\"))
            {
                fileName = imageID.ToLower().Replace(@"\\e07bds-san.r7bc.int\", @"\\e07bds-san\");
            }
            else
            {
                fileName = GSA.R7BD.Utility.Utilities.GetImageNowPath(imageID);
            }

            if (!System.IO.File.Exists(fileName))
            {
                fileName = GetImagePath("dummy.tif");
            }

            return fileName;
        }

        public ActionResult ViewDocument(string invKeyId)
        {
            try
            {
                return RedirectToAction("ViewInvoice", "TransHist", new { PDocNo = invKeyId });
            }
            catch (System.Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                return null;
            }
        }

        //private string GetImagePath(string pDocNo)
        private string GetImagePath(string fname)
        {
            // Need method to get image using something like the pegasys Document Number...
            //var mgrPegasys = new PegInvoiceManager();
            //var invoice = mgrPegasys.GeneralSearch();

            // For mockup
            var dir = Server.MapPath("~/Images");
            var path = Path.Combine(dir, fname);
            return path;
        }

        #endregion Test Image View?

        #endregion Image Management

        #region Mocked Views

        public ActionResult Mockups(string mockId)
        {
            return ReturnMockupView(mockId);
        }

        private ViewResult ReturnMockupView(string mockId)
        {
            if (mockId == "CodeList")
            {
                return View("Other/CodeList");
            }
            else if (mockId == "FaxNotes")
            {
                var FaxNotesModel = new FaxNotesViewModel();
                return View("Other/FaxNotes", FaxNotesModel);
            }
            else if (mockId == "InvEdit")
            {
                var InvEditModel = new InvEditViewModel();
                return View("Other/InvEdit", InvEditModel);
            }
            else if (mockId == "Notes")
            {
                var NotesModel = new NotesViewModel();
                return View("Other/Notes", NotesModel);
            }
            else if (mockId == "Input")
            {
                var InputModel = new InputViewModel();
                return View("Other/Input", InputModel);
            }

            else return View("");

            //var defaultModel = GetExceptionsViewModel();
            //return View("Index", defaultModel);
        }

        private List<P140Record> GetDummyP140ExceptionRecords(P140Record search)
        {
            var items = new List<P140Record>()
                {
                    new P140Record
                    {
                      PEG = "F", ID = "NONE", AmtAvailable = "0.00", InvoiceNo = "", Status = ""
                    },
                     new P140Record
                    {
                        PEG = "F", ID = "NONE", AmtAvailable = "0.00", InvoiceNo = "", Status = ""
                    },
                    new P140Record
                    {
                      PEG = "F", ID = "NONE", AmtAvailable = "0.00", InvoiceNo = "", Status = ""
                    }
               };
            return items;
        }

        #endregion Mocked Views

        public ActionResult ReturnToCaller(string Controller, string Action, NotesResult Result)
        {
            return RedirectToAction(Action, Controller, Result);
        }

        public ActionResult BackToUserException(NotesResult Results)
        {
            return GetUserExceptionView(false, Results.ExId, Results);
        }

        public ActionResult U044Accept(string invKeyId)
        {
            return Accept("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, invKeyId);
        }

        #region Notification Exceptions

        public ViewResult GetNotificationExceptionsView(string exCode, string exId)
        {
            var manager = new NotificationExceptionsManager();
            var model = manager.BuildModel(exCode, exId);
            Session[SessionKey.Model] = model;

            return View("NotificationExceptions/Exception", model);
        }

        public ActionResult ViewEDIPO(string Act, string ModNo, string InboxUidy, string Po_Id)
        {
            var thc = new TransHistController();

            return thc.ViewEDIPO(Act, ModNo, InboxUidy, Po_Id);
        }

        public JsonResult NotificationExceptionsUpdate(NotificationExceptionUpdateModel model)
        {
            NotificationExceptionsManager manager = new NotificationExceptionsManager();
            manager.Update(model);

            return Json("Success");
        }

        public JsonResult NotificationSendAsapUpdate(NotificationExceptionUpdateModel model)
        {
            NotificationExceptionsManager manager = new NotificationExceptionsManager();
            manager.SendAsapUpdate(model);

            return Json("Success");
        }

        #endregion Notification Exceptions

        #region Pegasys Main Exceptions

        private ViewResult GetMainExceptionsView(EXCEPTION exMain)
        {
            try
            {
                // For some reason GetMainExceptionsView gets called twice for the same exception
                // and so the model may already exist. Use te saved exception in this case. If there's
                // nothing saved or the saved exception is different than the current exception then
                // build a new view model.
                var vmExMain = TempData.Peek("vmExMain") as MainExceptionModel;
                if ((vmExMain == null) || (vmExMain.ExceptionId != exMain.EX_ID))
                {
                    vmExMain = new MainExceptionModel(exMain, this.PrepCode);

                    //Put this here for notes to use later
                    Session[SessionKey.DailyInterestAmount] = vmExMain.TabInvoice.DailyInterestAmount;

                    TempData["vmExMain"] = vmExMain;
                }
                return View("Main/Exception", vmExMain);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "GetMainExceptionsView");
            }
        }

        #endregion Pegasys Main Exceptions

        #region Pegasys P200

        private ViewResult GetP200View(EXCEPTION exMain)
        {
            var searchList = new List<SelectListItem>()
                    {
                        new SelectListItem { Text = "ACT", Value = "ACT" },
                        new SelectListItem { Text = "PDocNo", Value = "PDocNo" },
                        new SelectListItem { Text = "Vendor Name", Value = "VendName" },
                        new SelectListItem { Text = "Contract", Value = "Contract" },
                        new SelectListItem { Text = "PO Number", Value = "PONumber" },
                        new SelectListItem { Text = "Amount", Value = "Amount" },
                        new SelectListItem { Text = "Title", Value = "Title" }
                    };

            try
            {
                var mgr = new ExceptionsManager();
                var vmP200 = mgr.GetP200Exception(exMain);

                // Restore search params if they exist.
                var search = TempData.Peek("P200Search") as R200Search;
                if (search != null)
                {
                    ViewBag.txtSearchFor = search.SearchValue;
                    ViewBag.ddlSearchIn = new SelectList(searchList, ViewBag.txtSearchIn);
                }
                else
                {
                    ViewBag.ddlSearchIn = new SelectList(searchList);
                }

                return View("P200/Exception", vmP200);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "GetP200View");
            }
        }

        public ActionResult P200Search(DataSourceRequest request, R200Search search)
        {
            var helper = new R200SearchButton();
            var exception = TempData.Peek("exception") as EXCEPTION;

            var X200 = new List<X200Model>();
            if (search.SearchValue == null)
            {
                var resultFailed = X200.ToDataSourceResult(request);
                return Json(resultFailed, JsonRequestBehavior.AllowGet);
            }

            // Save so we can repopulate search params.
            TempData["P200Search"] = search;
            X200 = helper.InitializeP200(search.FieldName.ToUpper(), search.SearchValue, exception.PO_ID.ReplaceNull(""));
            var results = X200.ToDataSourceResult(request);

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        public ActionResult P200HasVcpo(string poId)
        {
            var hasVcpo = false;
            if (!string.IsNullOrWhiteSpace(poId))
            {
                var mgr = new PegasysPOManager();
                hasVcpo = mgr.P200HasVcpo(poId);
            }

            return Json(new { HasVcpo = hasVcpo }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult P200GetAmountAndDates(string poId, bool isPegasys)
        {
            if (string.IsNullOrWhiteSpace(poId))
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Response.Write("poId is required.");
                return Json(null, JsonRequestBehavior.AllowGet);
            }

            var mgr = new ExceptionsManager();
            var x200 = mgr.X200GetAmountAndDates(poId, isPegasys);
            var obj = new
            {
                AMOUNT = x200.AMOUNT.HasValue ? x200.AMOUNT.Value : 0,
                START_DT = x200.START_DT.HasValue ? x200.START_DT.Value.ToShortDateString() : "",
                END_DT = x200.END_DT.HasValue ? x200.END_DT.Value.ToShortDateString() : "",
            };

            return Json(obj, JsonRequestBehavior.AllowGet);
        }

        #endregion Pegasys P200

        #region P039

        [HttpPost]
        public ActionResult GetP039NoRRArray(string PDocNo, string Act)
        {
            var mgr = new P039Manager();
            var data = mgr.GetP039NoRRArray(PDocNo, Act);

            var result = new DataSourceResult()
            {
                Data = data
            };

            return Json(result);
        }

        [HttpPost]
        public ActionResult P039NotThisOne(ExceptionP039Model model)
        {
            //We will maybe need this later.
            //EX_ID = Server.UrlDecode(EX_ID);
            //var mgr = new P039Manager();
            //mgr.P039NotThisOne(InvKeyId, PrepCode, Act, EX_ID);

            var helper = new NotThisOneButton();
            var exception = TempData.Peek("exception") as EXCEPTION;
            var roles = Session[SessionKey.RoleModel] as RoleListModel;

            var Continue = helper.Initialize(exception, exception.PDOCNO, roles.PREPCODE);

            if (Continue == true)
            {
                Session[SessionKey.Helper] = helper;
                Session[SessionKey.TheQueue] = JsonConvert.DeserializeObject<List<NoRRArray>>(model.NoRRArrayJson);

                String dailyInterestAmount = "";
                if (Session[SessionKey.DailyInterestAmount] != null)
                {
                    dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                }

                var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                    ControllerAction.Exception.NotThisOne, ControllerAction.Exception.NotThisOneNotes, "Exceptions", dailyInterestAmount);

                return View("other/notes", vmNotes);
            }

            return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
        }

        [HttpPost]
        public ActionResult P039RRRequest(ExceptionP039Model model)
        {
            Session[SessionKey.TheQueue] = JsonConvert.DeserializeObject<List<NoRRArray>>(model.NoRRArrayJson);

            return RedirectToAction("RRRequest");
        }

        [HttpPost]
        public ActionResult P039Accept(ExceptionP039Model model)
        {
            model.YesRRArray = JsonConvert.DeserializeObject<List<NoRRArray>>(model.YesRRArrayJson);
            Session[SessionKey.P039Model] = model;

            return Accept("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, model.InvKeyId);
        }

        public ActionResult P202Accept(string invKeyId)
        {
            return Accept("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, invKeyId);
        }

        public ActionResult A224Accept()
        {
            return Accept("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, null);
        }

        public ActionResult A226Accept()
        {
            return Accept("", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", null, null);
        }

        #endregion P039

        private ViewResult LogErrorAndReturnView(System.Exception ex, string currentView)
        {
            var msg = string.Format("Error in Exceptions/{0}:\n\n\t{1}", currentView, ex.Message);
            Logging.AddWebError(msg, "Exceptions", currentView);
            ViewBag.Message = "System error encountered.";

            // Manually log error to ELMAH
            Elmah.ErrorSignal.FromCurrentContext().Raise(ex);

            return View("ViewErrorMessage");
        }

        public ActionResult U049RedirectToInvoiceEdit()
        {
            Session[SessionKey.U049Review] = true;

            return RedirectToAction(ControllerAction.Exception.InvEdit);
        }


        private void UpdateM237Exception(string returnVal3, string responseNotes)
        {
            var exception = TempData["exception"] as EXCEPTION;

            List<AEaccountingModel> matchLineNumsList = new List<AEaccountingModel>();

            if (TempData.Peek(SessionKey.M237Model) != null)
            {
                matchLineNumsList = TempData.Peek(SessionKey.M237Model) as List<AEaccountingModel>;
                TempData["exception"] = new M237Manager().Change(exception, PrepCode, matchLineNumsList, returnVal3, exception.RESPONSENOTES);
            }
        }

        public ActionResult ConfirmChangeLnumM303(List<AEgridModel> accountingInfoList)
        {
            var helper = Session[SessionKey.Helper] as ChangeButton;
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (accountingInfoList != null)
                Session["M303AccountingList"] = accountingInfoList;

            if (exception.ERR_CODE == "M224")
            {
                var model = TempData.Peek("M224Model") as ExceptionM303Model;
                var accrualsList = Session["M303AccountingList"] as List<AEgridModel>;

                if (accrualsList != null)
                {
                    foreach (var item in accrualsList)
                    {
                        if (model.POTab.POAccountLineRecs.Where(x => x.lnum == item.refd_lnum).Count() == 0)
                        {
                            ViewBag.Message = item.refd_lnum + " is not a valid line number on the PO. ";
                            return View("ViewErrorMessage");
                        }

                    }
                }
            }

            if (exception.ERR_CODE == "M303")
            {
                var model = TempData.Peek("M303Model") as ExceptionM303Model;
                var accrualsList = Session["M303AccountingList"] as List<AEgridModel>;

                if (accrualsList != null)
                {
                    if (!string.IsNullOrEmpty(model.ExpenseAccrualTab.PDOCNOae))
                    {
                        var message = new M303manager().ValidateLNumChanges(accrualsList, model.ExpenseAccrualTab.PDOCNOae);

                        if (!string.IsNullOrEmpty(message))
                        {
                            ViewBag.Message = message;
                            return View("ViewErrorMessage");
                        }
                    }
                }
            }

            if (accountingInfoList != null || Session["M303AccountingList"] != null)
            {

                var vmConfirm = new MessageDisplay()
                {
                    Title = "Clear",
                    Question = "Are you sure that you want to change the Line numbers on the Accrual?",
                    ReturnAction = "ChangeLnumM303",
                    ReturnController = "Exceptions",
                    ExId = exception.EX_ID,
                    ErrCode = exception.ERR_CODE,
                };

                return View("ConfirmYesNo", vmConfirm);
            }
            else
            {
                Session.Remove("M303AccountingList");
                return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });

            }

        }
        public ActionResult ChangeLnumM303(MessageDisplay vmConfirm)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;
            var AEGridList = Session["M303AccountingList"] as List<AEgridModel>;

            try
            {
                if (exception.ERR_CODE == "M303")
                {
                    new M303manager().UpdateLineNums(AEGridList, exception.AE_ID, exception.ACT, PrepCode);
                }
                else
                {
                    new M224Manager().UpdateLineNums(AEGridList, exception.PDOCNO, exception.AE_ID, exception.ACT, PrepCode);

                }
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "ReturnViewResult");
            }

            Session.Remove("M303AccountingList");
            return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });

        }

        public ActionResult M303MakeChangesConfirm(List<AEgridModel> accountingInfoList)
        {
            var helper = Session[SessionKey.Helper] as ChangeButton;
            var exception = TempData.Peek("exception") as EXCEPTION;
            var total = 0M;


            if (accountingInfoList != null)
            {
                TempData["M303AccountingList"] = accountingInfoList;
                accountingInfoList.ForEach(x => total += x.mdlamount ?? 0);
            }
            else
            {
                accountingInfoList = TempData.Peek("M303AccountingList") as List<AEgridModel>;
                accountingInfoList.ForEach(x => total += x.mdlamount ?? 0);
            }

            if (exception.ERR_CODE == "M303")
            {
                foreach (var item in accountingInfoList)
                {
                    if (item.mdlamount > item.aeamount)
                    {
                        ViewBag.Message = "Line " + item.refd_lnum + " cannot be more than available amount on the AE " + item.origamt + ". Try again or request correction";
                        return View("ViewErrorMessage");
                    }
                }
            }
            if (exception.ERR_CODE == "M224")
            {
                foreach (var item in accountingInfoList)
                {
                    if (item.mdlamount > item.poavail)
                    {
                        ViewBag.Message = "Line " + item.refd_lnum + " cannot be more than available amount on the PO " + item.poavail + ". Try again or request Mod.";
                        return View("ViewErrorMessage");
                    }
                }
            }


            var vmConfirm = new MessageDisplay()
            {
                Title = "Clear",
                Question = "Are you sure that you want to change Accrual amount to " + total + "?",
                ReturnAction = "M303MakeChanges",
                ReturnController = "Exceptions",
                ExId = exception.EX_ID,
                ErrCode = exception.ERR_CODE,
            };

            return View("ConfirmYesNo", vmConfirm);
        }

        public ActionResult M303MakeChanges(List<AEgridModel> accountingInfoList)
        {
            var exception = TempData.Peek("exception") as EXCEPTION;


            var AEGridList = TempData.Peek("M303AccountingList") as List<AEgridModel>;
            try
            {
                if (exception.ERR_CODE == "M303")
                    new M303manager().MakeChanges(AEGridList, exception.PDOCNO, exception.AE_ID, exception.ACT, PrepCode);
                else
                    new M224Manager().MakeChanges(AEGridList, exception.PDOCNO, exception.AE_ID, exception.ACT, PrepCode);
            }
            catch (System.Exception ex)
            {
                return LogErrorAndReturnView(ex, "ReturnViewResult");
            }

            return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });
        }



        public ActionResult ProcessedinPegClient(ExceptionO305Model model)
        {

            var exception = TempData.Peek("exception") as EXCEPTION;

            var errorMsg = new O305Manager().ProcessedinPegClient(model);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                ViewBag.Message = errorMsg;
                return View("ViewErrorMessage");
            }
            else
            {
                var fieldsToUpdate = new List<string>
                {
                    "ERR_RESPONSE",
                    "ALLPROCESS",
                    "PREPCODE",
                    "CLEARED_DATE",
                    "ALLPROCESS",
                    "OUT",
                    "ADDPC",
                    "RESPONSENOTES",
                };
                exception.ERR_RESPONSE = "A";
                exception.ALLPROCESS = " ";
                exception.PREPCODE = PrepCode;
                exception.CLEARED_DATE = DateTime.Now;
                exception.ALLPROCESS = "OUTBOX";
                exception.OUT = "F";
                exception.ADDPC = PrepCode;
                exception.RESPONSENOTES = model.Notes;

                // Restore updated exception in TempData
                TempData["exception"] = exception;

                var mgr = new ExceptionsManager();
                mgr.UpdateException(exception, fieldsToUpdate);

                var PegPOFieldstoUpdate = new List<string>
                {
                    "PO_STATUS",
                    "ERR_CODE"
                };

                new PegasysPO_FrmManager().UpdatePegasysPOFrm(new PEGASYSPO_FRM { PO_STATUS = "KEYED", ERR_CODE = null }, PegPOFieldstoUpdate);
            }

            return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });

        }
        public ActionResult AcceptO305(ExceptionO305Model model)
        {

            var exception = TempData.Peek("exception") as EXCEPTION;

            var errorMsg = new O305Manager().Accept(model);

            if (!string.IsNullOrEmpty(errorMsg))
            {
                ViewBag.Message = errorMsg;
                return View("ViewErrorMessage");
            }
            else
            {

                var fieldsToUpdate = new List<string>
                {
                    "ERR_RESPONSE",
                    "ALLPROCESS",
                    "PDOCNO",
                    "PREPCODE",
                    "CLEARED_DATE",
                    "ALLPROCESS",
                    "OUT",
                    "ADDPC",
                    "RESPONSENOTES",
                };
                exception.ERR_RESPONSE = "A";
                exception.PDOCNO = model.PDOCNO;
                exception.PREPCODE = PrepCode;
                exception.CLEARED_DATE = DateTime.Now;
                exception.ALLPROCESS = "OUTBOX";
                exception.OUT = "F";
                exception.ADDPC = PrepCode;
                exception.RESPONSENOTES = model.Notes;

                // Restore updated exception in TempData
                TempData["exception"] = exception;

                var mgr = new ExceptionsManager();
                mgr.UpdateException(new EXCEPTION() { VENDNAME = model.VendorName, PDOCNO = model.PDOCNO }, new List<string>() { "VENDNAME", "PDOCNO" });
                mgr.UpdateException(exception, fieldsToUpdate);

                var PegPOFieldstoUpdate = new List<string>
                {
                    "PO_STATUS",
                    "ERR_CODE"
                };

                new PegasysPO_FrmManager().UpdatePegasysPOFrm(new PEGASYSPO_FRM { PO_STATUS = "PEAREADY", ERR_CODE = null }, PegPOFieldstoUpdate);
            }

            return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });

        }


        public ActionResult RejectO305(ExceptionO305Model model)
{
    var exception = TempData.Peek("exception") as EXCEPTION;

    var PegPOFieldstoUpdate = new List<string>
                {
                    "PO_STATUS",
                    "ERR_CODE"
                };

    new PegasysPO_FrmManager().UpdatePegasysPOFrm(new PEGASYSPO_FRM { PO_STATUS = "REJECT", ERR_CODE = null }, PegPOFieldstoUpdate);

    var fieldsToUpdate = new List<string>
                {
                    "ERR_RESPONSE",
                    "ALLPROCESS",
                    "PDOCNO",
                    "PREPCODE",
                    "CLEARED_DATE",
                    "ALLPROCESS",
                    "OUT",
                    "ADDPC",
                    "RESPONSENOTES",
                };
    exception.ERR_RESPONSE = "X";
    exception.PREPCODE = PrepCode;
    exception.CLEARED_DATE = DateTime.Now;
    exception.ALLPROCESS = "REJECT";
    exception.OUT = "F";
    exception.ADDPC = PrepCode;
    exception.RESPONSENOTES = model.Notes;

    // Restore updated exception in TempData
    TempData["exception"] = exception;

    var mgr = new ExceptionsManager();
    mgr.UpdateException(exception, fieldsToUpdate);

    return RedirectToAction(ControllerAction.Exception.DisplayException, "Exceptions", new { exCode = exception.ERR_CODE, exId = exception.EX_ID.Replace("&amp;", "&") });

}
[HttpPost]
public ActionResult A224ChangeLnum(A224MainTabModel mainModel)
{
    mainModel.MainTabGrid = JsonConvert.DeserializeObject<List<Maketmp>>(mainModel.ChangeLnumJson);
    var exception = TempData.Peek("exception") as EXCEPTION;
    Session["temp224"] = mainModel.MainTabGrid;
    var vmConfirm = new MessageDisplay()
    {
        Title = "",
        Question = "Are you sure that you want to change the line numbers on the RR?",
        Origin = ControllerAction.Exception.A224ChangeLnum,
        Controller = "Exceptions",
        ReturnAction = ControllerAction.Exception.UpdateLineNums,
        ReturnController = "Exceptions",
        ExId = exception.EX_ID,
        ErrCode = exception.ERR_CODE,
    };

    return View("ConfirmYesNo", vmConfirm);
}

[HttpPost]
public ActionResult A226ChangeLnum(A226MainTabModel mainModel)
{
    mainModel.MainTabGrid = JsonConvert.DeserializeObject<List<Maketmp>>(mainModel.ChangeLnumJson);
    var exception = TempData.Peek("exception") as EXCEPTION;
    Session["temp226"] = mainModel.MainTabGrid;
    var vmConfirm = new MessageDisplay()
    {
        Title = "",
        Question = "Are you sure that you want to change the line numbers on the RR?",
        Origin = ControllerAction.Exception.A226ChangeLnum,
        Controller = "Exceptions",
        ReturnAction = ControllerAction.Exception.UpdateLineNums,
        ReturnController = "Exceptions",
        ExId = exception.EX_ID,
        ErrCode = exception.ERR_CODE,
    };

    return View("ConfirmYesNo", vmConfirm);
}

public ActionResult UpdateLineNums(MessageDisplay vmConfirm)
{
    var exception = TempData.Peek("exception") as EXCEPTION;


    if (vmConfirm.Response == true)
    {
        if (Session["temp224"] != null)
        {
            var mgrA224 = new A224Manager();
            var data = Session["temp224"] as List<Maketmp>;
            var prepcode = Session[SessionKey.PrepCode].ToString();

            mgrA224.ChangeLnums(data, prepcode, exception.ACT, exception.PDOCNO, exception.RR_ID);
        }

        if (Session["temp226"] != null)
        {
            var mgrA226 = new A226Manager();
            var data = Session["temp226"] as List<Maketmp>;
            var prepcode = Session[SessionKey.PrepCode].ToString();

            mgrA226.ChangeLnums(data, prepcode, exception.ACT, exception.PDOCNO, exception.RR_ID);
        }

        return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
    }
    Session.Remove("temp224");
    Session.Remove("temp226");
    return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
}


//EARequest button
public ActionResult EARequestConfirm()
{
    var exception = TempData.Peek("exception") as EXCEPTION;            
           
    var vmConfirm = new MessageDisplay()
    {
        Title = "Hold?",
        Question = "There is an Expense Accrual Correction(" + exception.RR_ID + ") being Processed in VITAP. Do you still want to Request an Expense Accrual?",
        Origin = ControllerAction.Exception.DisplayException,
        Controller = "Exceptions",
        ReturnAction = ControllerAction.Exception.EARequest,
        ReturnController = "Exceptions",
        ExId = exception.EX_ID,
        ErrCode = exception.ERR_CODE,
    };

    return View("ConfirmYesNo", vmConfirm);
}

public ActionResult EARequest(MessageDisplay vmConfirm)
{
            var exception = TempData.Peek("exception") as EXCEPTION;

            if (vmConfirm.Response)
            {

                var helper = new EARequestButton();
                var roles = Session[SessionKey.RoleModel] as RoleListModel;
                var Continue = helper.Initialize(exception, roles.PREPCODE);

                if (Continue == true)
                {
                    Session[SessionKey.Helper] = helper;

                    String dailyInterestAmount = "";
                    if (Session[SessionKey.DailyInterestAmount] != null)
                    {
                        dailyInterestAmount = Session[SessionKey.DailyInterestAmount].ToString();
                    }

                    var vmNotes = new NotesViewModel(exception, exception.ERR_CODE, exception.ACT, exception.EX_ID, exception.PDOCNO,
                        ControllerAction.Exception.EARequest, ControllerAction.Exception.EARequestNotes, "Exceptions", dailyInterestAmount);

                    return View("other/notes", vmNotes);
                }
                Session.Remove("temp226");
            }
    return RedirectToAction("DisplayException", new { exCode = exception.ERR_CODE, exId = exception.EX_ID });
  
}

        [ValidateInput(false)]
        public ActionResult EARequestNotes(NotesViewModel vmNotes)
{
    //If Finish button was not pushed, exit now
    if (vmNotes.returnVal1 != "FINISH") { return RedirectToAction("Index", Session[SessionKey.ExceptionsListModel] as ExceptionListModel); }

    var helper = Session[SessionKey.Helper] as EARequestButton;
    var roles = Session[SessionKey.RoleModel] as RoleListModel;

    helper.SetNotes(vmNotes);
    //helper.FinishCode(roles.PREPCODE);
    helper.FinishCode(PrepCode);

    var request = Session[SessionKey.MainRequest] as DataSourceRequest;
    var search = Session[SessionKey.MainSearch] as ExceptionListModel;
    return RedirectToAction("Index", new { request, search });
}



    }
}