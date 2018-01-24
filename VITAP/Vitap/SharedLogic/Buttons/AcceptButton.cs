using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using VITAP.Controllers;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.PegasysEntities;
using VITAP.Library.Strings;

namespace VITAP.SharedLogic.Buttons
{
    public class AcceptButton : ButtonExceptionManager
    {
        /*
         * Need to try to break up UpdatePegasysPO into small methods
         * Not all properties have been set yet
         * Back End process is disabled
         * Need to solve for messages requiring user input everywhere
        */

        protected string Ae_Id = "";

        /// <summary>
        /// Checks V Exceptions
        /// Checks 230 Exceptions
        /// Performs a ButtonSave for U047, U063, and U064 exceptions
        /// Displays the Notes screen
        /// Checks Button pushed in Notes screen
        /// Runs the FinishCode method if FINISH button was pushed or it's a "K" exception
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="rrchoice"></param>
        /// <param name="invquery"></param>
        /// <param name="pofrmquery"></param>
        /// <param name="ButtonCaption"></param>
        /// <param name="bDunsMatched"></param>       
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode, bool bDunsMatched = false)
        {
            /*Needs to have the following values from the screen: PDocNoAE, Po_Id are updated in the screen init() 
             * Duns Option choice, r_CodingChanges, Tab 4 DailyInt, Tab 4 DiscAmount,
             * For U047 - r_tin_as_key, r_tin, r_vendno
             * Calls the m_ClaimsQuery() method on the screen
             * Clicks the cmdSave button
             * Calls m_Close on the screen - exits screen an goes back to exceptions main screen
            */
            bDunsMatch = bDunsMatched;
            //Need to determine if Variables need to be changed after this method is called
            SetVariables(exc, Pdocno, prepCode, "ACCEPT");

            return true;
        }

        /// <summary>
        /// Checks the Button that was pushed and sets the notes variable
        /// </summary>
        /// <param name="notes"></param>
        /// <returns></returns>
        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        /// <summary>
        /// Completes the handling of the exceptions
        /// Updates NewNote for V215, V216, and V299 exceptions
        /// Updates the exception record
        /// Calls the Exception handler code for each exception
        /// Updates the ORGCODE and BA fields of the exception record
        /// </summary>
        /// <param name="exception"></param>
        public string FinishCode1(AddressValuesModel Search, AddressValuesModel Address, MiscValuesModel Misc)
        {
            if (String.IsNullOrWhiteSpace(PDocNo)) { PDocNo = exception.PO_ID; }
            if (!Check_V_Exceptions(Misc)) { return ""; }
            if (!Check_230_Exceptions()) { return theMsg; }

            string VendorID = "";

            if (Address != null)
            {
                VendorID = Address.VENDORCODE + "&" + Address.VENDORADDRESS;
            }

            //This code is specific to the ACCEPT button
            //Remember - the exception is already current upon entering the finishcode.

            //NewNote is from m.NewNotes from screen
            NewNotes();
            if (exception.ERR_CODE.InList("V215, V216, V299"))
            {
                //VendorCode and VendorAddress come from the screen values on tab 1

                if (exception.ERR_CODE == "V216")
                {
                    if (bDunsMatch == true) //This won't work right now because it's a string - need to check out what the screen value is
                    {
                        NewNote += "/r/n" + "DUNS Number Matched";

                    }
                }
            }
            NewNote = NewNote.Trim().ReplaceApostrophes();

            //For Novations - update exception info to allprocess field - exception.FAXNOTES comes from ReturnVal7 
            string responseNotes = exception.RESPONSENOTES + "\r" + NewNote + "\r" + VendorID;
            if (exception.ERR_CODE == "V216")
            {
                UpdateException(exception, "A", notes.returnVal7, notes.returnVal3, responseNotes, notes.returnVal2);
            }
            else
            {
                UpdateException(exception, "A", notes.returnVal7, notes.returnVal3, responseNotes, "");
            }

            return "";
        }

        public void FinishCode2(MiscValuesModel Misc)
        { 
            if (exception.ERR_CODE.InList("U063, U065"))
            {
                //The ORGCODE AND BA come from the Contact screen and need to be passed in
                var fieldsToUpdate = new List<string>
                {
                    "ORGCODE",
                    "BA"
                };

                //Uses Misc.ORGCODE/Misc.BA which needs to be passed in from thisform.r_orgcode/thisform.r_ba
                exception.ORGCODE = Misc.ORGCODE.Trim();
                exception.BA = Misc.BA.Trim();

                UpdateException(exception, fieldsToUpdate);

            }

            ButtonPushed = "ACCEPT";
        }

        /// <summary>
        /// Updates the exception and exceptionhist records by Act#
        /// </summary>
        public void ExceptionE063_Recycle()
        {
            UpdateExceptionByAct();
            UpdateExceptionHistByAct();
        }

