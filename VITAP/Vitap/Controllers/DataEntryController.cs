using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using VITAP.Data;
using System.Reflection;

using VITAP.Data.Managers;
using VITAP.Data.Models;
using VITAP.Data.Models.DataEntry;
using VITAP.Library;
using VITAP.Library.Strings;
using VITAP.Utilities.Attributes;

namespace VITAP.Controllers
{
    #region Index View Methods

    public class DataEntryController : VitapBaseController
    {
        PegasysInvoiceManager pegInvoiceManager = new PegasysInvoiceManager();
        string prepcode = "";

        // GET: DataEntry
        [HttpGet]
        public ActionResult Index(string InvoiceType)
        {
            string batchPrefix = null;
            AddInvoiceViewModel vm = new AddInvoiceViewModel();
            ViewBag.Message = "";
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            prepcode = this.PrepCode;

            switch (InvoiceType) {
                case DataEntry.TypeTops:
                    // TOPS
                    batchPrefix = pegInvoiceManager.GetBatchPrefixByConfigValue(DataEntry.BatchPrefixTopsConfig);
                    if (String.IsNullOrEmpty(batchPrefix)) batchPrefix = DataEntry.BatchPrefixTopsDefault;
                    break;
                case DataEntry.TypeNonTops:
                    // Non Tops
                    batchPrefix = pegInvoiceManager.GetBatchPrefixByConfigValue(DataEntry.BatchPrefixNonTopsConfig);
                    if (String.IsNullOrEmpty(batchPrefix)) batchPrefix = DataEntry.BatchPrefixNonTopsDefault;
                    break;
                case DataEntry.TypeConst:
                    // Construction
                    batchPrefix = pegInvoiceManager.GetBatchPrefixByConfigValue(DataEntry.BatchPrefixConstConfig);
                    if (String.IsNullOrEmpty(batchPrefix)) batchPrefix = DataEntry.BatchPrefixConstDefault;
                    break;
                case DataEntry.TypeNonConst:
                    // Non Construction
                    batchPrefix = pegInvoiceManager.GetBatchPrefixByConfigValue(DataEntry.BatchPrefixNonConstConfig);
                    if (String.IsNullOrEmpty(batchPrefix)) batchPrefix = DataEntry.BatchPrefixNonConstDefault;
                    break;
                default:
                    // Not supported. Will return null view.
                    return View(vm);
            }

            pegInvoiceManager.UpdateFailedDataEntries();
            var invoices = GetKeyablePegasysInvoicesByBatchPrefix(batchPrefix);

            // Find the first invoice that hasn't been keyed.
            if (invoices.Count > 0) {
                foreach (var invoice in invoices)
                {
                    AddInvoiceViewModel thisVM = AddInvoiceViewModel.MapToViewModel(invoice);
                    // Validate and claim this invoice.
                    if (ValidateInvoiceData(thisVM) &&
                        ClaimPegasysInvoiceForKeying(invoice))
                    {
                        // Go with this one.
                        FormatInvoiceData(invoice, thisVM);
                        vm = thisVM;
                        break;
                    }
                }
            }

            // Find any with working images?
            if (vm.IMAGEID == null)
            {
                if (!String.IsNullOrEmpty(ViewBag.ErrorMessage)) ViewBag.ErrorMessage += "<br />";
                ViewBag.ErrorMessage += DataEntry.NoValidInvoicesFound;
            }

            ViewBag.InvoiceType = InvoiceType ?? default(string);
            vm.INVOICETYPE = InvoiceType ?? default(string);
            return View(vm);
        }

        List<PEGASYSINVOICE> GetKeyablePegasysInvoicesByBatchPrefix(string batchPrefix)
        {
            // Find the number of invoices with this batch that are keyready.
            var invoices = pegInvoiceManager.GetKeyablePegasysInvoicesByBatchPrefix(batchPrefix);
            string plural = "";
            int totalInvoices = invoices.Count();
            if (totalInvoices != 1)
            {
                plural = "s";
            }
            ViewBag.Message = totalInvoices.ToString() + " invoice" + plural + " to key.";
            return invoices;
        }

        private bool ValidateInvoiceData(AddInvoiceViewModel vm)
        {
            bool result = false;
            if (!String.IsNullOrEmpty(vm.IMAGEID))
            {
                // Verify image file path.
                string imagePath = GSA.R7BD.Utility.Utilities.GetImageNowPath(vm.IMAGEID);
                if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
                {
                    vm.IMAGEPATH = imagePath;

                    // Grab page count.
                    string extension = Path.GetExtension(imagePath);
                    byte pageCount = 1;
                    if (extension == ".tiff" || extension == ".tif")
                    {
                        TIF TheFile = new TIF(imagePath);
                        pageCount = (byte)TheFile.PageCount;
                        TheFile.Dispose();
                    }

                    vm.DOCPAGE = pageCount;
                    result = true;
                }
            }
            return result;
        }

