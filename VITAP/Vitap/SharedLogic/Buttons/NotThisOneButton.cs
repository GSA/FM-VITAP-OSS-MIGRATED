using System.Collections.Generic;
using VITAP.Data.PegasysEntities;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Managers;

namespace VITAP.SharedLogic.Buttons
{
    public class NotThisOneButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "NOTTHISONE");

            return true;
        }
        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(List<MF_IC> TheQueue, List<RRCHOICE> RRChoice)
        {
            NewNotes();

            //Remember - the exception is already current upon entering the finishcode.
            switch (exception.ERR_CODE)
            {
                case "P023":
                case "P002":
                case "P202":

                    UpdatePegasysInvoiceStatusById("MATCHREADY");
                    var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" + "This is NOT a matching RR and Invoice - Dont Pay!";
                    UpdateException(exception, "Q", notes.returnVal7, "", responsenotes, "");

                    InsertMatchRRInv(exception.ACT, exception.RR_ID, exception.INV_KEY_ID);

                    //if P039 was previously linked to this RR then Q it so it will find a new match.
                    UpdateExceptionHistByErrCode(exception.INV_KEY_ID, exception.RR_ID, "P039");
                    break;

                case "P039":
                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                    UpdatePegasysInvoiceStatusById("MATCHREADY");

                    responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" + "This invoice does not match any listed RRs";
                    UpdateException(exception, "Q", notes.returnVal7, "", responsenotes, "");

                    if (TheQueue != null && TheQueue.Count > 0)
                    {
                        foreach (var row in TheQueue)
                        {
                            InsertMatchRRInv(exception.ACT, row.DOC_NUM, exception.INV_KEY_ID);
                        }
                    }

                    var mgr = new P039Manager();
                    mgr.P039NotThisOne(exception.INV_KEY_ID, PrepCode, exception.ACT, exception.EX_ID);


                    break;

                case "P140":
                    UpdatePegasysInvoiceStatusById("MATCHREADY");

                    responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote + "\r" + "This invoice does not match any listed RRs";
                    UpdateException(exception, "Q", notes.returnVal7, NewNote, responsenotes, "");

                    if (RRChoice.Count > 0)
                    {
                        foreach (var row in RRChoice)
                        {
                            InsertMatchRRInv(exception.ACT, row.RR_ID, exception.INV_KEY_ID);
                        }
                    }
                    break;
            }

            //This is disabled...Not doing the immediate back end processing anymore
            //int A = DisplayMessage("Do you want to run Payment/Matching Diagram Immediately?", 36, "Run Backend Processes");
            //if (A == 6)
            //{
            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW


            //    objBackend.r_this_inv_key_id = THISFORM.r_inv_key_id

            //    IF THISFORM.r_pegasys

            //        objBackend.m_matchingDiagram
            //    ELSE

            //        SET CLASS TO finlib ADDITIVE
            //        objPayment = CREATE("NEARTransaction")

            //        objBackend.m_PaymentDiagram
            //    ENDIF

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //}
        }
    }
}