        public string ExceptionK(string newInvKeyId, string invKeyId, string prepCode)
        {
            notes.returnVal1 = "NONE";

            var rtnMFII = new List<MF_II_LITE>();
            if (exception.ERR_CODE.InList("KMWA,KMKA,KI7A,KM7A"))
            {
                rtnMFII = GetMFIIData(newInvKeyId);
            }
            if (rtnMFII.Count == 0)
            {
                return "The New Pegasys Invoice Doc Number does not exist. Please make \r" +
                "sure that the invoice has been successfully PROCESSED in PEGASYS \r" +
                "and click ACCEPT again.";
            }

            var invQuery = GetPegasysInvoiceByKey(invKeyId);

            notes.returnVal5 = invQuery.VEND_CD + " " + invQuery.VEND_ADDR_CD;

            if (exception != null)
            {
                NewNote = NewNotes();

                string responseNotes = NewNote + "\r\nACCEPT " + " - Inv_Key_ID was changed from " + 
                    invKeyId + " to " + newInvKeyId + "\r\nVend Number: " + notes.returnVal5.Trim();

                //Update Exceptions
                var fieldsToUpdate = new List<string>
                {
                    "RESPONSENOTES",
                    "ERR_RESPONSE",
                    "PREPCODE",
                    "CLEARED_DATE",
                    "OUT",
                    "ADDPC"
                };

                exception.RESPONSENOTES = responseNotes.Trim();
                exception.ERR_RESPONSE = "A";
                exception.PREPCODE = prepCode;
                exception.CLEARED_DATE = DateTime.Now;
                exception.ALLPROCESS = "";
                exception.OUT = "F";
                exception.ADDPC = prepCode;
                UpdateException(exception, fieldsToUpdate);

                //Update PegasysInvoice
                fieldsToUpdate = new List<string>
                {
                    "INV_STATUS",
                    "ERR_CODE"
                };
                invQuery.INV_STATUS = "OUTBOX";
                invQuery.ERR_CODE = null;
                UpdatePegasysInvoice(invQuery, fieldsToUpdate);

                //Update all exceptions, notification and transhist records for the New Inv_Key_ID
                UpdatePegasysInvoiceNewInvKeyId(invKeyId, newInvKeyId);
                UpdateExceptionNewInvKeyId(invKeyId, newInvKeyId);
                UpdateTranshistNewInvKeyId(invKeyId, newInvKeyId);
                UpdateNotificationNewInvKeyId(invKeyId, newInvKeyId);
            }
            return "";
        }

        /// <summary>
        /// Updates the Notifications.STATUS field
        /// Updates the Exception and ExceptionHist records by Act#
        /// </summary>
        public void ExceptionE065_Recycle(MiscValuesModel Misc)
        {
            //Updates the Notification then recycles the exception
            //Uses exception.NOT_KEY_ID instead of thisform.r_not_key_id
            var fieldsToUpdate = new List<string>
                    {
                        "STATUS"
                    };
            var rtnNot = GetNotificationByKey(exception.NOT_KEY_ID);
            if (rtnNot != null)
            {
                rtnNot.STATUS = "Pending";
                UpdateNotification(rtnNot, fieldsToUpdate);
            }

            //bool pass_update = false;
            //Update the Exception table
            UpdateExceptionByAct();

            //Update the Exception History table
            UpdateExceptionHistByOrgCodeBa(Misc);

        }

        /// <summary>
        /// Updates the PegasysInvoice Record to INV_STATUS = "MATCHREADY"
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// Adds a new notification
        /// </summary>
        public void ExceptionP001()
        {
            //Used exception.INV_KEY_ID before
            UpdatePegasysInvoiceToMatchReady();

            //Backend process has been disabled
            #region BackendP001
            //m.theprocess = 'Automated Match Process'

            //A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //            36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion
        }

        /// <summary>
        /// Gets the PegasysInvoice data
        /// If the Amount from the Notes screen is less than the PegasysInvoice.Amount it creates a notification,
        ///     sets the PegasysInvoice to MATCHREADY, and updates the exception Amount 
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP002(AddressValuesModel Search, AddressValuesModel Address)
        {
            //used for P002 and P004
            //Sets the Amount value to notes.returnVal9 from the Notes screen
            decimal.TryParse(notes.returnVal9, out Amount);

            //Set administrative difference statement
            //Used exception.INV_KEY_ID before
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            if (rtnInv != null )
            {
                exception.PDOCNO = rtnInv.PDOCNOPO;
                if (Amount < rtnInv.AMOUNT)
                {
                    if (String.IsNullOrWhiteSpace(notes.returnVal5))
                    {
                        notes.returnVal5 = "";
                    }


                    if (notes.returnVal5.ToUpper() != Reportid.Authorized)
                    {
                        SetReportID();

                        //WAIT "Generating adminstrative difference statement..." WINDOW NOWAIT
                        //Set several local variables before...This needs to be tested since it uses exception values now
                        if (!CheckNotificationExists())
                        {
                            ReportForm = "AdminDiffStmt";
                            Status = "Waiting";

                            InsertNotification();
                        }
                    }

                    //update status code
                    //Used exception.INV_KEY_ID before
                    UpdatePegasysInvoiceToMatchReady();
                    UpdateExceptionAmount();
                }
            }

            #region BackendP002
            //m.theprocess = 'Automated Match Process'

            //A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //    36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion
        }

        /// <summary>
        /// Updates PegasysInvoice to MATCHREADY inv_status
        /// Updates the Exception amount field
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP023()
        {
            //Check for pegasys record or not
            //Used exception.INV_KEY_ID before
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            if (rtnInv != null)
            {
                exception.PDOCNO = rtnInv.PDOCNOPO;

                //update statuscode
                UpdatePegasysInvoiceToMatchReady();
            }
            

            UpdateExceptionAmount();

            //Backend job is no long run
            #region backendP023
            //m.theprocess = 'Automated Match Process'

            //A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //    36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion
        }

