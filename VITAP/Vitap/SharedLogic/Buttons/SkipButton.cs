using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using System;

namespace VITAP.SharedLogic.Buttons
{
    public class SkipButton : ButtonExceptionManager
    {
        /// <summary>
        /// Skip Button Checks P and 230 exceptions 
        /// It opens the notes screen
        /// It checks which button is pushed
        /// Adds a Transhist record
        /// Updates the Exception record
        /// Repulled the exception before, but it updates the existing one, so it's not necessary
        /// </summary>
        public string Initialize(EXCEPTION exc, string pdocno, string ex_id, string prepCode)
        {
            //Need to see if variables need to be set after the exceptions values are updated
            SetVariables(exc, prepCode, "SKIP");

            if (!Check_P_Exceptions()) { return theMsg; }
            if (!Check_230_Exceptions()) { return theMsg; }
            return "";
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode(string prepCode)
        {
            PrepCode = prepCode;

            //Add a transhist record
            if (exception != null)
            {
                if (exception.ERR_RESPONSE == "S")
                {
                    var strCuffMemo = "SKIP AGAIN - " + "\r\n" + notes.returnVal7.ReplaceNull("").ReplaceApostrophes();
                    InsertTranshist(exception, "S", strCuffMemo, "SKIP AGAIN NOTE", prepCode);
                }
            }

            //Update Exception to "S"
            var responseNotes = exception.RESPONSENOTES.ReplaceNull("") + "\r\n" + DateTime.Now.ToString() + " " + prepCode + " " + 
                notes.returnVal2.ReplaceNull("") + "/" + notes.returnVal3.ReplaceNull("").ReplaceApostrophes();
            UpdateExceptionForSkip(exception, "S", notes.returnVal7.ReplaceNull("").ReplaceApostrophes(), notes.returnVal3 + exception.EX_MEMO2, responseNotes, exception.ALLPROCESS);

            //Add notification
            switch (exception.ERR_CODE)
            {
                case "U047":
                    //Pulled the exception again in FoxPro, which is already updated here, but may be needed on the form still
                    //Normally the form closes and goes back to the exceptions search screen though
                    break;
            }
            Caption = "SKIP";
        }
    }
}
