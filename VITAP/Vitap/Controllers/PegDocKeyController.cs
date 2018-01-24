using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using VITAP.Data;
using VITAP.Data.Managers;
using VITAP.Data.Models;
using System;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using VITAP.Library.Strings;


namespace VITAP.Controllers
{
    public class PegDocKeyController : VitapBaseController
    {
        [HttpGet]
        public ActionResult Index()
        {

            var roleList = Session[SessionKey.RoleModel] as RoleListModel;
            if (roleList.HasExceptionsRole == false)
            {
                RedirectToAction("Index", "Home");
            }

            List<object> myModel = new List<object>();
            
            var pkm = GetInvoiceToBeKeyedByAssServ(); 
            myModel.Add(pkm);

            
            var pim = GetInvoiceToBeIndexedByAssServ(); 
            myModel.Add(pim);

            return View(myModel);
        }

        private List<PegasysKeyedIndexedViewModel> GetInvoiceToBeKeyedByAssServ()
        {
            var mgr = new PegasysInvoiceManager();
            List<PegasysKeyedIndexedViewModel> model = new List<PegasysKeyedIndexedViewModel>();
            
           model = mgr.GetPegasysInvoiceToBeKeyed(AssignSrv);

            return model;
        }

        

            private List<PegasysKeyedIndexedViewModel> GetInvoiceToBeIndexedByAssServ()
        {
            var mgr = new PegasysInvoiceManager();
            List<PegasysKeyedIndexedViewModel> model = new List<PegasysKeyedIndexedViewModel>();

            model = mgr.GetPegasysInvoiceToBeIndexed(AssignSrv);

            return model;
        }
    }
}