        /// <summary>
        /// Handles P024 exceptions
        /// Checks if the Notes Amount is less than the PegasysInvoice.Amount 
        /// Adds a notification
        /// Updates the PegasysInvoice to MATCHREADY
        /// Updates the exception.AMOUNT value
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP024(AddressValuesModel Search, AddressValuesModel Address)
        {
            //Used exception.INV_KEY_ID before
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            exception.PDOCNO = rtnInv.PDOCNOPO;
            //bool Pegasys = true;

            if (Amount < rtnInv.AMOUNT)
            {
                SetReportID();

                if (notes.returnVal2.ToUpper() == Reportid.QuantityVariance && notes.returnVal5.ToUpper() == Reportid.Authorized)
                {
                    if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                    {
                        exception.FAXNOTES = "Authorized Quantity Variance";
                    }
                }

            }

            //WAIT "Generating adminstrative difference statement..." WINDOW NOWAIT
            //Used local variables from the screen before...now uses exception values...need to test
            if (!CheckNotificationExists())
            {
                ReportForm = "AdminDiffStmt";
                Status = DocStatus.Waiting;
                InsertNotification();
            }

            ReportForm = "AdminDiffStmt";
            Status = DocStatus.Waiting;


            //update status code
            //Used exception.INV_KEY_ID before
            UpdatePegasysInvoiceToMatchReady();
            UpdateExceptionAmount();
        }

        /// <summary>
        /// Handles P033 and P034 exceptions
        /// Sets the PegasysInvoice to MATCHREADY
        /// Updates the exception.PARTIAL_MATCH_VENDNO value
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP033()
        {
            //Used for P033 and P034 exceptions
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            //The FoxPro does not update the lasttime like this code does
            //Used exception.INV_KEY_ID before
            UpdatePegasysInvoiceToMatchReady();

            var fieldsToUpdate = new List<string>
            {
                "PARTIAL_MATCH_VENDNO"
            };

            switch (exception.ERR_CODE)
            {
                case "P033":
                    if (notes.returnVal5 == PartialMatchVendNo.PayTaxes)
                    {
                        exception.PARTIAL_MATCH_VENDNO = notes.returnVal5;
                        UpdateException(exception, fieldsToUpdate);
                    }
                    break;
                case "P034":
                    if (notes.returnVal5 == PartialMatchVendNo.PayTariff)
                    {
                        exception.PARTIAL_MATCH_VENDNO = notes.returnVal5;
                        UpdateException(exception, fieldsToUpdate);
                    }
                    break;
            }
        }

        /// <summary>
        /// Removes the MULTRR and MATCHRRINV records for the exception.INV_KEY_ID
        /// If only 1 record exists, updates the RR_ID value in exceptions - Have to populate an RRList from the screen
        /// If multiple records exist in the RRList, it sets RR_ID = "MRR" in exceptions and adds the list to the MULTRR table
        /// Updates PegasysInvoice to MATCHREADY
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP039()
        {
            //Build MULTRR
            //These tables need to be added to the context
            var rtnMultRR = GetMultRRByInvKey(exception.INV_KEY_ID);
            var rtnMatchRR = GetMatchRRByInvKey(exception.INV_KEY_ID);
            using (var contextVitap = new OracleVitapContext())
            {
                contextVitap.Configuration.AutoDetectChangesEnabled = true;
                contextVitap.MULTRRs.Remove(rtnMultRR[0]);
                contextVitap.MATCHRRINVs.Remove(rtnMatchRR[0]);
                contextVitap.SaveChanges();
            }

            var fieldsToUpdate = new List<string>
            {
                "RR_ID"
            };

            if (RRList.Count == 1) // ALEN(YesRRArray,1) = 1 //only one rr picked, put rr_id in exception record
            {
                exception.RR_ID = RRList[0].RR_ID;
                UpdateException(exception, fieldsToUpdate);

                //lcSql = "UPDATE exceptions SET rr_id = '" + RRList[0, 3] + "' WHERE ex_id = '" + Ex_Id + "'";
                //objapp.m_vitapquery(lcSql)
            }
            else //more than one rr picked, leave "MRR" in exception.rr_id, add to MULTRR.DBF
            {
                exception.RR_ID = "MRR";
                UpdateException(exception, fieldsToUpdate);
                MULTRR multRR = new MULTRR();
                using (var contextVitap = new OracleVitapContext())
                {
                    contextVitap.Configuration.AutoDetectChangesEnabled = true;
                    foreach (var row in RRList)
                    {
                        //Act no longer exists in the oracle table and is not needed
                        multRR.EX_ID = exception.EX_ID;
                        multRR.INV_KEY_ID = exception.INV_KEY_ID;
                        multRR.RR_ID = row.RR_ID;
                        contextVitap.MULTRRs.Add(multRR);
                        contextVitap.SaveChanges();
                    }
                }
            }
            //if only one exception for this record
            UpdatePegasysInvoiceToMatchReady();

            #region backendP039
            //No longer called from Vitap
            // m.theprocess = 'Automated Match Process'

            // A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //    36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion
        }

