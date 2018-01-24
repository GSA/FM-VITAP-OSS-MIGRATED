using System;
using System.Linq;
using Kendo.Mvc.Extensions;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class POMatchP200Button : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode, string X200Pdocno)
        {
            SetVariables(exc, Pdocno, prepCode, "POMATCH");
            PDocNo = X200Pdocno;

            ButtonPushed = "NONE";

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public bool FinishCode1(string X200Pdocno, string X200Dscr, string Uidy)
        {
            var rtnMFIOItmz = GetMFIOItmzLnData(Uidy);

            var ItemizedPO = false;
            if (rtnMFIOItmz.Count > 0)
            {
                ItemizedPO = true;
            }
            return ItemizedPO;
        }

        public PEGASYSPO_FRM FinishCodeP2001B(string X200Pdocno, string X200Dscr)
        { 
            if (exception.ERR_CODE == "P200" && X200Dscr.Contains("VCPO") && X200Pdocno.Left(2) == "1B")
            {
                return GetPegasysPOFrmByKey(X200Pdocno);
            }

            return null;
        }
    }
}