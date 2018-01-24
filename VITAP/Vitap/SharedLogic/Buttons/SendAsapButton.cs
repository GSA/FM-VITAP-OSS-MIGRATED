using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;
using System;
using System.Collections.Generic;
using VITAP.Library.Strings;

namespace VITAP.SharedLogic.Buttons
{
    public class SendAsapButton : ButtonExceptionManager
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

        public string FinishCode1(string faxNotes, string faxNotes2, string newAddrName)
        {
            var strCuffMemo = NewNote.ReplaceApostrophes();
            var allProcess = "Send the FAX ASAP";

            if (exception.ERR_CODE == "P040" && exception.ERR_CODE == "P041" || exception.ERR_CODE == "P042")
            {
                theMsg = "";
                var rtnNot = GetNotificationByExId(exception.EX_ID);
                //string lcSql = "select * from notifications where ex_id = '" + exception.EX_ID + "'";

                if (rtnNot.Count == 0) // _TALLY = 0
                {
                    theMsg = "Notification not found.  Please Recycle";
                }

                //Uses Address Name from screen...Needs to be passed in now
                if (String.IsNullOrWhiteSpace(theMsg))
                {
                    if (rtnNot[0].ADDNAME.ReplaceNull("") != newAddrName)
                    {
                        //Need user input to determine whether to continue
                        theMsg = "Additional Contact Names have been changed.  Do you want to update?";
                        //a = MESSAGEBOX("Additional Contact Names have been changed.  Do you want to update?", 36, "Update names")
                    }
                }
                return theMsg;
            }
            else
            {
                //FOR THE NOTIFICATION EXCEPTIONS ONLY!!!
                if (faxNotes != faxNotes2)
                {
                    exception.FAXNOTES = faxNotes;
                    UpdateNotificationByEXIDToPending(exception.EX_ID);

                    allProcess = "Change Notification FaxNote";
                    InsertTranshist(exception, "", faxNotes, allProcess, PrepCode);
                }
                else
                {
                    UpdateNotificationByEXIDToPending(exception.EX_ID);
                }
                allProcess = "Send the FAX ASAP";
                InsertTranshist(exception, "", faxNotes, allProcess, PrepCode);

                notes.returnVal1 = "SENDASAP";
            }
            return "";
        }

        public string FinishCode2(string faxNotes, string faxNotes2, string newAddrName, bool Continue)
        {
            var strCuffMemo = NewNote.ReplaceApostrophes();
            var allProcess = "";

            if (exception.ERR_CODE == "P040" && exception.ERR_CODE == "P041" || exception.ERR_CODE == "P042")
            {
                var rtnNot = GetNotificationByExId(exception.EX_ID);

                var fieldsToUpdate = new List<string>
                {
                    "ADDNAME"
                };

                foreach (var row in rtnNot)
                {
                    row.ADDNAME = newAddrName;
                    UpdateNotification(row, fieldsToUpdate);
                }

                faxNotes = faxNotes.ReplaceApostrophes();
                faxNotes2 = faxNotes2.ReplaceApostrophes();

                NewNote = DateTime.Now.ShortDate() + " " + PrepCode + " ";
                NewNote = NewNote + faxNotes + "\r\n";

                if (faxNotes != faxNotes2)
                {
                    if (Continue)
                    { 
                        fieldsToUpdate = new List<string>
                        {
                            "FAXNOTES"
                        };

                        foreach (var row in rtnNot)
                        {
                            exception.FAXNOTES = faxNotes;
                            UpdateException(exception, fieldsToUpdate);

                            UpdateNotificationByNotKeyIdToPending(row.NOT_KEY_ID, faxNotes);
                        }

                        strCuffMemo = NewNote.ReplaceApostrophes();
                        allProcess = "Change Notification Fax Note";
                        InsertTranshist(exception, "", strCuffMemo, allProcess, PrepCode);
                    }
                    else
                    {
                        foreach (var row in rtnNot)
                        {
                            UpdateNotificationByNotKeyIdToPending(row.NOT_KEY_ID, "");
                        }
                    }
                }
                else
                {
                    foreach (var row in rtnNot)
                    {
                        UpdateNotificationByNotKeyIdToPending(row.NOT_KEY_ID, "");
                    }
                }

                strCuffMemo = NewNote.ReplaceApostrophes();
                allProcess = "Send the FAX ASAP";
                InsertTranshist(exception, "", strCuffMemo, allProcess, PrepCode);
            }

            return "";
        }

        public void FinishCodeP04X1(string addNewName, string Act, string prepCode)
        {
            var rtnNot = GetNotificationByExId(exception.EX_ID);

            foreach (var row in rtnNot)
            {
                if (row.CONTACT_ID == null)
                {
                    if (row.ACT == Act)
                    {
                        var fieldsToUpdate = new List<string>
                        {
                            "ADDNAME"
                        };
                        row.ADDNAME = addNewName;
                        UpdateNotification(row, fieldsToUpdate);
                    }
                }
                else
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "ADDNAME"
                    };
                    row.ADDNAME = addNewName;
                    UpdateNotification(row, fieldsToUpdate);
                }
            }
        }

        public void FinishCodeP04X2(string faxNotes2, string Act, string prepCode)
        {
            var rtnNot = GetNotificationByExId(exception.EX_ID);

            var fieldsToUpdate = new List<string>
            {
                "FAXNOTES"
            };
            exception.FAXNOTES = faxNotes2;
            UpdateException(exception, fieldsToUpdate);

            foreach (var row in rtnNot)
            {
                if (row.ACT == Act && row.CONTACT_ID != null)
                {
                    fieldsToUpdate = new List<string> {
                        "STATUS",
                        "FAX_NOTES"
                    };
                    row.STATUS = DocStatus.Pending;
                    row.FAX_NOTES = faxNotes2;
                    UpdateNotification(row, fieldsToUpdate);
                }
                else
                {
                    UpdateNotificationByNotKeyIdToPending(row.NOT_KEY_ID, faxNotes2);
                }
            }
            var TH = new TRANSHIST { ACT = Act, PDOCNO = PDocNo, PO_ID = exception.PO_ID, RR_ID = exception.RR_ID, INV_KEY_ID = exception.INV_KEY_ID, TRANSDATE = DateTime.Now,
                PREPCODE = prepCode, ERR_CODE = exception.ERR_CODE, EX_ID = exception.EX_ID, CUFF_MEMO = faxNotes2, ALLPROCESS = "Change Notification Fax Note",
                CLEARED_DATE = DateTime.Now, AE_ID = exception.AE_ID };
            InsertTranshist(TH);

        }

        public void FinishCodeP04X3(string faxNotes2, string Act)
        {
            var rtnNot = GetNotificationByExId(exception.EX_ID);
            if (rtnNot[0].CONTACT_ID == null)
            {
                foreach (var row in rtnNot)
                {
                    if (row.ACT == Act && row.CONTACT_ID != null)
                    {
                        var fieldsToUpdate = new List<string> {
                        "STATUS"
                    };
                        row.STATUS = DocStatus.Pending;
                        row.FAX_NOTES = faxNotes2;
                        UpdateNotification(row, fieldsToUpdate);
                    }
                    else
                    {
                        UpdateNotificationByNotKeyIdToPending(row.NOT_KEY_ID, faxNotes2);
                    }
                }
            }
            else
            {
                foreach (var row in rtnNot)
                {
                    var fieldsToUpdate = new List<string>
                    {
                        "STATUS"
                    };
                    row.STATUS = "Pending";
                    UpdateNotification(row, fieldsToUpdate);
                }
            }
        }
    }
}