        /// <summary>
        /// Handles the P060 and P061 exceptions
        /// Update the exception.ACTNCODE fields
        /// Updates exceptions.PARTIAL_MATCH_VENDNO for SUSPENSE INTEREST and SUSPEND INTEREST
        /// Updates the PegasysInvoice record to MATCHREADY
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP060()
        {
            //if manual payment there shgould be an entry in the manualpayment.dbf to indicate what type.
            //put "SUSPINT" in the partial_match_vendno field of the exception if Interest is to be suspensed
            //put the action code on the exception record.

            //ALSO USED FOR P061
            var fieldsToUpdate = new List<string>
            {
                "ACTNCODE"
            };

            //Updates value from ReturnVal5 in Notes screen (notes.returnVal5)
            exception.ACTNCODE = notes.returnVal5;

            UpdateException(exception, fieldsToUpdate);

            if (notes.returnVal2.ToUpper().InList("SUSPENSE INTEREST,SUSPEND INTEREST"))
            {
                exception.PARTIAL_MATCH_VENDNO = "SUSPINT";

                fieldsToUpdate = new List<string>
                {
                    "PARTIAL_MATCH_VENDNO"
                };

                UpdateException(exception, fieldsToUpdate);
            }

            UpdatePegasysInvoiceToMatchReady();

            #region BackendP039
            //Disabling the BackEnd code - this will be done in the Cron jobs later
            //string TheProcess = "Automated Match Process";

            //A = DisplayMessage("Do you want to run " + TheProcess + " Immediately?", 36, "Run Backend Processes");
            //if (A == 6)
            //{ 
            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion 
        }

        /// <summary>
        /// Handles P140 exceptions
        /// Adds Records to the MATCHRRINV table from the RRChoice list
        /// Updates the PegasysInvoice record to MATCHREADY, MATCHED, or WAITONRR depending
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP140(List<RRCHOICE> RRChoice)
        {
            if (exception.RR_ID == "NONE")
            {
                ExceptionP140None(RRChoice);
            }
            else
            {
                ExceptionP140Other(RRChoice);
            }
        }

        private void ExceptionP140None(List<RRCHOICE> RRChoice)
        {
            if (RRChoice.Count > 1)
            {
                foreach (var row in RRChoice)
                {
                    if (row.RR_ID == "NONE")
                    {
                        continue;
                    }

                    using (var contextVitap = new OracleVitapContext())
                    {
                        contextVitap.Configuration.AutoDetectChangesEnabled = true;
                        var matchRRInv = new MATCHRRINV();
                        matchRRInv.ACT = exception.ACT;
                        matchRRInv.RR_ID = row.RR_ID;
                        matchRRInv.INV_KEY_ID = exception.RR_ID;
                        matchRRInv.MATCH = "F";
                        contextVitap.MATCHRRINVs.Add(matchRRInv);
                        contextVitap.SaveChanges();
                    }
                }
            }
            //The FoxPro does not update the lasttime like this code does
            UpdatePegasysInvoiceToMatchReady();
        }

        private void ExceptionP140Other(List<RRCHOICE> RRChoice)
        { 
            bool InPeg = false;
            string MatchIt = "F";

            if (RRChoice.Count > 1)
            {
                foreach (var row in RRChoice)
                {
                    if (row.RR_ID == "NONE")
                    {
                        continue;
                    }

                    if (row.RR_ID == exception.RR_ID)
                    {
                        MatchIt = "T";
                    }
                    else
                    {
                        MatchIt = "F";
                    }

                    InPeg = row.INPEG;

                    using (var contextVitap = new OracleVitapContext())
                    {
                        contextVitap.Configuration.AutoDetectChangesEnabled = true;
                        var matchRRInv = new MATCHRRINV();
                        matchRRInv.ACT = exception.ACT;
                        matchRRInv.RR_ID = row.RR_ID;
                        matchRRInv.INV_KEY_ID = exception.RR_ID;
                        matchRRInv.MATCH = MatchIt;
                        contextVitap.MATCHRRINVs.Add(matchRRInv);
                        contextVitap.SaveChanges();

                        var fieldsToUpdate = new List<string>
                        {
                            "INV_STATUS",
                            "PDOCNORR",
                            "ERR_CODE"
                        };
                        var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                        if (MatchIt == "F")
                        {
                            if (InPeg)
                            {
                                rtnInv.INV_STATUS = "MATCHED";
                                rtnInv.PDOCNORR = exception.RR_ID;
                                UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                            }
                        }
                        else
                        {
                            rtnInv.INV_STATUS = "WAITONRR";
                            rtnInv.WAITPDOCNORR = exception.RR_ID;
                            rtnInv.ERR_CODE = null;
                            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);

                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles P201 exceptions
        /// Adds a Notification
        /// Updates PegasysInvoice to MATCHREADY
        /// Updates exceptions.PAY_AMOUNT to Amount from Notes
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP201(AddressValuesModel Search, AddressValuesModel Address)
        {
            SetReportID();

            if (notes.returnVal2 == Reportid.Freight && notes.returnVal5 == Reportid.Authorized)
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    exception.FAXNOTES = "Authorized Freight Charges" + "\r\n" + notes.returnVal3;
                }
            }
            else if (notes.returnVal2 == Reportid.QuantityVariance && notes.returnVal5 == Reportid.Authorized)
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    exception.FAXNOTES = "Authorized Quantity Variance" + "\r\n" + notes.returnVal2;
                }
            }
            else
            {
                if (String.IsNullOrWhiteSpace(exception.FAXNOTES))
                {
                    exception.FAXNOTES = notes.returnVal2;
                }
            }

            //WAIT "Generating adminstrative difference statement..." WINDOW NOWAIT
            if (CheckNotificationExists())
            {
                ReportForm = "AdminDiffStmt";
                RequestNo = "1";
                InsertNotification();
            }

            //This one does not inlcude lasttime in the update
            UpdatePegasysInvoiceToMatchReady();

            //This probably needs to be added to the Transhist table
            var CuffMemo = "P201 Exception Accepted - " + notes.returnVal2;
            if (Amount != 0)
            {
                CuffMemo += " - Pay $" + Amount.ToString();
            }

            var fieldsToUpdate = new List<string>
            {
                "AMOUNT"
            };

            exception.PAY_AMOUNT = Amount;
            UpdateException(exception, fieldsToUpdate);

            #region BackendP201
            //m.theprocess ="Automated Match Process";

            //A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //    36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion
        }

