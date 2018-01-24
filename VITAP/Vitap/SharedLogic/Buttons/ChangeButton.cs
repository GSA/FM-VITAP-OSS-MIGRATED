using System;
using System.Collections.Generic;
using VITAP.Controllers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class ChangeButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exception, string pdocno, string prepCode)
        {
            ButtonPushed = "NONE";
            SetVariables(exception, pdocno, prepCode, "NONE");
            if (!r_Pegasys && exception.ERR_CODE.Left(1) == "C")
            {
                //Need to figure out how to run this functionality, or if it is needed
                //objapp.m_append("validationchange", "exceptions", 1, false, Act, Mdl, exception.PO_ID);
            }
            else
            {
                bool bResult = ValidateGrid();

                if (bResult == false)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public bool FinishCode(PEGASYSPO_FRM POFrmQuery)
        {
            //Moved the cmdChange.click code to
            //the Click event of the cmdExceptionBase
            var ResponseNotes = NewNote;

            UpdateException(exception, "C", notes.returnVal7, notes.returnVal3, exception.RESPONSENOTES + "\r\n" + NewNote, "");

            // Not necessary, per Ron Sele's findings.
            //var success = UpdatePegasysPOToKeyed(exception.PO_ID);
            //if (!success) return false;

            if (exception.ERR_CODE.Right(3) == "200")
            {
                ExceptionU200(exception, POFrmQuery);
            }

            return true;
        }

        protected void ExceptionU200(EXCEPTION exception, PEGASYSPO_FRM POFrmQuery)
        {
            //Need to get the NewAct from the screen
            var strNewAct = "";
            var strNewPdocno = "";
            var ChangeField = "";
            var Change_Act = false;
            var Change_Pdocno = false;
            if (String.IsNullOrWhiteSpace(exception.ACT))
            {
                Change_Act = true;
            }
            else
            {
                if (exception.ACT != strNewAct)
                {
                    Change_Act = true;
                }
            }
            if (String.IsNullOrWhiteSpace(exception.PDOCNO))
            {
                Change_Pdocno = true;
                PDocNo = "";
            }
            else
            {
                if (exception.PDOCNO != strNewPdocno)
                {
                    Change_Pdocno = true;
                }
            }

            ChangeField = "";
            if (Change_Act && Change_Pdocno)
            {
                ChangeField = "ACT changed from " + exception.ACT + " to " + strNewAct + "\r\n" +
                    "PDOCNO changed from " + exception.PDOCNO + " to " + strNewPdocno;

                exception.PDOCNO = strNewPdocno;
            }
            else
            {
                if (Change_Act)
                {
                    ChangeField = "ACT changed from " + exception.ACT + " to " + strNewAct;
                }

                if (Change_Pdocno)
                {
                    ChangeField = "PDOCNO changed from " + exception.PDOCNO + " to " + strNewPdocno;
                    exception.PDOCNO = strNewPdocno;
                }
            }

            if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                string strNewPO_ID = strNewPdocno + "&" + POFrmQuery.MODNO.Trim();
                UpdatePegasysPOToModReady(exception.PO_ID, strNewPdocno, strNewPO_ID, strNewAct);
                UpdatePegasysPOAcctFrm(exception.PO_ID, strNewPdocno);
                UpdatePegasysPOOffcFrm(exception.PO_ID, strNewPO_ID);
                UpdateTranshistByPO(exception.PO_ID, strNewPO_ID, strNewAct);
                UpdateExceptionByPO(exception.PO_ID, strNewPO_ID, strNewAct);
                UpdateNotificationByPO(exception.PO_ID, strNewPO_ID, strNewAct);

                exception.PO_ID = strNewPO_ID;
            }
            else if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                UpdatePegasysInvoiceToReVerify(exception.INV_KEY_ID, strNewPdocno, strNewAct);
            }
            else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
            {
                UpdatePegasysRRToReVerify(exception.RR_ID, strNewPdocno, strNewAct);
            }
            else if (!String.IsNullOrWhiteSpace(exception.AE_ID))
            {
                UpdatePegasysAEToReVerify(exception.AE_ID, strNewPdocno, strNewAct);
            }
        }

        private void UpdatePegasysAEToReVerify(string ae_id, string strNewPdocno, string strNewAct)
        {
            var rtnAE = GetPegasysAEByKey(ae_id);

            var fieldsToUpdate = new List<string>
                {
                    "ACT",
                    "PDONOPO",
                    "AE_STATUS",
                    "VERIFICATION_FL",
                    "PREVALIDATION_FL",
                    "ERR_CODE"
                };

            rtnAE.ACT = strNewAct;
            rtnAE.PDOCNOPO = strNewPdocno;
            rtnAE.AE_STATUS = "RE-VERIFY";
            rtnAE.VERIFICATION_FL = "F";
            rtnAE.PREVALIDATION_FL = "F";
            rtnAE.ERR_CODE = null;

            UpdatePegasysAE(rtnAE, fieldsToUpdate);
        }

        private void UpdatePegasysRRToReVerify(string rR_ID, string strNewPdocno, string strNewAct)
        {
            var rtnRR = GetPegasysRRByKey(exception.INV_KEY_ID);

            var fieldsToUpdate = new List<string>
                {
                    "ACT",
                    "PDONOPO",
                    "RR_STATUS",
                    "VERIFICATION_FL",
                    "PREVALIDATION_FL",
                    "ERR_CODE"
                };

            rtnRR.ACT = strNewAct;
            rtnRR.PDOCNOPO = strNewPdocno;
            rtnRR.RR_STATUS = "RE-VERIFY";
            rtnRR.VERIFICATION_FL = "F";
            rtnRR.PREVALIDATION_FL = "F";
            rtnRR.ERR_CODE = null;

            UpdatePegasysRR(rtnRR, fieldsToUpdate);
        }

        private void UpdatePegasysInvoiceToReVerify(string iNV_KEY_ID, string strNewPdocno, string strNewAct)
        {
            //Update PegasysInvoice
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            var fieldsToUpdate = new List<string>
                    {
                        "ACT",
                        "PDONOPO",
                        "INV_STATUS",
                        "VERIFICATION_FL",
                        "PREVALIDATION_FL",
                        "ERR_CODE"
                    };

            rtnInv.ACT = strNewAct;
            rtnInv.PDOCNOPO = strNewPdocno;
            rtnInv.INV_STATUS = "RE-VERIFY";
            rtnInv.VERIFICATION_FL = "F";
            rtnInv.PREVALIDATION_FL = "F";
            rtnInv.ERR_CODE = null;

            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
        }

        private void UpdateNotificationByPO(string po_id, string strNewPO_ID, string strNewAct)
        {
            using (var context = new OracleVitapContext())
            {

                //Update notification
                var rtnNot = GetNotificationByPOID(po_id);

                var fieldsToUpdate = new List<string>
                    {
                        "PO_ID",
                        "ACT"
                    };

                rtnNot.PO_ID = strNewPO_ID;
                rtnNot.ACT = strNewAct;

                UpdateNotification(rtnNot, fieldsToUpdate);
            }
        }

        private void UpdatePegasysPOToModReady(string po_id, string strNewPdocno, string strNewPO_ID, string strNewAct)
        {
            var rtnPO = GetPegasysPOFrmByKey(po_id);

            var fieldsToUpdate = new List<string>
                {
                    "ACT",
                    "PO_ID",
                    "PDOCNO",
                    "ERR_CODE",
                    "PO_STATUS",
                    "DATAENTRY_FL",
                    "PREVALIDTION_FL",
                    "VCPO"
                };

            //Not sure where this is set. It needs to be addressed
            var strVCPO = "T";

            rtnPO.ACT = strNewAct;
            rtnPO.PO_ID = strNewPO_ID;
            rtnPO.PDOCNO = strNewPdocno;
            rtnPO.ERR_CODE = null;
            rtnPO.PO_STATUS = "MODREADY";
            rtnPO.DATAENTRY_FL = "T";
            rtnPO.PREVALIDATION_FL = "F";
            rtnPO.VCPO = strVCPO;

            UpdatePegasysPO(rtnPO, fieldsToUpdate);

        }        
    }
}
