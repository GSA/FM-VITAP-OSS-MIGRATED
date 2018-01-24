using System;
using System.Collections.Generic;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class POMatchButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string newPDocNo, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "POMATCH");
            PDocNo = newPDocNo;

            ButtonPushed = "NONE";

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(string NewAct, string NewPDocNo, string Vcpo, DateTime? StartDate, PEGASYSPO_FRM vpo)
        {
            //string Act = "";
            bool bChangeAct = false, bChangePdocno = false;
            if (String.IsNullOrWhiteSpace(exception.ACT))
            {
                bChangeAct = true;
                //Act = "";
            }
            else
            {
                if (exception.ACT != NewAct)
                {
                    bChangeAct = true;
                }
            }

            if (String.IsNullOrWhiteSpace(PDocNo))
            {
                bChangePdocno = true;
                PDocNo = "";
            }
            else
            {
                if (PDocNo != NewPDocNo)
                {
                    bChangePdocno = true;
                }
            }

            UpdateExceptionNewValues(bChangeAct, bChangePdocno, NewAct, NewPDocNo);

            if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                UpdatePOFrmData(NewPDocNo, NewAct, Vcpo, vpo);
            }

            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                UpdateInvData(NewAct, NewPDocNo, StartDate);
            }
        }

        private void UpdateExceptionNewValues(bool bChangeAct, bool bChangePdocno, string newAct, string newPDocNo)
        {
            var ChangeField = "";
            if (bChangeAct && bChangePdocno)
            {
                ChangeField = "ACT changed from " + exception.ACT + " to " + newAct + "/r/n" + "PDOCNO changed from " + PDocNo + " to " + newPDocNo;
                PDocNo = newPDocNo;
            }
            else if (bChangeAct)
            {
                ChangeField = "ACT changed from " + exception.ACT + " to " + newAct;
            }
            else if (bChangePdocno)
            {
                ChangeField = "PDOCNO changed from " + PDocNo + " to " + newPDocNo.Trim();
                PDocNo = newPDocNo.Trim();
            }

            //responsenotes information more than 1000 characters will not update the exceptions table.
            NewNote = NewNote + "\r" + ChangeField + "\r" + exception.RESPONSENOTES.ReplaceNull("").Trim();

            if (NewNote.Length > 1000)
            {
                NewNote = NewNote.Left(999);
            }

            UpdateException(exception, "C", notes.returnVal7, notes.returnVal3, NewNote, "");
        }

        private void UpdatePOFrmData(string newPDocNo, string newAct, string Vcpo, PEGASYSPO_FRM vpo)
        {
            //Update PO Frm data
            var PO_Status = "MODREADY";
            if (Status == "VITAP")
            {
                PO_Status = "WAITONPO";
            }
            var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);

            string modNo = "";
            if (vpo != null)
            {
                modNo = vpo.MODNO;
            }

            var newPoId = newPDocNo.Trim() + "&" + modNo;

            var fieldsToUpdate = new List<string>
                {
                    "ACT",
                    "PO_ID",
                    "PDONO",
                    "ERR_CODE",
                    "PO_STATUS",
                    "DATAENTRY_FL",
                    "PREVALIDATION_FL",
                    "VCPO"
                };

            if (rtnPO != null)
            {
                rtnPO.ACT = newAct;
                rtnPO.PO_ID = newPoId;
                rtnPO.PDOCNO = newPDocNo;
                rtnPO.ERR_CODE = null;
                rtnPO.PO_STATUS = PO_Status;
                rtnPO.DATAENTRY_FL = "T";
                rtnPO.PREVALIDATION_FL = "F";
                rtnPO.VCPO = Vcpo;
                UpdatePegasysPO(rtnPO, fieldsToUpdate);
            }
            

            //Update Accounting Line data
            var rtnPOAcct = GetPegasysPOAcctsFrmByKey(exception.PO_ID);

            fieldsToUpdate = new List<string>
                {
                    "PO_ID",
                    "PO_ACCT_ID"
                };

            foreach (var row in rtnPOAcct)
            {
                row.PO_ID = newPoId;
                row.PO_ACCT_ID = newPoId + row.PO_ACCT_ID.Substring(row.PO_ACCT_ID.IndexOf('&'));
                UpdatePegasysPOAcctFrm(row, fieldsToUpdate);
            }

            //Update Office data
            var rtnPOOffc = GetPegasysPOOffcsByKey(exception.PO_ID);

            fieldsToUpdate = new List<string>
                {
                    "PO_ID",
                    "PO_OFFC_ID"
                };

            foreach (var row in rtnPOOffc)
            {
                row.PO_ID = newPoId;
                row.PO_OFFC_ID = newPoId + row.PO_OFFC_ID.Substring(row.PO_OFFC_ID.IndexOf('&'));
                UpdatePegasysPOOffc(row, fieldsToUpdate);
            }

            //Update Transhist data
            UpdateTranshistByPO(exception.PO_ID, newPoId, newAct);

            //Update Exception data
            var rtnExc = GetExceptionsByPOID(exception.PO_ID);
            fieldsToUpdate = new List<string>
                {
                    "PO_ID",
                    "PDOCNO",
                    "ACT"
                };

            foreach (var row in rtnExc)
            {
                row.PO_ID = newPoId;
                row.PDOCNO = newPoId;
                row.ACT = newAct;
                UpdateException(row, fieldsToUpdate);
            }

            //Update Exception History data
            UpdateExceptionHistByPO(exception.PO_ID, newPoId, newAct);

            //Update Notification data
            var rtnNot = GetNotificationsByPOID(exception.PO_ID);
            fieldsToUpdate = new List<string>
                {
                    "PO_ID",
                    "ACT"
                };

            foreach (var row in rtnNot)
            {
                row.PO_ID = newPoId;
                row.ACT = newAct;
                UpdateNotification(row, fieldsToUpdate);
            }

            var PO_ID = newPoId;
        }

        public void UpdateInvData(string newAct, string newPDocNo, DateTime? StartDate)
        {
            var Inv_Status = "RE-VERIFY";
            if (Status == "VITAP")
            {
                Inv_Status = "WAITONPO";
            }

            var rtnInv = UpdateInvoiceRecord(newAct, newPDocNo, Inv_Status);

            if (newPDocNo.Left(2) == "RO" && exception.ERR_CODE == "R200" && StartDate != null)
            {
                AddR200TranshistRecord(StartDate, newAct, newPDocNo, rtnInv);
            }
            else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
            {
                UpdateRRRecord(newAct, newPDocNo);
            }
            else if (!String.IsNullOrWhiteSpace(exception.AE_ID))
            {
                UpdateAERecord(newAct, newPDocNo);
            }
        }

        private PEGASYSINVOICE UpdateInvoiceRecord(string newAct, string newPDocNo, string Inv_Status)
        {
            //Update PegasysInvoice table
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            if (rtnInv != null)
            {
                var fieldsToUpdate = new List<string>
                {
                    "ACT",
                    "PDOCNOPO",
                    "INV_STATUS",
                    "VERIFICATION_FL",
                    "PREVALIDATION_FL",
                    "ERR_CODE",
                    "PODTYP"
                };

                rtnInv.ACT = newAct;
                rtnInv.PDOCNOPO = newPDocNo;
                rtnInv.INV_STATUS = Inv_Status;
                rtnInv.VERIFICATION_FL = "F";
                rtnInv.PREVALIDATION_FL = "F";
                rtnInv.ERR_CODE = null;
                rtnInv.PODTYP = newPDocNo.Left(2);

                UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
            }
            
            return rtnInv;
        }

        private void AddR200TranshistRecord(DateTime? StartDate, string newAct, string newPDocNo, PEGASYSINVOICE rtnInv)
        {
            if (rtnInv == null)
            {
                return;
            }

            DateTime? EndDate;

            if (((DateTime)StartDate).Month + 1 == 13)
            {
                EndDate = (DateTime?)Convert.ToDateTime("12/31/" + ((DateTime)StartDate).Year.ToString());
            }
            else
            {
                string date = ((DateTime)StartDate).Month.ToString().PadLeft(2, '0') + "/01/" + ((DateTime)StartDate).Year.ToString();
                EndDate = (DateTime?)Convert.ToDateTime(date);
            }

            if (rtnInv.EDI_IND == "T")
            {
                exception.ACT = newAct;
                exception.PDOCNO = newPDocNo;
                string strCuffMemo = "R200 PO Match changed the service period on the " +
                    "electronic invoice from " + rtnInv.SVC_PERD_STRT.ToString() + "-" + rtnInv.SVC_PERD_END.ToString() +
                    " to " + StartDate.ShortDate() + "-" + EndDate.ShortDate() + "'";
                InsertTranshist(exception, "", strCuffMemo, "", PrepCode);

            }

            var fieldsToUpdate = new List<string>
                    {
                        "SVC_PERD_STRT",
                        "SVC_PERD_END"
                    };

            rtnInv.SVC_PERD_STRT = StartDate;
            rtnInv.SVC_PERD_END = EndDate;

            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);        
        }

        private void UpdateRRRecord(string newAct, string newPDocNo)
        {
            //Update RR Frm data
            var RR_Status = "RE-VERIFY";
            if (Status == "VITAP")
            {
                RR_Status = "WAITONPO";
            }

            var rtnRR = GetPegasysRRByKey(exception.RR_ID);

            if (rtnRR != null)
            {
                var fieldsToUpdate = new List<string>
                    {
                        "ACT",
                        "PDOCNOPO",
                        "RR_STATUS",
                        "VERIFICATION_FL",
                        "PREVALIDATION_FL",
                        "ERR_CODE"
                    };

                rtnRR.ACT = newAct;
                rtnRR.PDOCNOPO = newPDocNo;
                rtnRR.RR_STATUS = RR_Status;
                rtnRR.VERIFICATION_FL = "F";
                rtnRR.PREVALIDATION_FL = "F";
                rtnRR.ERR_CODE = null;

                UpdatePegasysRR(rtnRR, fieldsToUpdate);
            }      
        }

        private void UpdateAERecord(string newAct, string newPDocNo)
        {
            //Update AE Frm data
            var AE_Status = "RE-VERIFY";
            if (Status == "VITAP")
            {
                AE_Status = "WAITONPO";
            }

            var rtnAE = GetPegasysAEByKey(exception.AE_ID);

            if (rtnAE != null)
            {
                var fieldsToUpdate = new List<string>
                    {
                        "ACT",
                        "PDOCNOPO",
                        "AE_STATUS",
                        "VERIFICATION_FL",
                        "PREVALIDATION_FL",
                        "ERR_CODE"
                    };

                rtnAE.ACT = newAct;
                rtnAE.PDOCNOPO = newPDocNo;
                rtnAE.AE_STATUS = AE_Status;
                rtnAE.VERIFICATION_FL = "F";
                rtnAE.PREVALIDATION_FL = "F";
                rtnAE.ERR_CODE = null;

                UpdatePegasysAE(rtnAE, fieldsToUpdate);
            }
        }
    }
}