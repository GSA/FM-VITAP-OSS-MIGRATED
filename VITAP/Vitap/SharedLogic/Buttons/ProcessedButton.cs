using System;
using System.Collections.Generic;
using VITAP.Data.PegasysEntities;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;
using VITAP.Library.Strings;
using VITAP.Data.Managers;
using VITAP.Data.Models.PegasysPA;

namespace VITAP.SharedLogic.Buttons
{
    public class ProcessedButton : ButtonExceptionManager
    {
        /// <summary>
        /// Button Click Code
        /// Does not appear to have any missing code or issues at this time...Still untested
        /// </summary>
        /// <param name="exception"></param>
        public string Initialize(EXCEPTION exc, string prepCode)
        {
            theMsg = "";
            SetVariables(exc, prepCode, DocStatus.Processed);
            if (!Check_230_Exceptions()) { return theMsg; }

            return theMsg;
        }

        public void SetNotes(NotesViewModel Notes)
        {
            SetNotesValues(Notes);
        }

        public string FinishCode()
        {
            NewNotes();

            switch (exception.ERR_CODE)
            {
                case "M230":
                    return ExceptionM230();
                case "A230":
                    return ExceptionA230();

                case "P230":
                    return ExceptionP230();

                case "P231":
                case "P232":
                case "P234":
                    return ExceptionP231(exception);
            }
            return "";
        }

        /// <summary>
        /// Handles M230 exceptions
        /// Get M230 exceptions for the ae_id
        /// Pulls the MF_IC data for the ae_id
        /// </summary>
        protected string ExceptionM230()
        {
            var rtnExc = GetExceptionM230();

            if (rtnExc.Count == 0)
            {
                return "Exception not found.";
            }
            else 
            {
                var aeId = exception.AE_ID;
                var mgr = new PegasysAEManager();

                if (aeId.Right(1) == "C")
                {
                    // Use pdocnoae.
                    var frmtblAE = mgr.GetPegasysAEByKey(aeId);
                    if (frmtblAE == null) {
                        // Should not happen in Prod, but sending the user a message so we don't waste time on it again in test.
                        return "No row found in PEGASYSAE_FRM for AE_ID: " + aeId + ".";
                    }
                }

                // Just figure out if any records exist.
                if (!mgr.FindAEByDocNumCh(aeId))
                {
                    //Update the exceptions table 
                    string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" +
                        "M230 cleared because the AE is no longer in the Pegasys form table -- it was revised and processed by the user.";
                    UpdateException(exception, "Q", notes.returnVal7, exception.EX_MEMO2, responsenotes, "");

                    UpdatePegasysAEToInPeg(exception.AE_ID);

                    return "";
                }
                else
                {
                    return "The Expense Accrual is still in the form tables.";
                }
            }
        }

        private void UpdatePegasysAEToInPeg(string ae_id)
        {
            var rtnAE = GetPegasysAEByKey(ae_id);

            var fieldsToUpdate = new List<string>
                                {
                                    "AE_STATUS",
                                    "ERR_CODE"
                                };

            rtnAE.AE_STATUS = "INPEG";
            rtnAE.ERR_CODE = null;

            UpdatePegasysAE(rtnAE, fieldsToUpdate);
        }

        /// <summary>
        /// Handles A230 exceptions
        /// Get A230 exceptions for the rr_id
        /// Pulls the MF_IC data for the rr_id
        /// </summary>
        protected string ExceptionA230()
        {
            var rtnExc = GetExceptionA230();

            if (rtnExc.Count == 0)
            {
                return "Exception not found.";
            }
            else
            {
                var rrId = exception.RR_ID;
                var mgr = new PegasysRRManager();

                // Just figure out if any records exist.
                if (!mgr.FindRRByDocNumCh(rrId))
                {
                    //Update the exceptions table 
                    string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" +
                        "A230 cleared because the RR is no longer in the Pegasys form table -- it was revised and processed by the user.";
                    UpdateException(exception, "Q", notes.returnVal7, exception.EX_MEMO2, responsenotes, "");

                    UpdatePegasysRRToOutbox(exception.RR_ID);

                    return "";
                }
                else
                {
                    return "The Receiving Report is still in the form tables.";
                }
            }
        }

