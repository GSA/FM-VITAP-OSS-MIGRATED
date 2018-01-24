using VITAP.Controllers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using System.Collections.Generic;

namespace VITAP.SharedLogic.Buttons
{
    public class ShowUsedInvoicesButton : ButtonExceptionManager
    {
        public bool Initialize(EXCEPTION exc, string PrepCode, PEGASYSINVOICE InvQuery)
        {
            SetVariables(exception, PrepCode, "SHOWUSEDINVOICES");

            return true;
        }

        /// <summary>
        /// Needs to call the Pick list and pass in the table/list to it
        /// </summary>
        /// <param name="InvQuery"></param>
        public List<PEGASYSINVOICE> FinishCode(PEGASYSINVOICE InvQuery)
        {
            var rtnInv = GetPegasysInvoice(InvQuery.PDOCNOPO, InvQuery.VEND_CD);

            //picnt = _TALLY
            var rtnMFII = GetMFIIDataByVendCd(InvQuery.VEND_CD);
            
            foreach (var row in rtnMFII)
            {
                var pi = new PEGASYSINVOICE();
                pi.INVOICE = row.INVC_NUM;
                pi.ACT = row.XSYS_DOC_NUM.Left(8);
                pi.INV_STATUS = row.DOC_STUS.Left(10);
                pi.INV_KEY_ID = row.DOC_NUM;
                pi.AMOUNT = row.INVD_TA;
                pi.INVDATE = row.INVC_DT;
                pi.ERR_CODE = "NONE";
                pi.PDOCNOPO = "";

                rtnInv.Add(pi);                
            }
            rtnInv.Sort();

            return rtnInv;
        }

    }
}