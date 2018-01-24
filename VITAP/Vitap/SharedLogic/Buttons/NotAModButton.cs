using System.Collections.Generic;
using VITAP.Data;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class NotAModButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "NOTAMOD");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public int FinishCode1()
        {
            var rtnPOList = GetPegasysPOByPDocNo(PDocNo);
            return rtnPOList.Count;
        }
        public void FinishCode2(string Edi_Ind)
        {
            var rtnPOList = GetPegasysPOByPDocNo(PDocNo);

            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "Q", notes.returnVal7, exception.EX_MEMO2, responsenotes, "");

            //Uses the Edi_Ind set on the form (thisform.r_edi_ind)
            if (Edi_Ind == "T")
            {
                //Uses the PO_ID/Pdocno set on the form (thisform.r_po_id/thisform.r_pdocno)
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                if (rtnPO != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "PO_STATUS",
                        "DATAENTRY_FL",
                        "ERR_CODE",
                        "MODNO"
                    };

                    rtnPO.PO_STATUS = "KEYED";
                    rtnPO.DATAENTRY_FL = "T";
                    rtnPO.ERR_CODE = null;
                    rtnPO.MODNO = null;

                    UpdatePegasysPO(rtnPO, fieldsToUpdate);
                    UpdatePegasysPoId(rtnPO.PO_ID, PDocNo);
                }
                //Uses the PO_ID/Pdocno set on the form (thisform.r_po_id/thisform.r_pdocno)
                var rtnPOAcct = GetPegasysPOAcctByKey(exception.PO_ID, exception.PARTIAL_MATCH_VENDNO);
                if (rtnPOAcct != null)
                {
                    UpdatePegasysPOAcctFrmPoId(PDocNo, rtnPOAcct.PO_ACCT_ID, PDocNo + rtnPOAcct.LNUM);
                }
            }
            else
            {
                //Uses the PO_ID/Pdocno set on the form (thisform.r_po_id/thisform.r_pdocno)
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                if (rtnPO != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "PO_STATUS",
                        "DATAENTRY_FL",
                        "ERR_CODE",
                        "MODNO"
                    };

                    rtnPO.PO_STATUS = "KEYREADY";
                    rtnPO.DATAENTRY_FL = "F";
                    rtnPO.ERR_CODE = null;
                    rtnPO.MODNO = null;

                    UpdatePegasysPO(rtnPO, fieldsToUpdate);
                    UpdatePegasysPoId(rtnPO.PO_ID, PDocNo);
                }
            }
        }
    }
}