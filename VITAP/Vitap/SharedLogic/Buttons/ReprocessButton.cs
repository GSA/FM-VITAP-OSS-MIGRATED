using System;
using System.Collections.Generic;
using VITAP.Controllers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers;

namespace VITAP.SharedLogic.Buttons
{
    public class ReprocessButton : ButtonExceptionManager
    {
        public string Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "REPROCESS");

            if (!Check_P_Exceptions()) { return theMsg; }

            return "";
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public bool ReprocessPegInvoice()
        {
            //Need to get InvQuery.PDocnopo from the screen
            //PDocNo = String.IsNullOrWhiteSpace(PDocNo) ? InvQuery.PDOCNOPO : PDocNo;
            var CheckReprocess = false;

            switch (PDocNo.Left(2))
            {
                case "RO":
                    CheckReprocess = true;
                    break;

                case "1B":
                    var rtnPO = GetPegasysPOByPDocNo("T");

                    if (rtnPO != null)
                    {
                        CheckReprocess = true;
                    }
                    break;

                default:
                    CheckReprocess = false;
                    break;
            }

            if (CheckReprocess)
            {
                return true;
            }
            return false;
        }

        public void ReprocessSetStatus(string NewStatus)
        {
            UpdatePegasysInvoiceStatusById(NewStatus);
        }

        public void FinishCode()
        {
            NewNotes();
            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, exception.RESPONSENOTES, notes.returnVal2);
            new ExceptionsManager().CheckinException(exception.EX_ID);
        }
    }
}
