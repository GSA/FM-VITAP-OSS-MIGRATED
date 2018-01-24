using System;
using VITAP.Controllers;
using VITAP.Data;
using VITAP.Data.Managers.Buttons;

namespace VITAP.SharedLogic.Buttons
{
    public class TransHistButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string Pdocno, string prepCode)
        {
            SetVariables(exc, Pdocno, prepCode, "TRANSHIST");
            var tc = new TransHistController();

            if (String.IsNullOrWhiteSpace(exception.ACT))
            {
                tc.Index(Pdocno, "PDOCNO", "", "", "", exception.ACT, Pdocno, "");
            }
            else
            {
                tc.Index(exception.ACT, "ACT", "", "", "", exception.ACT, Pdocno, "");
            }
            return true;
        }
    }
}