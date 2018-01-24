using VITAP.Controllers;
using VITAP.Data;
using VITAP.Data.Models.Exceptions;

namespace VITAP.SharedLogic.Buttons
{
    public class OpenNotesScreenIgnore
    {
        public string Tin = "", VendNo = "";
        public bool Tin_As_Key = false;

        /// <summary>
        /// Opens the Notes screen for user input
        /// Need to determine ReturnAction and ReturnController
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="ButtonType"></param>
        /// <returns></returns>
        public NotesViewModel Open(EXCEPTION exception, string ButtonType)
        {
            //Runs on all Buttons except DON'T ACCRUAL
            string Val1 = exception.ACT, Val2 = exception.EX_ID, Val3 = exception.PDOCNO;
            ExceptionsController ec = new ExceptionsController();
            NotesViewModel nvm = new NotesViewModel();
            switch (exception.ERR_CODE)
            {
                case "U047":
                    bool p_tin_as_key = Tin_As_Key; // r_tin_as_key;

                    if (Tin_As_Key == true)
                    {
                        nvm.Tin = Tin;
                    }
                    else
                    {
                        nvm.VendNo = VendNo;
                    }
                    break;

                case "P040":
                    nvm.Act = exception.ACT;
                    break;

            }
            //Need to determine ReturnAction and ReturnController values
            ec.GetNotesView(exception.ERR_CODE, ButtonType, Val1, Val2, Val3, "ACTIONRESULT", "EXCEPTION");
            ec.FinishNotesVM(nvm);
            return nvm;
        }
    }
}