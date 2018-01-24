using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class PrintButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            SetVariables(exc, prepCode, "PRINT");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public void FinishCode()
        {
            NewNotes();

            //The exception is already current upon entering the finishcode.
            var responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
            UpdateException(exception, "A", notes.returnVal7, exception.EX_MEMO2, responsenotes, "");
            notes.returnVal1 = "PRINT";
        }        
    }
}