        /// <summary>
        /// Handles the P202 exception
        /// Updates PegasysInvoice to MATCHREADY
        /// Updates exceptions.PAY_AMOUNT to Amount from Notes
        /// Ran the Back End process in the old code, but is no longer going to be run in the new app
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionP202()
        {
            //Does not include lasttime setting
            UpdatePegasysInvoiceToMatchReady();

            UpdateExceptionAmount();

            #region backendP202
            //m.theprocess = 'Automated Match Process'

            //A = DisplayMessage("Do you want to run " + m.theprocess + " Immediately?",;
            //            36, "Run Backend Processes")
            //IF A = 6

            //    PUBLIC objBackend, objPayment

            //    SET CLASSLIB TO backend ADDITIVE
            //    objBackend = CREATE("finbackend")

            //    objBackend.SHOW

            //    SET CLASS TO finlib ADDITIVE

            //    objPayment = CREATE("NEARTransaction")

            //    objBackend.r_this_inv_key_id = exception.INV_KEY_ID

            //    objBackend.m_PaymentDiagram

            //    DisplayMessage("Finished running backend diagram!")

            //    RELE objPayment, objBackend
            //ENDIF
            #endregion 
        }

        /// <summary>
        /// Handles U044 exceptions
        /// Updates PegasysRR_Frm to KEYED
        /// Needs to generate a transhist record but doesn't
        /// </summary>
        public void ExceptionU044()
        {
            //RRExists is not set and needs to be addressed
            if (RRExists)
            {
                UpdatePegasysRRByAct();
               
                //Appears to need to generate transaction history record, but doesn't
                var CuffMemo = "";
                if (exception.ERR_CODE == "U044")
                {
                    CuffMemo = exception.ERR_CODE + " Exception Accepted - Not a Duplicate!";
                }
                else
                {
                    CuffMemo = exception.ERR_CODE + " Exception Accepted - keep the Period of Performance!";
                }
            }
        }

        /// <summary>
        /// Handles the U066 exception
        /// Updates the Notifications.CONTACT_ID value from the ContactList
        /// Doesn't produce a Transhist record
        /// </summary>
        public void ExceptionU066()
        {
            //update Notification records
            Contact_ID = ContactList.CONTACT_ID;

            if (!String.IsNullOrWhiteSpace(exception.NOT_KEY_ID))
            {
                var fieldsToUpdate = new List<string>
                {
                    "CONTACT_ID"
                };
                var rtnNot = GetNotificationByKey(exception.NOT_KEY_ID);

                if (rtnNot != null)
                {
                    UpdateNotification(rtnNot, fieldsToUpdate);
                }
            }
        }

        /// <summary>
        /// Handles the U084 exception
        /// Updates the PegasysPO_frm to KEYED
        /// Updates the Exceptions.VENDNAME and PDOCNO
        /// </summary>
        /// <param name="exception"></param>
        public void ExceptionU084(PEGASYSPO_FRM POFrmQuery)
        {
            //Update Pegasyspo_frm status
            var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);

            var fieldsToUpdate = new List<string>
            {
                "PO_STATUS",
                "ERR_CODE",
                "PREVALIDATION_FL"
            };

            rtnPO.PO_STATUS = "KEYED";
            rtnPO.ERR_CODE = null;
            rtnPO.PREVALIDATION_FL = "F";

            UpdatePegasysPO(rtnPO, fieldsToUpdate);

            //Update the Exceptions table
            fieldsToUpdate = new List<string>
            {
                "VENDNAME",
                "PDOCNO"
            };

            //PDOCNO is the value from the screen which could be overridden
            exception.PDOCNO = PDocNo;
            exception.VENDNAME = POFrmQuery.VENDNAME;

