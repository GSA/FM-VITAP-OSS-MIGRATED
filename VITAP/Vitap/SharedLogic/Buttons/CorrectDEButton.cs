using System;
using System.Collections.Generic;
using VITAP.Data.Managers;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class CorrectDEButton : ButtonExceptionManager
    {
        string U048table = "";

        /// <summary>
        /// Correct or Correct DE button
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="InvQuery"></param>
        /// <param name="POFrmQuery"></param>
        /// <param name="Search"></param>
        /// <param name="pdocno"></param>
        /// <param name="prepCode"></param>
        /// <returns></returns>
        public bool Initialize(EXCEPTION exc, string pdocno, string prepCode)
        {
            SetVariables(exc, prepCode, "CORRECTDE");

            if (exception.ERR_CODE.Right(3) == "230")
            {
                return false;
            }

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(PEGASYSINVOICE InvQuery, PEGASYSPO_FRM POFrmQuery, AddressValuesModel Search, string sNotesType, bool POExists, bool InvExists, bool RRExists)
        {
            NewNotes();

            var cImageID = "";
            switch (exception.ERR_CODE)
            {
                case "P060":
                case "P061":
                case "P005":
                    cImageID = InvQuery.IMAGEID;
                    break;

                case "V216":
                    if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                    {
                        cImageID = InvQuery.IMAGEID;
                    }
                    else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
                    {
                        cImageID = POFrmQuery.IMAGEID;
                    }
                    break;

                default:
                    if (POFrmQuery != null)
                    {
                        cImageID = POFrmQuery.IMAGEID;
                    }
                    
                    break;
            }

            if (!String.IsNullOrWhiteSpace(cImageID))
            {
                string cVendName = "";
                if (String.IsNullOrWhiteSpace(exception.VENDNAME.Trim()))
                {
                    if (!String.IsNullOrWhiteSpace(Search.VENDORNAME))
                    {
                        cVendName = Search.VENDORNAME;
                    }
                }
                else
                {
                    cVendName = exception.VENDNAME.Trim();
                }
            }

            if (exception.ERR_CODE.Right(3) == "230")
            {
                if (exception.ERR_CODE.Left(1) == "P")
                {
                    U048table = "PEGINV";
                }

                CreateExceptionU048(InvQuery, POFrmQuery);
            }
            else if (exception.ERR_CODE == "D062")
            {
                notes.returnVal3 = "D062";
                U048table = notes.returnValZ;
                CreateExceptionU048(InvQuery, POFrmQuery);
            }
            else if (exception.ERR_CODE.Left(1) == "V")
            {
                //generate U048 & blank out the PO.last_status
                if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                {
                    U048table = "PEGINV";
                }
                else if (String.IsNullOrWhiteSpace(exception.PO_ID))
                {
                    U048table = "PEGPO";
                }
                CreateExceptionU048(InvQuery, POFrmQuery);
            }
            else if (exception.ERR_CODE == "U043" || exception.ERR_CODE == "U044")
            {
                ExceptionU043(sNotesType, POExists, InvExists, RRExists);
            }
            else if (exception.ERR_CODE == "U049")
            {
                ExceptionU049(sNotesType);
            }
            else if (exception.ERR_CODE == "P060" || exception.ERR_CODE == "P061" || exception.ERR_CODE == "P005")
            {
                string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");

                UpdatePegasysInvoiceStatusById("MATCHREADY");

                //Disabled because it doesn't update the Oracle tables
                //SET CLASS TO exceptions ADDITIVE
                //objChg = CREATE("InterestCorrectDE")

                //objChg.r_act = THISFORM.r_act

                //objChg.r_rr_id = THISFORM.r_rr_id

                //objChg.r_inv_key_id = THISFORM.r_inv_key_id

                //objChg.r_pegasys = THISFORM.r_pegasys

                //objChg.SHOW
            }
            else
            {
                var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");
            }
        }

        protected void CreateExceptionU048(PEGASYSINVOICE InvQuery, PEGASYSPO_FRM POFrmQuery)
        {
            U048table = U048table.ToUpper();
            var lGoU048 = true;
            if (U048table == "PEGINV")
            {
                //Updating PegasysInvoice ...
                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                var fieldsToUpdate = new List<string>
                {
                    "ERR_CODE"
                };
                rtnInv.ERR_CODE = "U048";
                UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
            }
            else if (U048table == "PEGPO")
            {
                //Updating Pegasyspo_frm ...
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                var fieldsToUpdate = new List<string>
                {
                    "ERR_CODE"
                };

                UpdatePegasysPO(rtnPO, fieldsToUpdate);
            }
            else if (U048table == "PEGRR")
            {
                //Updating Pegasysrr_frm ...
                var rtnRR = GetPegasysRRByKey(exception.RR_ID);
                var fieldsToUpdate = new List<string>
                {
                    "ERR_CODE"
                };

                UpdatePegasysRR(rtnRR, fieldsToUpdate);
            }

            if (lGoU048 == true)
            {
                //Generating U048...
                var objException = new VITAPExceptions();

                objException.ActNum = exception.ACT;

                if (U048table.InList("PEGPO,PO"))
                {
                    objException.Po_id = exception.PO_ID;

                    objException.Updpostatus = "T";
                }

                if (U048table.InList("PEGRR,RR"))
                {
                    objException.Rr_id = exception.RR_ID;

                    objException.Updrrstatus = "T";
                }

                if (U048table.InList("PEGINV,INVOICE"))
                {
                    objException.Inv_key_id = exception.INV_KEY_ID;
                    objException.Updinvstatus = "T";
                }

                if (U048table == "EXPENSEACCRUAL")
                {
                    objException.Ae_id = exception.AE_ID;
                    objException.Updaestatus = "T";
                }

                if (U048table.InList("PEGINV,PEGPO,PEGRR"))
                {
                    objException.Ex_fund = exception.EX_FUND;
                    objException.Vendname = exception.VENDNAME;

                    if (U048table == "PEGINV" && InvQuery.AMOUNT != null)
                    {
                        objException.Poamount = (double)InvQuery.AMOUNT;
                    }
                    else if (U048table == "PEGPO" && POFrmQuery.AMOUNT != null)
                    {
                        objException.Poamount = (double)POFrmQuery.AMOUNT;
                    }
                }

                objException.Ex_memo = notes.returnVal3;
                objException.Err_code = "U048";
                objException.PrepCode = PrepCode;

                objException.AddException();

                var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");
            }
        }

        protected void ExceptionU043(string sNotesType, bool POExists, bool InvExists, bool RRExists)
        {
            var sFaxNotes = notes.returnVal7;
            var sResponseNotes = NewNote + "\r\n";
            var sErr_Response = "C";
            var sAllProcess = "";
            var sEx_Memo2 = notes.returnVal3;
            var NewStatus = "";

            if (POExists)
            {
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);

                if (rtnPO != null)
                {
                    NewStatus = GetPOStatus(rtnPO);

                    var fieldsToUpdate = new List<string>
                    {
                        "PO_STATUS",
                        "ERR_CODE",
                        "VERIFICATION_FL"
                    };

                    rtnPO.PO_STATUS = "VERIFY";
                    rtnPO.ERR_CODE = null;
                    if (!rtnPO.PO_ID.Contains("&") && !String.IsNullOrWhiteSpace(rtnPO.MODNO))
                    {
                        rtnPO.VERIFICATION_FL = "F";
                    }
                    UpdatePegasysPO(rtnPO, fieldsToUpdate);
                }
                else
                {
                    //Create an error log here
                    //INSERT INTO (m.vitapdata) + 'systemerrors'(po_id, err_date, errordesc, errormod) VALUES;
                    //(THISFORM.r_po_id, DATE(),"Could not find PO to set status","correctde.m_u043")
                }
            }
            else if (InvExists)
            {
                if (exception.ERR_CODE == "U044" && sNotesType == "U049CRCT")
                {
                    sEx_Memo2 = "Invoice Number Changed. Send to Manager for Approval";
                    sAllProcess = "Sent to Manager for Approval";

                    // Create a new Exception U049
                    var objException = new VITAPExceptions();

                    objException.ActNum = exception.ACT;
                    objException.Inv_key_id = exception.INV_KEY_ID;
                    objException.Err_code = "U049";

                    objException.Updinvstatus = "T";
                    objException.Pdocnopo = PDocNo;

                    objException.Ex_memo = "Invoice Number changed on U044. To Manager for Approval.";
                    objException.PrepCode = PrepCode;
                    objException.AddException();
                }
                else
                {
                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                    NewStatus = "MATCHREADY";
                    if (rtnInv != null)
                    {
                        NewStatus = GetInvStatus(rtnInv);

                    }

                    //Just in case there is a V299 on Hold
                    var rtnExc = GetExceptionV299();
                    if (rtnExc.Count > 0)
                    {
                        var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                        UpdateException(exception, "H", notes.returnVal7, notes.returnVal3, responsenotes, sAllProcess);

                        var fieldsToUpdate = new List<string>
                        {
                            "INV_STATUS",
                            "LAST_STATUS",
                            "ERR_CODE",
                            "LASTTIME"
                        };

                        rtnInv.INV_STATUS = "EXCEPTION";
                        rtnInv.LAST_STATUS = null;
                        rtnInv.ERR_CODE = "V299";
                        rtnInv.LASTTIME = 0;

                        UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                    }
                    else
                    {
                        var fieldsToUpdate = new List<string>
                        {
                            "INV_STATUS",
                            "LAST_STATUS",
                            "ERR_CODE",
                            "LASTTIME"
                        };

                        rtnInv.INV_STATUS = NewStatus;
                        rtnInv.LAST_STATUS = null;
                        rtnInv.ERR_CODE = null;
                        rtnInv.LASTTIME = 0;

                        UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                    }
                }
            }
            else if (RRExists)
            {
                var rtnRR = GetPegasysRRByKey(exception.RR_ID);

                NewStatus = "MATCHREADY";
                if (rtnRR != null)
                {
                    NewStatus = GetRRStatus(rtnRR);
                }

                var fieldsToUpdate = new List<string>
                {
                    "RR_STATUS",
                    "ERR_CODE"
                };

                rtnRR.RR_STATUS = NewStatus;
                rtnRR.ERR_CODE = null;

                UpdatePegasysRR(rtnRR, fieldsToUpdate);
            }

            UpdateException(exception, sErr_Response, sFaxNotes, sEx_Memo2, sResponseNotes, sAllProcess);
        }

        public void ExceptionU049(string sNotesType)
        {
            NewNotes();

            switch (sNotesType)
            {
                case "CORRECT":
                    //Clear the U049 Exception
                    var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                    UpdateException(exception, "C", notes.returnVal7, "Manager Accepts U044 Invoice Number Change", responsenotes, "Approved by Manager");


                    //Make a Custom Transhist Entry
                    var strCuffMemo = "U044 Invoice Number Change Accepted by Manager.";
                    InsertTranshist(exception, "", strCuffMemo, "Approved by Manager", PrepCode);

                    //Update Invoice table as this invoice is now cleared
                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                    var fieldsToUpdate = new List<string>
                    {
                        "INV_STATUS",
                        "PREVALIDATION_FL",
                        "DATAENTRY_FL",
                        "ERR_CODE"                        
                    };

                    rtnInv.INV_STATUS = "KEYED";
                    rtnInv.PREVALIDATION_FL = "F";
                    rtnInv.DATAENTRY_FL = "T";
                    rtnInv.ERR_CODE = null;

                    UpdatePegasysInvoice(rtnInv, fieldsToUpdate);

                    break;

                case "RESET":
                    //Clear the U049 Exception
                    responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                    UpdateException(exception, "C", notes.returnVal7, "Send back to U044", responsenotes, "");

                    //Make a Custom Transhist Entry
                    strCuffMemo = "Re-establish U044 per Manager.";
                    InsertTranshist(exception, "", strCuffMemo, "Back to U044 per Manager.", PrepCode);

                    //Create U044 Exception
                    VITAPExceptions objException = new VITAPExceptions();

                    objException.ActNum = exception.ACT;
                    objException.Inv_key_id = exception.INV_KEY_ID;
                    objException.Err_code = "U044";
                    objException.Updinvstatus = "T";
                    objException.Pdocnopo = PDocNo;
                    objException.Ex_memo = "Re-establish U044 per Manager.";
                    objException.PrepCode = PrepCode;

                    objException.AddException();

                    break;
            }
        }

        protected string GetPOStatus(PEGASYSPO_FRM rtnPO)
        {
            if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "T" && rtnPO.VALIDATION_FL == "T" && rtnPO.VENDORMATCH_FL == "T" && rtnPO.OFFICEMATCH_FL == "T")
            {
                return "PREOUT";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "T" && rtnPO.VALIDATION_FL == "T" && rtnPO.VENDORMATCH_FL == "T" && rtnPO.OFFICEMATCH_FL == "F")
            {
                return "NEEDOFFC";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "T" && rtnPO.VALIDATION_FL == "F" && rtnPO.VENDORMATCH_FL == "T")
            {
                return "NEEDVAL";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "T" && rtnPO.VALIDATION_FL == "T" && rtnPO.VENDORMATCH_FL == "F")
            {
                return "NEEDVEND";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "T" && rtnPO.VALIDATION_FL == "F" && rtnPO.VENDORMATCH_FL == "F")
            {
                return "VVREADY";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "F" && !String.IsNullOrWhiteSpace(rtnPO.MODNO))
            {
                return "MODREADY";
            }
            else if (rtnPO.DATAENTRY_FL == "T" && rtnPO.PREVALIDATION_FL == "F")
            {
                return "KEYED";
            }
            else if (rtnPO.DATAENTRY_FL == "F" && rtnPO.PREVALIDATION_FL == "F")
            {
                return "KEYREADY";
            }
            return "";
        }

        protected string GetInvStatus(PEGASYSINVOICE rtnInv)
        {
            if (rtnInv.PDOCNOPO.Left(2) == "2I")
            {
                return "PREVAL3GS";
            }
            else if (rtnInv.VERIFICATION_FL == "T" && rtnInv.DATAENTRY_FL == "T" && rtnInv.PREVALIDATION_FL == "T")
            {
                return "MATCHREADY";
            }
            else if (rtnInv.VERIFICATION_FL == "T" && rtnInv.DATAENTRY_FL == "T" && rtnInv.PREVALIDATION_FL == "F")
            {
                return "KEYED";
            }
            else if (rtnInv.VERIFICATION_FL == "T" && rtnInv.DATAENTRY_FL == "F" && rtnInv.PREVALIDATION_FL == "F")
            {
                return "KEYREADY";
            }
            else if (rtnInv.VERIFICATION_FL == "F" && rtnInv.DATAENTRY_FL == "F" && rtnInv.PREVALIDATION_FL == "F")
            {
                return "RE-VERIFY";
            }
            return "MATCHREADY";
        }

        protected string GetRRStatus(PEGASYSRR_FRM rtnRR)
        {
            var NewStatus = "MATCHREADY";
            if (rtnRR.VERIFICATION_FL == "T" && rtnRR.DATAENTRY_FL == "T" && rtnRR.PREVALIDATION_FL == "T")
            {
                NewStatus = "MATCHREADY";
            }
            else if (rtnRR.VERIFICATION_FL == "T" && rtnRR.DATAENTRY_FL == "T" && rtnRR.PREVALIDATION_FL == "F")
            {
                NewStatus = "KEYED";
            }
            else if (rtnRR.VERIFICATION_FL == "T" && rtnRR.DATAENTRY_FL == "F" && rtnRR.PREVALIDATION_FL == "F")
            {
                NewStatus = "KEYREADY";
            }
            else if (rtnRR.VERIFICATION_FL == "F" && rtnRR.DATAENTRY_FL == "F" && rtnRR.PREVALIDATION_FL == "F")
            {
                NewStatus = "NEW";
            }

            return NewStatus;
        }
    }
}