        private bool ClaimPegasysInvoiceForKeying(PEGASYSINVOICE invoice)
        {
            // If update is successful, we claimed it.
            // If it wasn't (i.e., someone already set these fields), we failed.
            invoice.OUT = "T";
            invoice.OUTPREP = prepcode;
            invoice.OUTDATE = DateTime.Now;
            return pegInvoiceManager.UpdatePegasysInvoiceReturnResult(invoice);
        }

        private void FormatInvoiceData(PEGASYSINVOICE invoice, AddInvoiceViewModel vm)
        {
            // Grab the legal business name from Pegasys, if it exists.
            string legalBusinessName = pegInvoiceManager.GetLegalBusinessName(invoice);

            // Address text area is as follows:
            // legalBusinessName
            // ADDR_L1
            // ADDR_L2
            // ADDR_L3
            // ADDR_CITY, ADDR_STATE ADDR_ZPCD
            string address = "";
            if (!string.IsNullOrEmpty(legalBusinessName)) address = legalBusinessName + "\n";
            if (!string.IsNullOrEmpty(invoice.ADDR_L1)) address += invoice.ADDR_L1 + "\n";
            if (!string.IsNullOrEmpty(invoice.ADDR_L2)) address += invoice.ADDR_L2 + "\n";
            if (!string.IsNullOrEmpty(invoice.ADDR_L3)) address += invoice.ADDR_L3 + "\n";
            if (!string.IsNullOrEmpty(invoice.ADDR_CITY)) address += invoice.ADDR_CITY;
            if (!string.IsNullOrEmpty(invoice.ADDR_STATE)) address += ", " + invoice.ADDR_STATE;
            if (!string.IsNullOrEmpty(invoice.ADDR_ZPCD)) address += " " + invoice.ADDR_ZPCD;
            vm.VENDADDRESS = address;
        }

        private bool KeyPegasysInvoice(AddInvoiceViewModel viewModel)
        {
            // Grab the full invoice.
            PEGASYSINVOICE invoice = pegInvoiceManager.GetPegasysInvoiceByKeyId(viewModel.INV_KEY_ID);
            if (invoice == null) return false;

            // Update invoice with our view model.
            invoice = AddInvoiceViewModel.MapToEntityFramework(viewModel, invoice);

            // Set keying specific fields.
            invoice.OUT = "F";
            invoice.OUTPREP = null;
            invoice.OUTDATE = null;
            invoice.DATAENTRY_FL = "T";
            invoice.KEYDATE = DateTime.Now;
            invoice.KEYPC = prepcode;
            invoice.PREPCODE = prepcode;
            invoice.INV_STATUS = DataEntry.Keyed;

            if (!viewModel.VENDORMATCH) {
                invoice.ADDR_L1 = null;
                invoice.ADDR_L2 = null;
                invoice.ADDR_L3 = null;
                invoice.ADDR_CITY = null;
                invoice.ADDR_STATE = null;
                invoice.ADDR_ZPCD = null;
                invoice.VEND_ADDR_CD = DataEntry.VendAddrCd;
            }

            if (viewModel.GENE043S) invoice.NEEDERR_CODE = DataEntry.NeedErrCode;

            // Update it.
            return pegInvoiceManager.UpdatePegasysInvoiceReturnResult(invoice);
        }

        private TRANSHIST CreateTransHistForChange(AddInvoiceViewModel inv, string memo, string allProcess)
        {
            var th = new TRANSHIST()
            {
                TH_ID = null,
                PDOCNO = inv.PDOCNOPO,
                INV_KEY_ID = inv.INV_KEY_ID,
                ACT = inv.ACT,
                ALLPROCESS = allProcess,
                TRANSAMT = inv.AMOUNT,
                PREPCODE = prepcode,
                CUFF_MEMO = memo
            };
            return th;
        }

        private IMAGETRANSHIST CreateImageTransHistForChange(AddInvoiceViewModel inv, string memo)
        {
            var th = new IMAGETRANSHIST()
            {
                IMG_TH_ID = null,
                IMG_KEY_ID = "NONE",
                IMAGEBATCH = inv.IMAGEBATCH,
                BATCH_INDEX = "NO",
                STATUS = "K",
                QUEUE = "",
                VITAP_KEY_ID = inv.INV_KEY_ID,
                IMAGETYPE = "INV",
                PROCESS = DataEntry.Keyed,
                SOURCE = DataEntry.Keyed,
                PDOCNO = inv.PDOCNOPO,
                ACT = inv.ACT,
                PREPCODE = prepcode,
                IMAGEID = inv.IMAGEID,
                NOTES = memo
            };
            return th;
        }

