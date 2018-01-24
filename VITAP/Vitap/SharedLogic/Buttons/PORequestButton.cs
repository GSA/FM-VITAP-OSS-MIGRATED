using System;
using System.Collections.Generic;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class PORequestButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode, List<X200Model> X200)
        {
            SetVariables(exc, Pdocno, prepCode, "POMATCH");
            PDocNo = X200[0].PDOCNO;

            ButtonPushed = "NONE";

            return true;
        }

        public bool Initialize(EXCEPTION exc, string prepCode, string pDocNo)
        {
            SetVariables(exc, pDocNo, prepCode, "POMATCH");
            ButtonPushed = "NONE";

            return true;
        }

        public void SetNotes(NotesViewModel vmNotes)
        {
            SetNotesValues(vmNotes);
        }

        public void FinishCode(string prepCode)
        {
            if (exception.ERR_CODE.Right(3) == "200")
            {
                Report_ID = "P02";
                ReportForm = "RequestPO";
                Status = "Pending";
                notes.returnVal7 = notes.returnVal7.ReplaceNull("").ReplaceApostrophes();
                exception.PREPCODE = prepCode;
                string responsenotes = exception.RESPONSENOTES + "\rPO requested" + "\r" + NewNote.ReplaceApostrophes();
                UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3.ReplaceNull(""), responsenotes, "");

                var objException = new VITAPExceptions();
                objException.ActNum = exception.ACT;
                objException.Po_id = exception.PO_ID;
                objException.Pdocnopo = exception.PDOCNO;
                objException.Rr_id = exception.RR_ID;
                objException.Ae_id = exception.AE_ID;
                objException.Err_response = "M";
                objException.PrepCode = prepCode;

                objException.Inv_key_id = exception.INV_KEY_ID;
                //To avoid creating R041 Exceptions..

                if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                {
                    objException.Err_code = "P041";

                    if (exception.ERR_CODE != "R200")
                    {
                        objException.Err_code = exception.ERR_CODE.Left(1) + "041";
                    }

                    objException.Ex_memo = "No PO in VITAP";

                    objException.Updinvstatus = "T";

                    if (!String.IsNullOrWhiteSpace(exception.VENDNAME))
                    {
                        objException.Vendname = exception.VENDNAME;
                    }

                    objException.AddException();

                    string newExId = "";

                    if (String.IsNullOrWhiteSpace(newExId))
                    {
                        var rtnExc = GetExceptionByInvKeyID(exception.INV_KEY_ID);
                        string exId = "";
                        foreach (var row in rtnExc)
                        {
                            if (row.ERR_CODE.Right(3) == "041")
                            {
                                exId = row.EX_ID;
                                break;
                            }
                        }
                        if (String.IsNullOrWhiteSpace(exId))
                        {
                            exId = newExId;
                        }
                    }
                }
                else
                {                    
                    Report_ID = "F02";
                    ReportForm = "poreq";
                }

                if (!CheckNotificationExists())
                {
                    var Address = GetAddressFromInvoice(exception.INV_KEY_ID);
                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                    RequestNo = "";
                    Contact_ID = "";
                    Amount = rtnInv.AMOUNT.Value;
                    var Search = Address;
                    InsertNotification();
                }
            }
        }
    }
}