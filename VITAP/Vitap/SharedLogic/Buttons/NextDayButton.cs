using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class NextDayButton : ButtonExceptionManager
    {
       
        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            Caption = "NEXTDAY";
            exception = exc;
            PrepCode = prepCode;

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

        public void FinishCode()
        {
            NewNotes();

            var faxNotes = notes.returnVal7;
            var responseNotes = exception.RESPONSENOTES + "\r\n" + NewNote + " - Next Day.', ";
            ExceptionsManager manager = new ExceptionsManager();
            manager.NextDayUpdate(exception.EX_ID, PrepCode, faxNotes, responseNotes);
        }
    }
}