        private void UpdatePegasysRRToOutbox(string rr_id)
        {
            var rtnRR = GetPegasysRRByKey(rr_id);

            var fieldsToUpdate = new List<string>
                                {
                                    "RR_STATUS",
                                    "ERR_CODE"
                                };

            rtnRR.RR_STATUS = "OUTBOX";
            rtnRR.ERR_CODE = null;

            UpdatePegasysRR(rtnRR, fieldsToUpdate);
        }

        /// <summary>
        /// Handles P230 exceptions
        /// Get P230 exceptions for the inv_key_id
        /// Pulls the MF_II data for the inv_key_id
        /// </summary>
        /// <param name="exception"></param>
        protected string ExceptionP230()
        {
            var rtnExc = GetExceptionP230();

            if (rtnExc.Count == 0)
            {
                return "The invoice is still in the form tables.";
            }
            else
            { 
                var rtnMFII = GetMFIIFrmData(exception.INV_KEY_ID);
                if (rtnMFII.Count == 0)
                {
                    rtnMFII = GetMFIIData(exception.INV_KEY_ID);
                }

                if (rtnMFII.Count == 0)
                {
                    //Invoice was deleted...Do nothing
                    return "";
                }
                else
                {
                    //Invoice was changed and processed
                    //Update the exceptions table 
                    string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" +
                        "P230 cleared because the Invoice is no longer in the Pegasys form table -- it was revised and processed by the user.";
                    UpdateException(exception, "Q", notes.returnVal7, exception.EX_MEMO2, responsenotes, "");

                    //Update the PegasysInvoice table to INPEG
                    UpdatePegasysInvoiceToInPeg(exception.INV_KEY_ID);
                }
            }

            return "";
        }

        private void UpdatePegasysInvoiceToInPeg(string inv_key_id)
        {
            var rtnInv = GetPegasysInvoiceByKey(inv_key_id);

            var fieldsToUpdate = new List<string>
                                {
                                    "INV_STATUS",
                                    "ERR_CODE"
                                };

            rtnInv.INV_STATUS = "INPEG";
            rtnInv.ERR_CODE = null;

            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
        }

        protected string ExceptionP231(EXCEPTION exception)
        {
            string ThisRR = "", OneRR = "", ThisPO = "", Doc_Date = "", SchedDate = "", P7Num = "", RefdLimit = "", RR_List = "";
            decimal? PayAmt = 0, Interest = 0;

            //Check MF_II table - MFMI in FoxPro
            var rtnMFII = GetMFIIData(exception.INV_KEY_ID);

            if (rtnMFII.Count == 0)
            {
                return "";
            }

            //Invoice was changed and processed
            if (rtnMFII[0].INVD_AM == 0 || rtnMFII[0].CLSD_AM == 0)
            {
                //Still a P231/ P232 Exception
                return "Invoice has not been paid yet..";
            }

            //Update the Exception
            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" +
                "Exception cleared because the Invoice is no longer in the Pegasys form table -- " +
                "it was revised and processed by the user.";
            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "PROCESSED");

            UpdatePegasysInvoiceToInPeg(exception.INV_KEY_ID);
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            //"Checking if Invoice has been PAID.."
            var InvPeg = GetMFRefInfo(exception.INV_KEY_ID);

