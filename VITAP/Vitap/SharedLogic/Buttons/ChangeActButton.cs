using System;
using System.Collections.Generic;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Managers.Buttons;

namespace VITAP.SharedLogic.Buttons
{
    public class ChangeActButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string prepCode, string strACT)
        {
            
            SetVariables(exc, prepCode, "CHANGEACT");
            var AskVal = "EMPTY";
            
            //Used thisform.r_act before
            if (!String.IsNullOrWhiteSpace(exception.ACT))
            {
                AskVal = exception.ACT;
            }

            //Update Exceptions Table.
            var fieldsToUpdate = new List<string>
            {
                "ACT"
            };

            exception.ACT = strACT;

            UpdateException(exception, fieldsToUpdate);

            //Update the related Pegasys frm table and transhist

            if (exception.ERR_CODE == "P200")
            {
                TransHistManager transHistManager = new TransHistManager();
                transHistManager.UpdateActByInvKeyId(exception.INV_KEY_ID, strACT);

                var rtnInv = GetPegasysInvoiceByKey(exception.INV_KEY_ID);

                if (rtnInv != null)
                {
                    fieldsToUpdate = new List<string>
                    {
                        "ACT"
                    };

                    rtnInv.ACT = strACT;

                    UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                }
            }

            //Insert a Transhist Record recording the change to Act number
            //Used thisform.r_act before
            var strCuffMemo = string.Format("ACT Number Changed from {0} to {1}.", exception.ACT, strACT);
            InsertTranshist(exception, "", strCuffMemo, "", prepCode);

            //Does not close out of the screen and enables the button
            //By returning true it should enable the button on the screen
            return true;
            //THISFORM.cmdPoRequest1.ENABLED = true;
        }
    }
}