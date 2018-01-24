using System.Collections.Generic;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class UpdateAddNameButton : ButtonExceptionManager
    {
        public bool Initialize(NOTIFICATION thisnotification, string NewAddName)
        {
            if (thisnotification.ADDNAME != NewAddName)
            { 
                var fieldsToUpdate = new List<string>
                {
                    "ADDNAME"
                };

                thisnotification.ADDNAME = NewAddName;

                UpdateNotification(thisnotification, fieldsToUpdate);
            }

            return true;
        }
    }
}