            if (InvPeg.Count == 0)
            {
                Doc_Date = rtnMFII[0].CLSD_DT.ShortDate();
                SchedDate = "???";
                P7Num = "???";
                PayAmt = rtnMFII[0].CLSD_AM;
                Interest = 0;
            }
            else
            {
                var invPegPA = new List<MFIPDataResult>();

                foreach (var row in InvPeg)
                {
                    invPegPA = GetMFIPData(row);
                    if (invPegPA[0].DOC_STUS == DocStatus.Processed)
                    {
                        break;
                    }
                }
                if (invPegPA[0].DOC_STUS != DocStatus.Processed)
                {
                    return "";
                }
                PayAmt = getPayAmt(invPegPA[0]);

                Interest = 0;
                if (invPegPA[0].DSBD_AM > 0)
                {
                    Interest = invPegPA[0].DSBD_AM - invPegPA[0].AUTD_AM;
                }

                SchedDate = invPegPA[0].SCHD_DT.ShortDate();
                Doc_Date = invPegPA[0].DOC_DT.ShortDate();
                P7Num = InvPeg[InvPeg.Count - 1].REFG_DOC_NUM.Trim();
                RR_List = "";
                RefdLimit = "";
                if (rtnInv.INV_KEY_ID.Left(1) == "I")
                {
                    RefdLimit = "I";
                }
                else
                {
                    RefdLimit = "R";
                }

                var MFRip7 = GetMFRip7(RefdLimit, P7Num);

                if (MFRip7.Count == 0)
                {
                    if (rtnInv.PDOCNORR == null)
                    {
                        ThisRR = "";
                        OneRR = "";
                    }
                    else
                    {
                        ThisRR = rtnInv.PDOCNORR;
                        OneRR = rtnInv.PDOCNORR;
                    }
                }
                else if (MFRip7.Count == 1)
                {
                    ThisRR = MFRip7[0].REFD_DOC_NUM;
                    OneRR = MFRip7[0].REFD_DOC_NUM;
                }
                else if (MFRip7.Count > 1)
                {
                    ThisRR = "MRR";
                    OneRR = MFRip7[0].REFD_DOC_NUM;
                    RR_List = "";

                    foreach (var row in MFRip7)
                    {
                        RR_List += row.REFD_DOC_NUM + ", ";
                    }
                    RR_List = RR_List.Left(RR_List.Length - 2);
                }

                var rtnPO = GetPOInfoFromPegasys(OneRR);

                ThisPO = rtnInv.PDOCNOPO;

                if (rtnPO != null && rtnPO.REFD_DOC_NUM.Substring(1, 1) != "E")
                {
                    ThisPO = rtnPO.REFD_DOC_NUM;
                }
            }

            if (String.IsNullOrWhiteSpace(ThisPO))
            {
                return "";
            }
            else
            { 
                var strInterest = Interest > 0 ? " --  Interest $" + Interest.ToString() : "";
                var strCuffMemo = P7Num.Trim() + " was generated by Pegasys in the amount of " + PayAmt + " for invoice " +
                        rtnMFII[0].INVC_NUM + " on " + Doc_Date + strInterest +
                    " --- Estimated Schedule date " + SchedDate + " " + RR_List;
                using (var contextVitap = new OracleVitapContext())
                {
                    contextVitap.Configuration.AutoDetectChangesEnabled = true;
                    var transhist = new TRANSHIST()
                    {
                        ACT = rtnInv.ACT,
                        PDOCNO = ThisPO,
                        PO_ID = ThisPO,
                        RR_ID = ThisRR,
                        INV_KEY_ID = rtnInv.INV_KEY_ID,
                        TRANSDATE = DateTime.Now,
                        TRANSAMT = PayAmt,
                        ACTNCODE = "P7",
                        PREPCODE = "VI",
                        CUFF_MEMO = strCuffMemo,
                        PA_ID = P7Num.Trim()
                    };

                    contextVitap.TRANSHISTs.Add(transhist);
                    contextVitap.SaveChanges();
                }

                var fieldsToUpdate = new List<string>
                    {
                        "INV_STATUS",
                        "PAIDDATE",
                        "PAY_AMT",
                        "INVOICE"
                    };

                rtnInv.INV_STATUS = DocStatus.Paid;
                rtnInv.PAIDDATE = DateTime.Parse(Doc_Date);
                rtnInv.PAY_AMT = PayAmt;
                strCuffMemo = "Invoice number changed from " + rtnInv.INVOICE + " to" + rtnMFII[0].INVC_NUM + " per Pegasys";
                bool insertTransHist = false;
                if (rtnInv.INVOICE != rtnMFII[0].INVC_NUM)
                {
                    rtnInv.INVOICE = rtnMFII[0].INVC_NUM;
                    insertTransHist = true;
                }

                UpdatePegasysInvoice(rtnInv, fieldsToUpdate);

                if (insertTransHist)
                {
                    InsertTranshist(exception, "", strCuffMemo, "Invoice", PrepCode);
                }
            }
            return "";
        }

        private decimal? getPayAmt(MFIPDataResult invPegPA)
        {
            decimal? PayAmt = 0;

            if (invPegPA.DSBD_AM > 0)
            {
                PayAmt = invPegPA.DSBD_AM;
            }
            else if (invPegPA.SCHD_AM > 0)
            {
                PayAmt = invPegPA.SCHD_AM;
            }
            else if (invPegPA.INTS_AM > 0)
            {
                PayAmt = invPegPA.INTS_AM;
            }
            else if (invPegPA.CLSD_AM > 0)
            {
                PayAmt = invPegPA.CLSD_AM;
            }
            else
            {
                PayAmt = invPegPA.AUTD_AM;
            }
            return PayAmt;
        }

    }
}
