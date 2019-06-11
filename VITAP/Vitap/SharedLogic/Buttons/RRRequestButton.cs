using System;
using System.Collections.Generic;
using VITAP.Data.Managers;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models;

namespace VITAP.SharedLogic.Buttons
{
    public class RRRequestButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exception, string prepCode)
        {
            SetVariables(exception, prepCode, "RRREQUEST");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(List<NoRRArray> noRRArray)
        {
            //set err_response to Q because the P040 will have the M err_response***
            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");

            if (exception.RR_ID == "MRR")
            {
                if (noRRArray != null && noRRArray.Count > 0)
                {
                    foreach (var row in noRRArray)
                    {
                        InsertMatchRRInv(exception.ACT, row.DOC_NUM, exception.INV_KEY_ID);
                    }
                }
            }
            else
            {
                InsertMatchRRInv(exception.ACT, exception.RR_ID, exception.INV_KEY_ID);
            }
            var objException = new VITAPExceptions();
            objException.ActNum = exception.ACT;
            objException.Po_id = exception.PO_ID;
            objException.Inv_key_id = exception.INV_KEY_ID;
            objException.Err_code = "P040";
            objException.Err_response = "M";

            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            
            if (rtnInv != null)
            {
                objException.Vendname = rtnInv.VENDNAME;
                objException.Poamount = (double)rtnInv.AMOUNT;

                objException.Ex_memo = "No RR in VITAP - Invoice " + rtnInv.INVOICE +
                        "/$" + Math.Round((double)rtnInv.AMOUNT, 2).ToString();
            }
            else
            {
                objException.Ex_memo = "No RR in VITAP";
            }

            if (exception.ERR_CODE.Left(1) == "P")
            {
                objException.Updinvstatus = "T";
            }
            objException.Updstatus = "T";
            objException.PrepCode = PrepCode;
            objException.AddException();
            string NewEx_id = objException.Ex_id;

            //generate RR request F04
            var excList = GetExceptionByInvKeyID(exception.INV_KEY_ID);
            foreach (var row in excList)
            {
                if (row.EX_ID == NewEx_id && row.ERR_CODE == "P040")
                {
                    exception = row;
                    break;
                }
            }
            var Act = exception.ACT;
            
            var Inv_key_id = exception.INV_KEY_ID;
            var Po_id = exception.PO_ID;
            var DateQueued = DateTime.Now;
            Report_ID = "F04";
            ReportForm = "RR Request";
            if (rtnInv != null)
            {
                Amount = (decimal)rtnInv.AMOUNT;
            }
            var FaxNotes = notes.returnVal7;
            Status = "Tomorrow";

            InsertNotification();
        }
    }
}