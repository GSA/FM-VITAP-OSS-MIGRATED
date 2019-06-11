using System;
using System.Collections.Generic;
using VITAP.Data;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class ChangePdocnoButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "CHANGEPDOCNO");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }
        
        public bool FinishCode(string strNewPdocno)
        {
            //Used thisform.r_pdocno before
            var AskVal = "EMPTY";

            if (!String.IsNullOrWhiteSpace(PDocNo))
            {
                AskVal = PDocNo;
            }

            if (strNewPdocno != PDocNo)
            {
                //Update Exceptions Table.

                var fieldsToUpdate = new List<string>
                {
                    "PDOCNO"
                };

                exception.PDOCNO = strNewPdocno;
                UpdateException(exception, fieldsToUpdate);

                //Update the related Pegasys frm table and transhist
                if (exception.ERR_CODE == "P200")
                {
                    var rtnTranshist = GetTranshistByInvKeyID(exception.INV_KEY_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNO"
                    };

                    foreach (var row in rtnTranshist)
                    {
                        row.PDOCNO = strNewPdocno;

                        UpdateTranshist(row, fieldsToUpdate);
                    }

                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNOPO"
                    };

                    rtnInv.PDOCNOPO = strNewPdocno;
                    UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                }
                else if (exception.ERR_CODE == "A200")
                {
                    var rtnTranshist = GetTranshistByRRID(exception.RR_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNO"
                    };

                    foreach (var row in rtnTranshist)
                    {
                        row.PDOCNO = strNewPdocno;

                        UpdateTranshist(row, fieldsToUpdate);
                    }

                    var rtnRR = GetPegasysRRByKey(exception.RR_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNOPO"
                    };

                    rtnRR.PDOCNOPO = strNewPdocno;
                    UpdatePegasysRR(rtnRR, fieldsToUpdate);
                }
                else if (exception.ERR_CODE == "M200")
                {
                    var rtnTranshist = GetTranshistByAeID(exception.AE_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNO"
                    };

                    foreach (var row in rtnTranshist)
                    {
                        row.PDOCNO = strNewPdocno;

                        UpdateTranshist(row, fieldsToUpdate);
                    }

                    var rtnAE = GetPegasysAEByKey(exception.AE_ID);

                    fieldsToUpdate = new List<string>
                    {
                        "PDOCNOPO"
                    };

                    rtnAE.PDOCNOPO = strNewPdocno;
                    UpdatePegasysAE(rtnAE, fieldsToUpdate);
                }

                //Insert a Transhist Record recording the change to Pdocno
                var strCuffMemo = "Pdocno Changed from " + PDocNo + " to " + strNewPdocno.Trim() + ".";
                InsertTranshist(exception, "", strCuffMemo, "", PrepCode);

                return true;
            }
            return true;
        }
    }
}