using System.Collections.Generic;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class UnMatchButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            SetVariables(exception, prepCode, "RECYCLE");

            return true;

        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(string exId, string invKeyId)
        {
            exception = GetExceptionByKeyId(exId);

            if (exception != null && !string.IsNullOrWhiteSpace(exception.ERR_CODE))
            {
                if (exception.ERR_CODE.Left(1) == "K")
                {
                    var InvQuery = GetPegasysInvoiceByKey(invKeyId);
                    var poId = exception.PO_ID;
                    if (string.IsNullOrWhiteSpace(poId))
                    {
                        poId = InvQuery.PDOCNOPO.Trim();
                    }
                    notes.returnVal5 = InvQuery.VEND_CD + " " + InvQuery.VEND_ADDR_CD;

                    switch (notes.returnVal1)
                    { 
                        case "CANCEL":
                            Close();
                            break;
                        case "BACK":
                            // do nothing
                            break;
                        case "FINISH":
                            NewNote = "UnMatch - " + notes.returnVal3;

                            if (notes.returnVal5.Trim().Length > 9)
                            {
                                notes.returnVal5 = notes.returnVal5.Trim().Left(9);
                            }
                            exception.PARTIAL_MATCH_VENDNO = notes.returnVal5.Trim();
                            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote.Trim();
                            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");

                            // Update pegasysinvoice table
                            UpdatePegasysInvoiceStatusById("MATCHREADY");

                            // Delete records from matchrrinv and multrr for the inv_key_id
                            // Build MATCHRRINV
                            var rtnMatchRR = GetMatchRRByInvKey(exception.INV_KEY_ID);
                            DeleteMatchRRInv(rtnMatchRR);

                            var rtnMultRR = GetMultRRByInvKey(exception.INV_KEY_ID);
                            DeleteMultRR(rtnMultRR);

                            // Update P039 and P024 exceptions in both exceptions and exceptionhist tables
                            UpdateExceptionP039andP024();

                            break;
                    }
                }
                else
                {
                    NewNote = "UnMatch - " + NewNote;

                    var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                    UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes, "");

                    var fieldsToUpdate = new List<string>
                {
                    "PARTIAL_MATCH_VENDNO"
                };

                    exception.PARTIAL_MATCH_VENDNO = notes.returnVal5.Trim().Left(9);

                    UpdateException(exception, fieldsToUpdate);

                    //Update pegasysinvoice table
                    UpdatePegasysInvoiceStatusById("MATCHREADY");

                    //Delete records from matchrrinv and multrr for the inv_key_id

                    //Build MATCHRRINV
                    var rtnMatchRRInv = GetMatchRRByInvKey(exception.INV_KEY_ID);
                    DeleteMatchRRInv(rtnMatchRRInv);

                    //Build MULTRR
                    var rtnMultRR = GetMultRRByInvKey(exception.INV_KEY_ID);
                    DeleteMultRR(rtnMultRR);
                    //DELETE FROM multrr WHERE inv_key_id = THISFORM.r_inv_key_id

                    //Update P039 and P024 exceptions in both exceptions and exceptionhist tables
                    UpdateExceptionP039andP024();

                    //Add to TRANSHIST - It currently does not add this
                    Close();
                }
            }
        }
    }
}