        [HttpPost, ActionName("Index"), SubmitButton(Name = "btnAdd")]
        public ActionResult AddInvoice(AddInvoiceViewModel viewModel)
        {
            prepcode = this.PrepCode;

            // Key this invoice.
            if (!KeyPegasysInvoice(viewModel)) {
                TempData["ErrorMessage"] = "Update failed for invoice " + viewModel.INV_KEY_ID;
            }

            // Write transhist entries.
            string memo = "";
            string allProcess = "";
            switch (viewModel.INVOICETYPE) {
                case DataEntry.TypeTops:
                    memo = DataEntry.TopsMemo;
                    allProcess = DataEntry.TopsAllProcess;
                    break;
                case DataEntry.TypeNonTops:
                    memo = (viewModel.GENE043S ? DataEntry.NonTopsMemoError : DataEntry.NonTopsMemo);
                    allProcess = DataEntry.NonTopsAllProcess;
                    break;
                case DataEntry.TypeConst:
                    memo = (viewModel.GENE043S ? DataEntry.ConstMemoError : DataEntry.ConstMemo);
                    allProcess = DataEntry.ConstAllProcess;
                    break;
                case DataEntry.TypeNonConst:
                    memo = DataEntry.NonConstMemo;
                    allProcess = DataEntry.NonConstAllProcess;
                    break;
                default:
                    // Shouldn't get here.
                    break;
            }
            var mgrTH = new TransHistManager();
            TRANSHIST th = CreateTransHistForChange(viewModel, memo, allProcess);
            mgrTH.InsertTransHist(th);
            var mgrITH = new ImageTransHistManager();
            IMAGETRANSHIST ith = CreateImageTransHistForChange(viewModel, memo);
            mgrITH.InsertImageTransHist(ith);

            // Reload the index page to move to the next invoice, if available.
            return RedirectToAction("Index", new { InvoiceType = viewModel.INVOICETYPE });
        }

        private bool RejectPegasysInvoice(AddInvoiceViewModel viewModel)
        {
            // Grab the full invoice.
            PEGASYSINVOICE invoice = pegInvoiceManager.GetPegasysInvoiceByKeyId(viewModel.INV_KEY_ID);
            if (invoice == null) return false;

            // Set rejection fields.
            invoice.OUT = null;
            invoice.OUTPREP = null;
            invoice.OUTDATE = null;
            invoice.INV_STATUS = DataEntry.ImgReject;

            // Update it.
            return pegInvoiceManager.UpdatePegasysInvoiceReturnResult(invoice);
        }

        private void MarkImageListAsRejected(IMAGELIST image, string reason)
        {
            image.STATUS = "X";
            image.PREPCODE = prepcode;
            image.SOURCE = DataEntry.ImgReject;
            image.REASON = reason;
        }

        [HttpPost, ActionName("Index"), SubmitButton(Name = "btnReject")]
        public ActionResult RejectInvoice(AddInvoiceViewModel viewModel)
        {
            string errorMessage = "";
            prepcode = this.PrepCode;

            // Format reason string.
            string reason = DateTime.Now.ToString() + " " + prepcode;
            reason += " - " + viewModel.REJECTOPTION + " / " + viewModel.REJECTDESCRIPTION;

            // Mark invoice as rejected.
            if (!RejectPegasysInvoice(viewModel)) {
                errorMessage = "Reject invoice failed. ";
            }

            // Write transhist entry.
            string memo = "Image Rejected from data entry - " + reason;
            var mgrTH = new TransHistManager();
            TRANSHIST th = CreateTransHistForChange(viewModel, memo, DataEntry.ImgReject);
            mgrTH.InsertTransHist(th);

            // Update imagelist.
            var mgrIL = new ImageListManager();
            IMAGELIST il = mgrIL.GetImageListByImageId(viewModel.IMAGEID);
            if (il != null) {
                // Mark as rejected and update.
                MarkImageListAsRejected(il, reason);
                mgrIL.UpdateImageList(il);
            } else {
                var mgrILH = new ImageListHistManager();
                IMAGELISTHIST ilh = mgrILH.GetImageListHistByImageId(viewModel.IMAGEID);
                if (ilh != null) {
                    // Convert to ImageList and mark as rejected.
                    il = mgrIL.MapToImageList(ilh);
                    MarkImageListAsRejected(il, reason);
                    // Add to ImageList.
                    mgrIL.InsertImageList(il);
                    // Remove from ImageListHist.
                    mgrILH.DeleteImageListHist(ilh);
                } else {
                    // Uh oh. No imagelisthist entry.
                    errorMessage += viewModel.IMAGEID + " does not exist in the ImageListHist table.";
                }
            }

            // Reload the index page to move to the next invoice, if available.
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction("Index", new { InvoiceType = viewModel.INVOICETYPE });
        }

        #endregion Index View Methods
    }
}