using System;
using System.Collections.Generic;
using VITAP.Data.Managers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class RouteButton :ButtonExceptionManager
    {

        public string Initialize(EXCEPTION exc, string prepCode)
        {
            theMsg = "";
            SetVariables(exc, prepCode, "ROUTE");

            if (!Check_230_Exceptions()) { return theMsg; }
            return "";
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode()
        {
            exception = GetExceptionByKeyId(notes.ExId);
            NewNotes();

            switch (exception.ERR_CODE.Right(3))
            {
                case "200":
                case "066":
                    ExceptionU200();
                    break;

                case "230":
                case "231":
                    if (exception.ERR_CODE == "P231" || exception.ERR_CODE.Right(3) == "230")
                    {
                        Exception230();
                    }
                    break;
                case "041":
                    UpdateStatusToRoute();
                    break;
            }            

            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "Y", notes.returnVal7.ReplaceNull("").ReplaceApostrophes(), notes.returnVal3, responsenotes, "Route");

            new ExceptionsManager().CheckinException(exception.EX_ID);
        }

        protected void Exception230()
        {
            UpdateStatusToRoute();

            AddE052Exception();
        }

        private bool CheckExceptionP()
        {
            var rtnInv = new PEGASYSINVOICE();
            if (exception.ERR_CODE.Left(1) == "P")
            {
                if (notes.returnVal4 == "VITAP" || notes.returnVal4 == "PEGASYS/NON-VITAP")
                {
                    //Uses
                    rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                    if (rtnInv != null)
                    {
                        if (rtnInv.EDI_IND == "F")
                        {
                            var ImageID = rtnInv.IMAGEID;
                            //This was updating the imagelist/imagelisthist tables in FoxPro but is obsolete
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected void ExceptionU200()
        {
            UpdateStatusToRoute();

            if (notes.returnVal4 != "VITAP")
            {
                AddE052Exception();
            }
            else
            {
                string ImageID = "", ImageBatch = "", Act = "", PdocNo = "", DocType = "";
                if (!String.IsNullOrWhiteSpace(exception.PO_ID))
                {
                    var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                    if (rtnPO != null)
                    {
                        ImageID = rtnPO.IMAGEID; ImageBatch = rtnPO.IMAGEBATCH; Act = rtnPO.ACT; PdocNo = rtnPO.PDOCNO; DocType = "PO";
                    }
                }
                else if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                {
                    var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                    if (rtnInv != null)
                    {
                        ImageID = rtnInv.IMAGEID; ImageBatch = rtnInv.IMAGEBATCH; Act = rtnInv.ACT; PdocNo = rtnInv.PDOCNOPO; DocType = "INV";
                    }
                }
                else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
                {
                    HandleRR();
                    return;
                }
                else if (!String.IsNullOrWhiteSpace(exception.AE_ID))
                {
                    HandleAE();

                    return;
                }

                if (!String.IsNullOrWhiteSpace(ImageID))
                {
                    UpdateImageListByImageID(ImageID, ImageBatch, Act, PdocNo, DocType);
                }
            }

            ExceptionU066();
        }

        protected void ExceptionU066()
        {
            //Remove the Notifications Record from Notifications Table.
            if (exception.ERR_CODE == "U066")
            {
                if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
                {
                    var rtnExc = GetExceptionByInvKeyID(exception.INV_KEY_ID);
                    if (rtnExc.Count > 0)
                    {
                        foreach (var row in rtnExc)

                        {
                            if ((row.ERR_RESPONSE == null || row.ERR_RESPONSE.InList("M,S,T")) &&
                                row.NOT_KEY_ID != null && row.NOT_KEY_ID.Length >= 3 && row.NOT_KEY_ID.Left(3) == "ORA")
                            {
                                var responsenotes = "Document cleared from U066 Exception";
                                UpdateException(row, "Q", "", notes.returnVal3, responsenotes, "");
                            }
                        }

                        DeleteNotificationByInvKeyIDandExID(exception.INV_KEY_ID, exception.EX_ID);
                    }
                }
            }
        }

        protected void UpdateStatusToRoute()
        {
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID)) //!EMPTY(THISFORM.r_inv_key_id)
            {
                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                if (rtnInv != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "INV_STATUS",
                        "ERR_CODE"
                    };

                    rtnInv.INV_STATUS = "ROUTE";
                    rtnInv.ERR_CODE = null;
                    UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                }
            }
            else if (!String.IsNullOrWhiteSpace(exception.RR_ID)) // !EMPTY(THISFORM.r_rr_id)
            {
                var rtnRR = GetPegasysRRByKey(exception.RR_ID);
                if (rtnRR != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "RR_STATUS",
                        "ERR_CODE"
                    };

                    rtnRR.RR_STATUS = "ROUTE";
                    rtnRR.ERR_CODE = null;
                    UpdatePegasysRR(rtnRR, fieldsToUpdate);
                }
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID)) // !EMPTY(THISFORM.r_po_id)
            {
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                if (rtnPO != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "PO_STATUS",
                        "ERR_CODE"
                    };

                    rtnPO.PO_STATUS = "ROUTE";
                    rtnPO.ERR_CODE = null;
                    UpdatePegasysPO(rtnPO, fieldsToUpdate);
                }
            }
            else if (!String.IsNullOrWhiteSpace(exception.AE_ID)) // !EMPTY(THISFORM.r_ae_id)
            {
                var rtnAE = GetPegasysAEByKey(exception.AE_ID);
                if (rtnAE != null)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "AE_STATUS",
                        "ERR_CODE"
                    };

                    rtnAE.AE_STATUS = "ROUTE";
                    rtnAE.ERR_CODE = null;
                    UpdatePegasysAE(rtnAE, fieldsToUpdate);
                }
            }

        }

        protected void AddE052Exception()
        {
            VITAPExceptions objException = new VITAPExceptions();
            objException.ActNum = exception.ACT;

            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                objException.Inv_key_id = exception.INV_KEY_ID;
            }
            else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
            {
                objException.Rr_id = exception.RR_ID;
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                objException.Po_id = exception.PO_ID;
            }
            else if (!String.IsNullOrWhiteSpace(exception.AE_ID))
            {
                objException.Ae_id = exception.AE_ID;
            }

            objException.Err_code = "E052";
            objException.Ex_memo = "Paper Queue - Other System";
            objException.Ex_memo2 = NewNote;
            objException.PrepCode = PrepCode;
            objException.Ba = exception.BA;
            objException.Orgcode = exception.ORGCODE;
            objException.PoDocType = exception.PODOCTYPE;
            objException.Updstatus = "F";

            objException.AddException();
        }

        protected void HandleRR()
        {
            //This was inserting into FoxPro tables and may not be functioning correctly now with the code removed
            //It now clears the err_code value and sets rr_status = 'ROUTE'
            var rtnRR = GetPegasysRRByKey(exception.RR_ID);

            if (rtnRR == null)
            {
                return;
            }

            var fieldsToUpdate = new List<string>
                    {
                        "RR_STATUS",
                        "ERR_CODE"
                    };

            rtnRR.RR_STATUS = "ROUTE";
            rtnRR.ERR_CODE = null;

            UpdatePegasysRR(rtnRR, fieldsToUpdate);

            var strCuffMemo = "RR routed from Pegasys to VITAP (" + rtnRR.ACT + ")";
            InsertTranshist(exception, "", strCuffMemo, "Routed", "PrepCode");
        }

        protected void HandleAE()
        {
            //This was inserting into FoxPro tables, but no longer and may not be working correctly
            //It now clears the err_code value and sets ae_status = 'ROUTE'
            var rtnAE = GetPegasysAEByKey(exception.AE_ID);

            if (rtnAE == null)
            {
                return;
            }

            var fieldsToUpdate = new List<string>
                    {
                        "AE_STATUS",
                        "ERR_CODE"
                    };

            rtnAE.AE_STATUS = "ROUTE";
            rtnAE.ERR_CODE = null;

            UpdatePegasysAE(rtnAE, fieldsToUpdate);

            var strCuffMemo = "Accrual routed from Pegasys to VITAP (" + rtnAE.ACT + ")";
            InsertTranshist(exception, "", strCuffMemo, "Routed", PrepCode);
        }
    }
}