            UpdateException(exception, fieldsToUpdate);
        }

        /// <summary>
        /// Handles the V295 exceptions
        /// Updates numerous fields in the PegasysPO_frm and PegasysPOOffc_frm tables
        /// </summary>
        public void ExceptionV295(AddressValuesModel Search,AddressValuesModel Address)
        {
            PDocNo = exception.PO_ID;

            if (!String.IsNullOrWhiteSpace(exception.PO_ID))
            {
                var rtnOffc = GetPegasysPOOffcByKey(exception.PO_ID, exception.PARTIAL_MATCH_VENDNO);
                //This may be obsolete since it's using FoxPro tables
                var rtnAddr = GetPOVendAddr(Address.ADDR1);
                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);

                switch (rtnPO.VALIDATION_FL)
                {
                    case "T":
                        if (!String.IsNullOrWhiteSpace(rtnPO.MODNO)) // and edi_ind = 'T' - Changed on 02 / 26 / 02 by guru
                        {
                            rtnPO.PO_STATUS = "PEAREADY";
                            rtnPO.ERR_CODE = null;
                        }
                        else
                        {
                            rtnPO.PO_STATUS = "PREOUT";
                            rtnPO.ERR_CODE = null;
                        }
                        break;

                    case "F":
                        rtnPO.PO_STATUS = "NEEDVAL";
                        rtnPO.ERR_CODE = null;
                        break;
                }
                rtnPO.OFFICEMATCH_FL = "T";

                var fieldsToUpdate = new List<string>
                {
                    "PO_STATUS",
                    "ERR_CODE",
                    "OFFICEMATCH_FL",
                    "OFFC_CD",
                    "ADDR_CD",
                    "ADDR_NM",
                    "ADDR_L1",
                    "ADDR_L2",
                    "ADDR_L3",
                    "ADDR_CITY",
                    "ADDR_STATE",
                    "ADDR_ZPCD"
                };

                UpdatePegasysPO(rtnPO, fieldsToUpdate);

                //The following values are derived from the screen display and come from Pegasys data in the following tables: mf_addr_levl_offc.* from mf_addr_levl_offc
                rtnOffc.OFFC_CD = Search.VENDORCODE;
                rtnOffc.ADDR_CD = Search.VENDORADDRESS;
                rtnOffc.ADDR_NM = Search.VENDORNAME;
                rtnOffc.ADDR_L1 = Search.ADDR1;
                rtnOffc.ADDR_L2 = Search.ADDR2;
                rtnOffc.ADDR_L3 = Search.ADDR3;
                rtnOffc.ADDR_CITY = Search.CITY;
                rtnOffc.ADDR_STATE = Search.STATE;
                rtnOffc.ADDR_ZPCD = Search.ZIP;

                UpdatePegasysPOOffc(rtnOffc, fieldsToUpdate);

                //It appears to not do anything with this value...it may be handled in the screens
                var Officecode = Search.VENDORCODE + " " + Search.VENDORADDRESS;

            }
        }

        /// <summary>
        /// Handles the V299 exceptions
        /// Uses the InvQuery and POFrmQuery lists from the screen to get PdocnoPO and Pdocno, so the value(s) need(s) to be set somewhere
        /// Updates PegasysInvoice or PegasysPO_frm records, and Rolls Back to PEA in certain cases
        /// </summary>
        public void ExceptionV299(string invKeyId, PEGASYSPO_FRM POFrmQuery, MiscValuesModel Misc, AddressValuesModel Search, AddressValuesModel Address)
        {
            //Need to figure out how to get the tables for this update
            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                var InvQuery = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                if (InvQuery != null)
                {
                    exception.PDOCNO = InvQuery.PDOCNOPO;
                }
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID) && POFrmQuery != null)
            {
                exception.PDOCNO = POFrmQuery.PDOCNO;
            }

            if (!String.IsNullOrWhiteSpace(exception.INV_KEY_ID))
            {
                UpdatePegasysInvoice(Search);
            }
            else if (!String.IsNullOrWhiteSpace(exception.PO_ID) && exception.ERR_CODE.InList("V215, V216"))
            {
                //This may be obsolete since it pulls from FoxPro tables
                //InsertPOVendAddrRecord();

                var rtnPO = GetPegasysPOFrmByKey(exception.PO_ID);
                UpdatePegasysPO(rtnPO, Misc, Search);
                RollBackToPEA(rtnPO);
            }
        }

        /// <summary>
        /// Only works with ACCEPT button
        /// Handles V215 and V216 exceptions
        /// </summary>
        /// <returns></returns>
        public bool Check_V_Exceptions(MiscValuesModel Misc)
        {
            //Only ACCEPT button works with this
            //Uses exception.ERR_CODE instead of Thisform.r_err_code from the screen
            if (exception.ERR_CODE != "V216" && exception.ERR_CODE != "V215")
            {
                return true;
            }

            return true;
        }

        public void UpdateExceptionAmount()
        {
            var fieldsToUpdate = new List<string>
            {
                "PAY_AMOUNT"
            };

            exception.PAY_AMOUNT = Amount;

            UpdateException(exception, fieldsToUpdate);
        }

        public void ExceptionException046()
        {

        }

        /// <summary>
        /// Updates PegasysInvoice table in various fields
        /// Needs to get values from the screen in order to update properly
        /// </summary>
        public void UpdatePegasysInvoice(AddressValuesModel Search)
        {
            var fieldsToUpdate = new List<string>
            {
                "INV_STATUS",
                "ERR_CODE",
                "VEND_CD",
                "VEND_ADDR_CD",
                "VENDNAME",
                "ADDR_L1",
                "ADDR_L2",
                "ADDR_L3",
                "ADDR_CITY",
                "ADDR_STATE",
                "ADDR_ZPCD"
            };

            var rtn = GetPegasysInvoiceByKey(exception.INV_KEY_ID);
            //update statuscode and address details                
            rtn.ERR_CODE = null;

            if (exception.ERR_CODE == "V300")
            {
                rtn.INV_STATUS = "MATCHREADY";
            }
            else
            {
                rtn.INV_STATUS = "KEYED";
                rtn.PREVALIDATION_FL = "F";
            }
            //These values come from the screen and need to be passed in from there
            rtn.VEND_CD = Search.VENDORCODE;
            rtn.VEND_ADDR_CD = Search.VENDORADDRESS;
            rtn.VENDNAME = Search.VENDORNAME;
            rtn.ADDR_L1 = Search.ADDR1;
            rtn.ADDR_L2 = Search.ADDR2;
            rtn.ADDR_L3 = Search.ADDR3;
            rtn.ADDR_CITY = Search.CITY;
            rtn.ADDR_STATE = Search.STATE;
            rtn.ADDR_ZPCD = Search.ZIP;

            UpdatePegasysInvoice(rtn, fieldsToUpdate);
        }

        public void UpdatePegasysInvoiceToMatchReady()
        {
            var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

            if (rtnInv != null)
            {
                var fieldsToUpdate = new List<string>
                {
                    "INV_STATUS",
                    "LASTTIME",
                    "ERR_CODE"
                };
                rtnInv.INV_STATUS = "MATCHREADY";
                rtnInv.LASTTIME = 0;
                rtnInv.ERR_CODE = null;

                UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
            }
        }

        //Need to try to break up UpdatePegasysPO into small methods
        public void UpdatePegasysPO(PEGASYSPO_FRM rtnPO, MiscValuesModel Misc, AddressValuesModel Search)
        {

            //If there are V216 exceptions for the same po_id retain the status to Exception and err_code as V216
            var rtnVExc = GetExceptionV21X();

            if (rtnVExc != null)
            {
                rtnPO.PO_STATUS = "EXCEPTION";
                rtnPO.ERR_CODE = exception.ERR_CODE;
            }
            else
            {
                var rtnC500 = GetExceptionC500();
                if (rtnC500 != null)
                {
                    rtnPO.PO_STATUS = "EXCEPTION";
                    rtnPO.ERR_CODE = "C500";
                }
                else
                {
                    if (rtnPO.VALIDATION_FL == "T")
                    {
                        rtnPO.PO_STATUS = "NEEDOFFC";
                    }
                    else
                    {
                        rtnPO.PO_STATUS = "NEEDVAL";
                    }
                }
            }
            rtnPO.VENDORMATCH_FL = "T";

            CheckPayeeExceptions(rtnPO, Search);

            var ccr_flag = bDunsMatch == true ? "T" : bDunsMatch != null ? "F" : "";
            rtnPO.CCR_FLAG = ccr_flag;
            rtnPO.DUNS = Misc.DUNS;
            rtnPO.DUNS_PLUS_4 = Misc.DUNSPLUS4;

            if (notes.returnVal2.Contains("NOVAT"))
            {
                if (notes.returnVal5.ToUpper().Trim() == "YES") // Check Remove Designated Agent Information
                {

                    rtnPO.ADDR_TYPE = "NOVATE_RD";
                }
                else
                {
                    rtnPO.ADDR_TYPE = "NOVATE";
                }
            }
            else if (notes.returnVal2.Contains("DESIG"))
            {
                rtnPO.ADDR_TYPE = "DESIG";
            }
            else
            {
                rtnPO.ADDR_TYPE = "";
            }

            rtnPO.PREVALIDATION_FL = "T";

            var fieldsToUpdate = new List<string>
            {
                "INV_STATUS",
                "ERR_CODE",
                "VEND_CD",
                "VEND_ADDR_CD",
                "VENDNAME",
                "ADDR_L1",
                "ADDR_L2",
                "ADDR_L3",
                "ADDR_CITY",
                "ADDR_STATE",
                "ADDR_ZPCD"
            };
            UpdatePegasysPO(rtnPO, fieldsToUpdate);

        }

        /// <summary>
        ///To process for records with alternate payee and designated payee exceptions
        /// </summary>
        /// <param name="rtnPO"></param>
        public void CheckPayeeExceptions(PEGASYSPO_FRM rtnPO, AddressValuesModel Search)
        {
            //Search values below come from the screen Search values
            switch (exception.EX_MEMO)
            {
                case "NO ALTERNATE PAYEE MATCH":

                    rtnPO.ALTR_PAYE_CD = Search.VENDORCODE;
                    rtnPO.ALTR_PAYE_ADDR_CD = Search.VENDORADDRESS;
                    rtnPO.ALTR_PAYE_NM = Search.VENDORNAME;
                    rtnPO.ALTR_PAYE_ADDR_L1 = Search.ADDR1;
                    rtnPO.ALTR_PAYE_ADDR_L2 = Search.ADDR2;
                    rtnPO.ALTR_PAYE_ADDR_L3 = Search.ADDR3;
                    rtnPO.ALTR_PAYE_CITY = Search.CITY;
                    rtnPO.ALTR_PAYE_STAE = Search.STATE;
                    rtnPO.ALTR_PAYE_ZPCD = Search.ZIP;
                    break;

                case "NO DESIGGNATED PAYEE MATCH":

                    rtnPO.DGGT_CD = Search.VENDORCODE;
                    rtnPO.DGGT_ADDR_CD = Search.VENDORADDRESS;
                    rtnPO.DGGT_ADDR_NM = Search.VENDORNAME;
                    rtnPO.DGGT_ADDR_L1 = Search.ADDR1;
                    rtnPO.DGGT_ADDR_L2 = Search.ADDR2;
                    rtnPO.DGGT_ADDR_L3 = Search.ADDR3;
                    rtnPO.DGGT_ADDR_CITY = Search.CITY;
                    rtnPO.DGGT_ADDR_STAE = Search.STATE;
                    rtnPO.DGGT_ADDR_ZPCD = Search.ZIP;
                    break;

                default:

                    rtnPO.VEND_CD = Search.VENDORCODE;
                    rtnPO.VEND_ADDR_CD = Search.VENDORADDRESS;
                    rtnPO.VENDNAME = Search.VENDORNAME;
                    rtnPO.ADDR_L1 = Search.ADDR1;
                    rtnPO.ADDR_L2 = Search.ADDR2;
                    rtnPO.ADDR_L3 = Search.ADDR3;
                    rtnPO.ADDR_CITY = Search.CITY;
                    rtnPO.ADDR_STATE = Search.STATE;
                    rtnPO.ADDR_ZPCD = Search.ZIP;
                    break;
            }
        }
        public void RollBackToPEA(PEGASYSPO_FRM rtnPO)
        {
            //Sometimes V216 exceptions are created after the Mod has been processed through
            //the PEA Process i.e from Copy Forward.

            //In such cases, we have to process the updated vendor information(from V216 Exception)
            //through PEA Process and hence the accounting line and office information
            //has to be rolled back to the pre PEA process stage.

            //First check if the PO Mod has processed through the PEA Process

            //Convert to EF
            var allprocess = "PEA";
            var rtnTH = GetTransHistsByPOID(exception.PO_ID, allprocess);

            if (rtnTH.Count > 0)
            {
                //Delete the accounting line information in the Form tables
                using (var contextVitap = new OracleVitapContext())
                {
                    contextVitap.Configuration.AutoDetectChangesEnabled = true;
                    var AcctLine = new PEGASYSPOACCT_FRM { PO_ID = exception.PO_ID };
                    contextVitap.PEGASYSPOACCT_FRM.Remove(AcctLine);

                    //Delete the office information in the Form tables
                    var OffcLine = new PEGASYSPOOFFC_FRM { PO_ID = exception.PO_ID };
                    contextVitap.PEGASYSPOOFFC_FRM.Remove(OffcLine);

                    //Insert Accounting Line from History data
                    var rtnPOAcctHist = GetPegasysPOAcctAmdHistByKey(exception.PO_ID);
                    contextVitap.PEGASYSPOACCT_FRM.Add(rtnPOAcctHist[0]);

                    //Insert Office Line from History data
                    var rtnPOOffcHist = GetPegasysPOOffcAmdHistByKey(exception.PO_ID);
                    PEGASYSPOOFFC_FRM poOffc = new PEGASYSPOOFFC_FRM();
                    poOffc.ADDR_CD = rtnPOOffcHist.FirstOrDefault().ADDR_CD;
                    poOffc.ADDR_CITY = rtnPOOffcHist.FirstOrDefault().ADDR_CITY;
                    poOffc.ADDR_CNTC = rtnPOOffcHist.FirstOrDefault().ADDR_CNTC;
                    poOffc.ADDR_CTRY = rtnPOOffcHist.FirstOrDefault().ADDR_CTRY;
                    poOffc.ADDR_EMAL = rtnPOOffcHist.FirstOrDefault().ADDR_EMAL;
                    poOffc.ADDR_FAX = rtnPOOffcHist.FirstOrDefault().ADDR_FAX;
                    poOffc.ADDR_L1 = rtnPOOffcHist.FirstOrDefault().ADDR_L1;
                    poOffc.ADDR_L2 = rtnPOOffcHist.FirstOrDefault().ADDR_L2;
                    poOffc.ADDR_L3 = rtnPOOffcHist.FirstOrDefault().ADDR_L3;
                    poOffc.ADDR_NM = rtnPOOffcHist.FirstOrDefault().ADDR_NM;
                    poOffc.ADDR_PHON = rtnPOOffcHist.FirstOrDefault().ADDR_PHON;
                    poOffc.ADDR_STATE = rtnPOOffcHist.FirstOrDefault().ADDR_STATE;
                    poOffc.ADDR_ZPCD = rtnPOOffcHist.FirstOrDefault().ADDR_ZPCD;
                    poOffc.OFFC_CD = rtnPOOffcHist.FirstOrDefault().OFFC_CD;
                    poOffc.OFFC_TYP = rtnPOOffcHist.FirstOrDefault().OFFC_TYP;
                    poOffc.PO_ID = rtnPOOffcHist.FirstOrDefault().PO_ID;
                    poOffc.PO_OFFC_ID = rtnPOOffcHist.FirstOrDefault().PO_OFFC_ID;

                    contextVitap.PEGASYSPOOFFC_FRM.Add(poOffc);
                    contextVitap.SaveChanges();

                    //Update PegasysPO amount                    
                    var rtnAcctAmount = contextVitap.PEGASYSPOACCT_FRM.GroupBy(temp => temp.AMOUNT).Select(temp => temp.Sum(Amt => Amt.AMOUNT));

                    var fieldsToUpdate = new List<string>
                    {
                        "AMOUNT"
                    };

                    UpdatePegasysPO(rtnPO, fieldsToUpdate);
                }
            }

        }

        public List<Data.PegasysEntities.MF_ADDR_LEVL_VEND> DunsList(string DunsValue)
        {
            return GetDunsList(DunsValue);
        }
    }
}
