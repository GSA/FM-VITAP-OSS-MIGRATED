using System;
using System.Collections.Generic;
using VITAP.Data.Managers;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class POModButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            SetVariables(exc, prepCode, "POMOD");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public bool FinishCode(string Invoice, double InvAmount, double POAmount, string VendName, String PrepCode)
        {
            var TheResponse = "Q";
            if (exception.ERR_CODE == "U084")
            {
                TheResponse = "B";
            }

            Report_ID = "F01";
            ReportForm = "POMod Request";

            var responsenotes = NewNote.ReplaceApostrophes();
            UpdateException(exception, TheResponse, notes.returnVal7, notes.returnVal3, exception.RESPONSENOTES + "\r\rn" + NewNote.ReplaceApostrophes(), "");

            switch (exception.ERR_CODE)
            {
                case "P001":
                case "P024":
                    break;

                case "P201":
                    Status = "Pending";
                    ExceptionP201();
                    break;
            }

            var objException = new VITAPExceptions();

            objException.ActNum = exception.ACT;
            objException.Po_id = exception.PO_ID;
            objException.Rr_id = exception.RR_ID;
            objException.Ae_id = exception.AE_ID;
            objException.Err_response = "M";
            objException.Inv_key_id = exception.INV_KEY_ID;
            objException.PrepCode = PrepCode;

            if (exception.ERR_CODE == "D062" || exception.ERR_CODE == "P201")
            {
                objException.Err_code = "P042";
                objException.Updinvstatus = "T";
            }
            else if (exception.ERR_CODE == "V299" && !String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                objException.Err_code = "P042";
                objException.Updinvstatus = "T";
            }
            else if (exception.ERR_CODE == "V299" && !String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                objException.Err_code = "O042";
                objException.Updpostatus = "T";
            }
            else
            {
                objException.Err_code = exception.ERR_CODE.Left(1) + "042";

                switch (exception.ERR_CODE.Left(1))
                {
                    case "P":
                        objException.Updinvstatus = "T";
                        break;
                }
            }
            objException.Ex_memo = "Missing Modification";
            if (exception.ERR_CODE == "P201")
            {                
                objException.Ex_memo = "Mod needed for additional goods: invoice " + 
                Invoice + " / invoice amount " + InvAmount + " / PO amount " + 
                POAmount + " / Vend Name " + VendName.ReplaceApostrophes();
            }

            objException.Updstatus = "T";
            if (!String.IsNullOrWhiteSpace(VendName))
            {
                objException.Vendname = VendName;
            }

            objException.AddException();
            var NewEx_id = objException.Ex_id;

            if (!String.IsNullOrWhiteSpace(NewEx_id))
            {
                //Send PO MOD request notificaton
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        public void ExceptionP201()
        {
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            var fieldsToUpdate = new List<string>
            {
                "ERR_CODE",
                "PREPCODE"
            };
            rtnInv.ERR_CODE = "P042";
            rtnInv.PREPCODE = PrepCode;
            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
        }
    }
}