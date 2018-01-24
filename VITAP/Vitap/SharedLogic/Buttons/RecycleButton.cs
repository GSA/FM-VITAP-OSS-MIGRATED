using System.Collections.Generic;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class RecycleButton : ButtonExceptionManager
    {

        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            SetVariables(exc, prepCode, "RECYCLE");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public List<MATCHRRINV> FinishCode1()
        {
            //FOR THE NOTIFICATION EXCEPTIONS ONLY!!!
            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "Recycle");

            if (exception.ERR_CODE.Left(1) == "P")
            {
                //To set the inv_status based on the flags.
                //Uses exception inv_key_id instead of screen thisform.r_inv_key_id value
                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                if (rtnInv != null)
                {
                    var InvStatus = "RE-VERIFY";

                    if (rtnInv.PREVALIDATION_FL.ReplaceNull("F") == "T")
                    {
                        InvStatus = "MATCHREADY";
                    }
                    else if (rtnInv.DATAENTRY_FL.ReplaceNull("F") == "T")
                    {
                        InvStatus = "KEYED";
                    }
                    else if (rtnInv.VERIFICATION_FL.ReplaceNull("F") == "T")
                    {
                        if (rtnInv.EDI_IND.ReplaceNull("F") == "T")
                        {
                            InvStatus = "KEYED";
                        }
                        else
                        {
                            InvStatus = "KEYREADY";
                        }
                    }

                    var fieldsToUpdate = new List<string>
                    {
                        "INV_STATUS",
                        "LASTTIME",
                        "ERR_CODE"
                    };

                    //Used screen value thisform.r_err_code
                    if (exception.ERR_CODE != "P041")
                    {
                        rtnInv.INV_STATUS = InvStatus;
                        rtnInv.LASTTIME = 0;
                        rtnInv.ERR_CODE = null;

                    }
                    else
                    {
                        fieldsToUpdate = new List<string>
                    {
                        "INV_STATUS",
                        "VERIFICATION_FL",
                        "LASTTIME",
                        "ERR_CODE"
                    };

                        rtnInv.INV_STATUS = "RE-VERIFY";
                        rtnInv.VERIFICATION_FL = "F";
                        rtnInv.LASTTIME = 0;
                        rtnInv.ERR_CODE = null;

                    }
                    UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                }
            }

            if (exception.ERR_CODE == "P040")
            {
                //Used thisform.r_inv_key_id before, now using exception.INV_KEY_ID
                var rtnMatchRR = GetMatchRRByInvKey(exception.INV_KEY_ID);

                if (rtnMatchRR.Count > 0)
                {
                    return rtnMatchRR;
                }
            }
            return null;
        }

        public void DeleteRRInv(List<MATCHRRINV> rtnMatchRR)
        {
            if (rtnMatchRR.Count > 0)
            {
                DeleteMatchRRInv(rtnMatchRR);
            }
        }

        public void FinishCode2()
        {
            DeleteNotificationByInvKeyIDandExID(exception.INV_KEY_ID, exception.EX_ID);

            //No longer running back end job
            //Should not run, Payment Diagram for P041 Recycle. Since the Document has just been
            //matched to the PO
            //if (exception.ERR_CODE.Left(1) == "P" && exception.ERR_CODE != "P041")
            //{
            //    int Ans = DisplayMessage("Do you want to run Payment/Matching Diagram Immediately?", 36, "Run Backend Processes");
            // if (Ans == 6)
            //    { 
            //        PUBLIC objBackend, objPayment

            //        SET CLASSLIB TO backend ADDITIVE
            //        objBackend = CREATE("finbackend")

            //        objBackend.SHOW

            //        IF AT("FINLIB", SET("CLASSLIB")) = 0

            //            SET CLASS TO finlib ADDITIVE
            //        ENDIF

            //        objPayment = CREATE("NEARTransaction")

            //        objBackend.r_this_inv_key_id = THISFORM.r_inv_key_id

            //        objBackend.m_matchingDiagram

            //        DisplayMessage("Finished running backend diagram!")

            //        RELE objPayment, objBackend

            //    ENDIF
            //}

            notes.returnVal1 = "RECYCLE";
        }
    }
}