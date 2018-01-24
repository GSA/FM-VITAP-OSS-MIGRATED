using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VITAP.Data;
using VITAP.Data.Managers;

namespace VITAP.SharedLogic
{
    public class ExceptionHelper
    {
        public EXCEPTION Skip(EXCEPTION exception, string prepCode = null)
        {
            var fieldsToUpdate = new List<string>
            {
                "ERR_RESPONSE",
                "OUT",
                "PREPCODE",
                "CLEARED_DATE"
            };

            exception.ERR_RESPONSE = "S";
            exception.OUT = "T";

            if (!string.IsNullOrEmpty(prepCode))
            {
                exception.PREPCODE = prepCode;
            }

            exception.CLEARED_DATE = DateTime.Now;

            var mgr = new ExceptionsManager();
            mgr.UpdateException(exception, fieldsToUpdate);

            return exception;
        }

        public EXCEPTION Accept(EXCEPTION exception)
        {
            //TOD: TBD
            return exception;
        }
    }
}