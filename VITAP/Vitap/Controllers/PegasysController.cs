using System.Web.Mvc;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;

using VITAP.Data;
using VITAP.Data.Models.Pegasys;

namespace VITAP.Controllers
{
    public class PegasysController : VitapBaseController
    {
        [HttpGet]
        public ActionResult PurchaseOrderEdi()
        {
            var model = new PegasysPOEdiModel(true);
            return View("PurchaseOrderEdi", model);
        }

        [HttpPost]
        public ActionResult GetMemos([DataSourceRequest]DataSourceRequest request, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Json(null, JsonRequestBehavior.AllowGet);

            var mgr = new PegasysInvoiceManager();
            var efMemos = mgr.GetPegasysMemos(id);
            var vm = new PegasysMemoModel(efMemos);

            var result = vm.Memos.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}