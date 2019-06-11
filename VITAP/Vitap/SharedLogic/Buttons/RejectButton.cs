using System;
using System.Collections.Generic;
using VITAP.Controllers;
using VITAP.Data.Managers;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class RejectButton : ButtonExceptionManager
    {
        /*
         * Need to handle Adding Exceptions in P041 exception
          */
        public string Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            theMsg = "";
            SetVariables(exc, Pdocno, prepCode, "REJECT");
            if (!Check_P_Exceptions()) { return theMsg; }
            if (!Check_230_Exceptions()) { return theMsg; }
            return "";
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        /// <summary>
        /// Calls the various Error Code methods
        /// Need to handle adding an exception under P041
        /// U084 was in the case statement twice, so the second one was removed
        /// Z234, Z237, and Z310 has been removed
        /// </summary>
        /// <param name="exception"></param>
        public void FinishCode(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address, 
            PEGASYSINVOICE InvQuery, PEGASYSRR_FRM RRFrmQuery, PEGASYSPO_FRM POFrmQuery, String PrepCode)
        {
            //string _sEx_Memo2_Prefix = "";
            //The following needs to be handled..It was being set in each method, but not necessary if we can do it once here.
            NewNotes();
            var FaxNotes = notes.returnVal2 + notes.returnVal7;

            if (!String.IsNullOrWhiteSpace(FaxNotes))
            {
                exception.FAXNOTES = FaxNotes;
                notes.FaxNotes = FaxNotes;
            }

            exception.FAXNOTES = exception.FAXNOTES.ReplaceNull("").ReplaceApostrophes();
            NewNote = NewNote.ReplaceApostrophes();

            if (exception.ERR_CODE.Right(3).InList("029,009,037"))
            {
                Exception_029_009_037(exception, Search, Address);
            }
            else if (exception.ERR_CODE.Right(3) == "036")
            {
                Exception_036(PrepCode);
            }
            else if (exception.ERR_CODE.Right(3).InList("230,232,234"))
            {
                Exception230(Search, Address, InvQuery, RRFrmQuery, POFrmQuery);
            }
            else if (exception.ERR_CODE.Right(3) == "046")
            {
                Exception046(exception, Search, Address);
            }
            else if (exception.ERR_CODE.Right(3) == "200")
            {
                ExceptionU200(Search, Address);
            }
            else
            {
                switch (exception.ERR_CODE)
                {
                    //case "C500":
                    //case "C520":
                    //    ExceptionC500(exception);
                    //    break;

                    case "D062":
                    case "M303":
                    case "M224":
                    case "M237":
                        ExceptionM237(exception);
                        break;
                    case "P060":
                        ExceptionD062(Search, Address);
                        break;

                    case "P001":
                    case "P002":
                    case "P004":
                    case "P024":
                    case "P008":
                        ExceptionP002(exception, Search, Address);
                        break;

                    case "P041":
                        if (Caption == "FINISH")
                        {
                            Exception046(exception, Search, Address);

                            //Add the Exception E052 here
                            AddException("", exception.ERR_CODE, PrepCode);
                        }
                        break;

                    case "P140":
                    case "P039": 
                        ExceptionP140(exception, Search, Address);
                        break;

                    case "P201":
                        ExceptionU200(Search, Address);
                        break;


                    case "P231":
                        Exception230(Search, Address, InvQuery, RRFrmQuery, POFrmQuery);
                        break;

                    case "V299":
                    case "V216":
                    case "V300":
                    case "V215":
                        ExceptionV299(Search, Address, InvQuery, POFrmQuery);
                        break;

                    case "A224":
                        ExceptionA224(exception,Search, Address, RRFrmQuery);
                        break;                   

                    case "A226":
                        ExceptionA226(exception, Search, Address);
                        break;

                    case "A237":
                        ExceptionA237(exception, Search, Address);
                        break;

                    default:
                        break;
                }
            }

            

            //Update the exception
            string responseNotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "X", notes.returnVal7, notes.returnVal3, responseNotes, "REJECT");
        }

        /// <summary>
        /// Used for 009, 029, and 037 exceptions handled here
        /// Need to handle setting exception.FAXNOTES value
        /// Uses Ac_Id field, but this field doesn't exist in the exceptions table anymore
        /// Need to test the code for adding the Notifications 
        /// </summary>
        /// <param name="exception"></param>
        private void Exception_029_009_037(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            if (notes.returnVal2 == "Other")
            {
                exception.FAXNOTES = notes.returnVal7;
            }
            else
            {
                exception.FAXNOTES = notes.returnVal2;
            }

            //Does not exist in Exceptions
            //Ac_Id = exception.AC_ID           

            var Ex_Date = exception.EX_DATE;
            var S_Note = "No";
            Report_ID = "F08";
            ReportForm = "eaReject";
            Status = "Pending";

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }
        }

        /// <summary>
        /// Handles 036 exceptions
        /// Only adds an E052 exception
        /// It was updating FoxPro tables, but that has been removed
        /// </summary>
        private void Exception_036(String PrepCode)
        {
            //Need the Add Exception code here to add and E052
            //This was updating the foxpro tables (Invoice, RR, ExpenseAccruals tables), and code has been removed?

            // add an E052 exception
            var objException = new VITAPExceptions();
            objException.ActNum = exception.ACT;

            if (exception.ERR_CODE.Left(1) == "P")
            {
                objException.Inv_key_id = exception.INV_KEY_ID;
            }
            objException.Err_code = "E052";
            objException.Ex_memo = "Paper Queue - Other system";
            objException.Ex_memo2 = NewNote;
            objException.PrepCode = PrepCode;
            objException.AddException();

        }

        /// <summary>
        /// Handles 046 exceptions
        /// Sets the S_Note value, but not sure what it does with it.
        /// It may be used in the screens later, but not sure
        /// Generates an EA Rejection notification if a the notes.returnVal2 in the notes screen does not contain "DUPLICATE"
        /// </summary>
        /// <param name="exception"></param>
        private void Exception046(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            var Ex_Date = exception.EX_DATE;            
            ReportForm = "";

            //Generating EA Rejection notification
            Report_ID = "F08";
            ReportForm = "EA Rejection";
            Status = "Pending";

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }
            }
        }

        /// <summary>
        /// Handles 230 exceptions
        /// Uses InvQuery.PDOCNOPO & POFrmQuery.PDOCNO values, so those need to be passed in
        /// Sets PegasysInvoice to REJECT, Adds a Notification, and a Transhist record
        /// </summary>
        private void Exception230(AddressValuesModel Search, AddressValuesModel Address, PEGASYSINVOICE InvQuery, PEGASYSRR_FRM RRFrmQuery, PEGASYSPO_FRM POFrmQuery)
        {
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID) && InvQuery != null) // && USED("invquery"))
            {
                PDocNo = InvQuery.PDOCNOPO;
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID) && POFrmQuery != null) // && USED("pofrmquery"))
            {
                PDocNo = POFrmQuery.PDOCNO;
            }
            else
            {
                PDocNo = "";
            }

            if (exception.ERR_CODE.Left(1) == "P")
            {
                NewNote = "Reject Invoice - " + NewNote;

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                if (rtnInv != null)
                {
                    UpdatePegasysInvoiceToReject(rtnInv);
                }
            }
            else if (exception.ERR_CODE.Left(1) == "A")
            {
                NewNote = "Reject RR - " + NewNote;

                var rtnRR = GetPegasysRRByKey(exception.RR_ID);
                if (rtnRR != null)
                {
                    UpdatePegasysRRToReject(rtnRR);
                }
            }
            else if (exception.ERR_CODE.Left(1) == "M")
            {
                NewNote = "Reject AE - " + NewNote;

                var rtnAE = GetPegasysAEByKey(exception.AE_ID);
                if (rtnAE != null)
                {
                    UpdatePegasysAEToReject(rtnAE);
                }

                // Do not send Rejection Notification Letter for EA's.
                return;
            }

            if (notes.returnVal2 == null || !notes.returnVal2.Contains("DUPLICATE"))
            {
                if (exception.ERR_CODE.Left(1) == "P")
                {
                    Report_ID = "P08";
                    ReportForm = "PegInvReject";
                }
                else if (exception.ERR_CODE.Left(1) == "A")
                {
                    Report_ID = "F05";
                    ReportForm = "PegRRReject";
                }

                //Do not send Rejection Notification Letter for EA's.

                if (!CheckNotificationExists())
                {
                    Status = "Pending";
                    InsertNotification();
                }

                var strCuffMemo = "Send Notification " + Report_ID + ": " + ReportForm;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }
        }

        /// <summary>
        /// Handles U200 exceptions
        /// Sets a value TheQuestion which is never used in this code, but may be used on the screen elsewhere
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// It sets the PegasysInvoice record to "REJECT"
        /// </summary>
        private void ExceptionU200(AddressValuesModel Search, AddressValuesModel Address)
        {
            var NoteInfo = "";
            ReportForm = "";

            if (!notes.returnVal2.ReplaceNull("").Contains("DUPLICATE"))
            {
                exception.PO_ID = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO;

                if (exception.ERR_CODE.Left(1) == "P" || exception.ERR_CODE.Left(1) == "R")
                {
                    Report_ID = "P08";
                    ReportForm = "PegInvReject";
                }
                else if (exception.ERR_CODE.Left(1) == "A")
                {
                    Report_ID = "F05";
                    ReportForm = "PegRRReject";
                }
                else if (exception.ERR_CODE.Left(1) == "M")
                {
                    Report_ID = "F08";
                    ReportForm = "PegAEReject";
                }

                Status = "Pending";
                var FaxNotes = notes.returnVal7;

                //Do not send Rejection Notification
                if (exception.ERR_CODE.Left(1) != "M")
                {
                    if (!CheckNotificationExists())
                    {
                        InsertNotification();
                    }

                    NoteInfo = "Send Notification: " + Report_ID;

                    var S_Note = "Yes";
                    InsertTranshist(exception, "X", "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                        Report_ID + " - " + ReportForm + "\r" + NewNote, "Reject Notification", PrepCode);
                }
            }


            if (exception.ERR_CODE.Left(1) == "P" || exception.ERR_CODE.Left(1) == "R")
            {
                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                UpdatePegasysInvoiceToReject(rtnInv);
            }
            else if (exception.ERR_CODE.Left(1) == "A")
            {
                var rtnRR = GetPegasysRRByKey(exception.RR_ID);
                UpdatePegasysRRToReject(rtnRR);
            }
            else if (exception.ERR_CODE.Left(1) == "M")
            {
                var rtnAE = GetPegasysAEByKey(exception.AE_ID);
                UpdatePegasysAEToReject(rtnAE);
            }

            NewNote = NoteInfo + "/r/n" + ReportForm + "\r" + NewNote;
        }

        /// <summary>
        /// Handles D062 exceptions
        /// Adds a notification and a transhist record
        /// Updates PegasysInvoice to reject
        /// </summary>
        private void ExceptionD062(AddressValuesModel Search, AddressValuesModel Address)
        {
            if (!notes.returnVal2.Contains("DUPLICATE"))
            {

                //WAIT "Generating Invoice Rejection Notification..." WINDOW NOWAIT
                var FaxNotes = notes.returnVal7;
                Report_ID = "P08";
                ReportForm = "PegInvReject";
                Status = "Pending";

                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                var S_Note = "Yes";

                NewNote = "Reject Invoice " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm +
                    "\r\n" + NewNote;
                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                        Report_ID + " - " + ReportForm + "\r" + NewNote;

                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }

            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            UpdatePegasysInvoiceToReject(rtnInv);
        }

        /// <summary>
        /// Handles M237 exceptions
        /// </summary>
        private void ExceptionM237(EXCEPTION exception)
        {
            NewNote = "Reject AE - " + NewNote;

            var rtnAE = GetPegasysAEByKey(exception.AE_ID);
            if (rtnAE != null)
            {
                UpdatePegasysAEToReject(rtnAE);
            }
        }


        /// <summary>
        /// Handles P001, P002, P004, and P024 exceptions
        /// Sets the ReturnValZ, but not sure what it does with that...maybe something on the screen afterwards?
        /// Adds a notification and transhist record
        /// Sets PegasysInvoice record to "REJECT"
        /// </summary>
        /// <param name="exception"></param>
        private void ExceptionP002(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            PDocNo = "";
            var Ex_Date = exception.EX_DATE;
            var S_Note = "No";
            Status = "Pending";

            if (exception.ERR_CODE == "P024")
            {
                //Not sure what happens with this...Need to handle
                notes.returnValZ = "INV";
            }

            //Generate rejection notification
            //WAIT "Generating Invoice Rejection Notification..." WINDOW NOWAIT
            var pdocno = "";
            var FaxNotes = notes.returnVal7;
            if (Pegasys)
            {
                Report_ID = "P08";
                ReportForm = "PegInvReject";
                pdocno = exception.PO_ID;
            }
            else
            {
                Report_ID = "L08";
                ReportForm = "Inv Reject";
                pdocno = "";
            }
            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                NewNote = "Reject Invoice " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;

                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                if (rtnInv != null)
                {
                    UpdatePegasysInvoiceToReject(rtnInv);
                }
            }
        }

        /// <summary>
        /// Handles P140 exceptions
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// Updates PegasysInvoice to reject
        /// </summary>
        /// <param name="exception"></param>
        private void ExceptionP140(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            var PDocNo = "";
            var Ex_Date = exception.EX_DATE;
            var S_Note = "No";
            Status = "Pending";

            //WAIT "Generating Invoice Rejection Notification..." WINDOW NOWAIT
            if (!notes.returnVal2.ReplaceNull("").Contains("DUPLICATE"))
            {
                var FaxNotes = notes.returnVal7;
                exception.PO_ID = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO; 
                Report_ID = "P08";
                ReportForm = "PegInvReject";

                PDocNo = exception.PO_ID;

                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                NewNote = "Reject Invoice " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                var strCuffMemo = "Reject RR - " + "\r\n" + "Send Notification " + S_Note + ": " +
                        Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", "", "Reject Notification", PrepCode);

            }

            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            if (rtnInv != null)
            {
                UpdatePegasysInvoiceToReject(rtnInv);
            }
        }

        /// <summary>
        /// Handles A224 exceptions
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// Update PegasysRR record to "REJECT"
        /// Update Exceptions Properties
        /// </summary>
        private void ExceptionA224(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address,PEGASYSRR_FRM RRFrmQuery)
        {           
            var rtnRR = GetPegasysRRByKey(exception.RR_ID);
            UpdatePegasysRRToReject(rtnRR);
            
            var Ex_Date = exception.EX_DATE;
            ReportForm = "";

            var S_Note = "No";
            Report_ID = "F05";
            ReportForm = "PRRReject";

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }

            var properties = new List<string>
            {
                "VENDNAME",
                "RRAMOUNT"
            };

            exception.VENDNAME = RRFrmQuery.VENDNAME;
            exception.RRAMOUNT = RRFrmQuery.AMOUNT;

            UpdateException(exception, properties);           
        }

        /// <summary>
        /// Handles A226 exceptions
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// Update PegasysRR record to "REJECT"
        /// </summary>
        private void ExceptionA226(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            var rtnRR = GetPegasysRRByKey(exception.RR_ID);
            UpdatePegasysRRToReject(rtnRR);

            var Ex_Date = exception.EX_DATE;
            ReportForm = "";

            //Generating EA Rejection notification
            var S_Note = "No";
            Report_ID = "F05";
            ReportForm = "PRRReject";

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }           
        }

        /// <summary>
        /// Handles A237 exceptions
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// Update PegasysRR record to "REJECT"
        /// </summary>
        private void ExceptionA237(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address)
        {
            var rtnRR = GetPegasysRRByKey(exception.RR_ID);
            UpdatePegasysRRToReject(rtnRR);

            var Ex_Date = exception.EX_DATE;
            ReportForm = "";

            //Generating EA Rejection notification
            var S_Note = "No";
            Report_ID = "F05";
            ReportForm = "PRRReject";

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    InsertNotification();
                }

                S_Note = "Yes";

                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }
        }

        ///// <summary>
        ///// Handles U043 exceptions
        ///// Deals with either Invoices, RRs, or POs depending on data
        ///// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        ///// Clears V299 exceptions for invoices
        ///// Sets the PegasysInvoice, PegasysRR_frm or PegasysPO_frm records to "REJECT"
        ///// </summary>
        ///// <param name="exception"></param>
        //public void ExceptionU043(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address, PEGASYSRR_FRM RRFrmQuery, PEGASYSPO_FRM POFrmQuery)
        //{
        //    DateTime? Ex_Date = exception.EX_DATE;
        //    Report_ID = "";
        //    ReportForm = "";
        //    Pegasys = false;
        //    PDocNo = "";

        //    if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
        //    {
        //        HandleInvoice(Search, Address);

        //    }
        //    else if (String.IsNullOrWhiteSpace(exception.RR_ID))
        //    {
        //        HandleRR(Search, Address, RRFrmQuery);
        //    }
        //    else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
        //    {
        //        HandlePO(Search, Address, POFrmQuery);

        //    }
        //}

        public void HandlePO(AddressValuesModel Search, AddressValuesModel Address, PEGASYSPO_FRM POFrmQuery, string thismemo)
        {
            NewNotes();

            //Need to have access to the POQuery List
            var DateQueued = DateTime.Now;
            
            if (!String.IsNullOrWhiteSpace(POFrmQuery.MODNO))
            {
                Report_ID = "F06";
                ReportForm = "Mod Reject";
            }
            else
            {
                Report_ID = "F03";
                ReportForm = "PO Reject";
            }

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (thismemo == "CANCEL")
                {
                    //DisplayMessage("Only Notification cancelled - PO will still be rejected!", 0, "Nothing");
                }
                else
                {
                    var properties = new List<string>
                    {
                        "FAXNOTES"
                    };
                    exception.FAXNOTES = thismemo; //objmemo.p_thismemo;
                    UpdateException(exception, properties);

                    if (!CheckNotificationExists())
                    {
                        Status = "Pending";
                        InsertNotification();
                    }
                }
                var S_Note = "Yes";

                NewNote = "Reject PO/Mod " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                var strCuffMemo = "'Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                    Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "", PrepCode);
            }

            var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
            UpdatePegasysPOToReject(rtnPO);
        }

        public void HandleRR(AddressValuesModel Search, PEGASYSRR_FRM RRFrmQuery, string thismemo)
        {
            NewNotes();
            //F05
            var DateQueued = DateTime.Now;
            Report_ID = "F05";
            ReportForm = "RR Reject";
            //Don't have the RRQuery...Need to get it set up
            if (RRFrmQuery != null)
            {
                exception.RR_ID = RRFrmQuery.RR_ID;
            }

            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    if (thismemo == "CANCEL")
                    {
                        //DisplayMessage("Only Notification canceled - RR will still be rejected!", 0, "Nothing");
                    }
                    else
                    {
                        var properties = new List<string>
                        {
                            "FAXNOTES"
                        };

                        exception.FAXNOTES = thismemo; //objmemo.p_thismemo;
                        UpdateException(exception, properties);
                    }

                    if (!CheckNotificationExists())
                    {
                        Status = "Pending";
                        InsertNotification();
                    }
                }
                var S_Note = "Yes";
                
                NewNote = "Reject RR " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " + Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }

            var rtnRR = GetPegasysRRByKey(exception.RR_ID);
            if (rtnRR != null)
            {
                UpdatePegasysRRToReject(rtnRR);
            }
        }

        public void HandleInvoice(AddressValuesModel Search, string thismemo)
        {
            var Address = GetAddressFromInvoice(exception.INV_KEY_ID);
            Report_ID = "P08";
            Pegasys = true;
            ReportForm = "PegInvReject";
            var po_id = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO;
            var FaxNotes = notes.returnVal7;
            if (!notes.returnVal2.Contains("DUPLICATE"))
            {
               
                    if (thismemo == "CANCEL")
                    {
                        //DisplayMessage("Only Notification cancelled - Invoice will still be rejected!", 0, "Nothing");
                    }
                    else
                    {
                        FaxNotes = thismemo;
                    }

                    if (!CheckNotificationExists())
                    {
                        Status = "Pending";
                        InsertNotification();
                    }
               
                var S_Note = "Yes";

                NewNote = "Reject Invoice " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                var strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " + Report_ID + " - " + ReportForm + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                UpdatePegasysInvoiceToReject(rtnInv);

                //Just in case there is a V299 on Hold
                UpdateExceptionV299(exception.INV_KEY_ID);

                //Update the exception
                string responseNotes = exception.RESPONSENOTES + "\r\n" + NewNote;
                UpdateException(exception, "X", notes.returnVal7, notes.returnVal3, responseNotes, "REJECT");
            }
        }

        /// <summary>
        /// Handls U049 exceptions
        /// Deals with either Invoices, RRs, or POs depending on data
        /// If notes.returnVal2 does not contain "DUPLICATE", it adds a notification and transhist record
        /// Clears V299 exceptions for invoices
        /// Sets the PegasysInvoice, PegasysRR_frm or PegasysPO_frm records to "REJECT"
        /// Users the RRQuery.RR_ID and POQuery.MODNO values, so those need to be passed in
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionU049(EXCEPTION exception, AddressValuesModel Search, AddressValuesModel Address, 
            PEGASYSRR_FRM RRFrmQuery, PEGASYSPO_FRM POFrmQuery, string thismemo)
        {
            var Ex_Date = exception.EX_DATE;
            Report_ID = "";
            string S_Note = "No";
            ReportForm = "";
            exception.PDOCNO = "";
            string strCuffMemo = "";
            Status = "Pending";

            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                var DateQueued = DateTime.Now;
                Report_ID = "P08";

                Pegasys = true;
                ReportForm = "PegInvReject";
                exception.PO_ID = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO;

                // Not sending Notification if its a DUPLICATE Invoice
                if (!notes.returnVal2.Contains("DUPLICATE"))
                {
                    //Open MandMemo screen to get user input
                    if (thismemo == "CANCEL")
                    {
                        //DisplayMessage("Only Notification cancelled - Invoice will still be rejected!", 0, "Nothing");
                    }
                    else
                    {
                        exception.FAXNOTES = thismemo;


                        if (!CheckNotificationExists())
                        {   
                            InsertNotification();
                        }
                    }

                    S_Note = "Yes";
                    NewNote = "Reject Invoice " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;

                    strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                        Report_ID + " - " + ReportForm + "\r" + NewNote;
                    InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
                }

                //Updating Invoice Table to Reject the Invoice

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                //Doesn't set lasttime to 0
                UpdatePegasysInvoiceToReject(rtnInv);

                // Just in case there is a V299 on Hold
                var rtnExc = GetExceptionV299();
                if (rtnExc != null)
                {
                    UpdateExceptionV299(exception.INV_KEY_ID);
                }

                // Creating a Custom TransHist Entry
                strCuffMemo = "U044 Invoice Number Changed Not Approved, Rejected back to Vendor";
                InsertTranshist(exception, "X", strCuffMemo, "Rejected by Manager", PrepCode);
            }
            else if (!String.IsNullOrWhiteSpace(exception.RR_ID))
            {
                //F05
                var DateQueued = DateTime.Now;
                Report_ID = "F05";
                ReportForm = "RR Reject";
                exception.RR_ID = RRFrmQuery.RR_ID;

                if (!notes.returnVal2.Contains("DUPLICATE"))
                {
                    if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                    {
                        if (thismemo == "CANCEL")
                        {
                            //DisplayMessage("Only Notification cancelled - RR will still be rejected!", 0, "Nothing");
                        }
                        else
                        {
                            exception.FAXNOTES = thismemo;

                            if (!CheckNotificationExists())
                            {
                                InsertNotification();
                            }
                        }
                    }

                    S_Note = "Yes";
                    NewNote = "Reject RR " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;
                    strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " +
                        Report_ID + " - " + ReportForm + "\r" + NewNote;
                    InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
                }

                var rtnRR = GetPegasysRRByKey(exception.RR_ID);

                UpdatePegasysRRToReject(rtnRR);
            }

            else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                var DateQueued = DateTime.Now;
                ReportForm = "";

                if (!String.IsNullOrWhiteSpace(POFrmQuery.MODNO))
                {
                    Report_ID = "F06";
                    ReportForm = "Mod Reject";
                }
                else
                {
                    Report_ID = "F03";
                    ReportForm = "PO Reject";
                }

                if (!notes.returnVal2.Contains("DUPLICATE"))
                {
                    if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                    {
                        if (thismemo == "CANCEL")
                        {
                            //DisplayMessage("Only Notification cancelled - PO will still be rejected!", 0, "Nothing");
                        }
                        else
                        {
                            exception.FAXNOTES = thismemo;

                            if (!CheckNotificationExists())
                            {
                                InsertNotification();
                            }
                        }
                    }

                    S_Note = "Yes";
                    NewNote = "Reject PO/Mod " + "\r\n" + "Send Notification " + S_Note + " " + ReportForm + "\r\n" + NewNote;

                    strCuffMemo = "Reject - " + "\r\n" + "Send Notification " + S_Note + ": " + Report_ID + " - " + ReportForm + "\r" + NewNote;
                    InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
                }

                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                //Doesn't update lasttime
                UpdatePegasysPOToReject(rtnPO);
            }
            else
            {
                //Need to do error handling here
                //DisplayMessage("Unexpected Branching of code..Needs Investigation..Contact Helpdesk..", 64, "Unexpected Outcome!!");
            }
        }

        /// <summary>
        /// Handles V299 exceptions
        /// Deals with either Invoices or POs
        /// Sets PegasysInvoice or PegasysPO_frm record to "REJECT"
        /// Adds notification and transhist records
        /// Did update C500 or C520 exceptions, but no longer needed
        /// Uses InvQuery.PDOCNOPO, POFrmQuery.PDOCNO, and GetEx.EX_DATE values so those need to be passed in
        /// 
        /// </summary>
        private void ExceptionV299(AddressValuesModel Search, AddressValuesModel Address, PEGASYSINVOICE InvQuery, PEGASYSPO_FRM POFrmQuery)
        {
            //For Both Pegasys Vendor Exceptions V299 and V216
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                if (InvQuery != null)
                {
                    PDocNo = InvQuery.PDOCNOPO.ReplaceNull("");
                    var Po_Id = InvQuery.PDOCNOPO.ReplaceNull("");
                }
                else
                {
                    PDocNo = "";
                }
                
                //Update Pegasysinvoice table

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
                if (rtnInv != null)
                {
                    UpdatePegasysInvoiceToReject(rtnInv);
                }
                

                NewNote = "Reject Invoice - " + NewNote;
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                PDocNo = POFrmQuery.PDOCNO.ReplaceNull("");
                //Update Pegasyspo_frm table

                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                UpdatePegasysPOToReject(rtnPO);
            }


            //Create Notification V08
            exception.PO_ID = String.IsNullOrWhiteSpace(exception.PDOCNO) ? exception.PO_ID : exception.PDOCNO;
            var FaxNotes = notes.returnVal7;
            var DateQueued = DateTime.Now;
            //exception.PREPCODE = objapp.prepcode;
            var strPegasys = "";
            
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID) && exception.ERR_CODE.InList("V299,V300"))
            {
                Report_ID = "V08";
                strPegasys = "PegInvReject-Vendor";
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                Report_ID = "F03";
                strPegasys = "PegPOReject";
            }

            Pegasys = true;

            if (notes.returnVal2 != null && !notes.returnVal2.Contains("DUPLICATE"))
            {
                if (!CheckNotificationExists())
                {
                    Status = "Pending";
                    InsertNotification();
                }

                var s_Note = "Yes";

                var strDate = ((DateTime)exception.EX_DATE).ToShortDateString();

                var strCuffMemo = "Reject Document - " + "\r\n" + "Send Notification " + s_Note + ": " +
                    Report_ID + " - " + strPegasys + "\r" + NewNote;
                InsertTranshist(exception, "X", strCuffMemo, "Reject Notification", PrepCode);
            }
        }

        /// <summary>
        /// Sets the PegasysInvoice.INV_STATUS = "REJECT"
        /// </summary>
        /// <param name="rtnInv"></param>
        private void UpdatePegasysInvoiceToReject(PEGASYSINVOICE rtnInv)
        {
            var fieldsToUpdate = new List<string>
                {
                    "INV_STATUS",
                    "ERR_CODE"
                };
            rtnInv.INV_STATUS = "REJECT";
            rtnInv.ERR_CODE = null;

            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
        }

        /// <summary>
        /// Sets the PegasysAEFrm.AE_STATUS = "REJECT"
        /// </summary>
        /// <param name="rtnAE"></param>
        private void UpdatePegasysAEToReject(PEGASYSAE_FRM rtnAE)
        {
            var fieldsToUpdate = new List<string>
                {
                    "AE_STATUS",
                    "ERR_CODE"
                };
            rtnAE.AE_STATUS = "REJECT";
            rtnAE.ERR_CODE = null;

            UpdatePegasysAE(rtnAE, fieldsToUpdate);
        }

        private void AddException(string allprocess, string err_code, string prepCode)
        {
            var objException = new VITAPExceptions();
            objException.ActNum = exception.ACT;
            objException.Rr_id = exception.RR_ID;
            objException.Po_id = exception.PO_ID;
            objException.Inv_key_id = exception.INV_KEY_ID;
            objException.Err_code = err_code;
            objException.Allprocess = allprocess;
            objException.PrepCode = prepCode;
            objException.AddException();
        }
    }
}

