using System;
using System.Linq;
using Kendo.Mvc.Extensions;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using System.Collections.Generic;

namespace VITAP.SharedLogic.Buttons
{
    public class ReferencingInvoicesButton : ButtonExceptionManager
    {
        public List<R200InvoiceModel> Intialize(string Pdocno)
        {
            var rtnMFIIInv = GetMFIIInvoiceData(Pdocno);

            return rtnMFIIInv;

        }
    }
}