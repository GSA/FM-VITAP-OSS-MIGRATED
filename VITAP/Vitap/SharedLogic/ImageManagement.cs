using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using VITAP.Data.Managers;
using VITAP.Data.Models.Exceptions;
using VITAP.Library;

namespace VITAP.SharedLogic
{
    public static class ImageManagement
    {
        public static ViewResult GetViewResult(Controller controller, string viewName, object viewModel)
        {
            ViewResult result = new ViewResult
            {
                ViewName = viewName,
                ViewData = new ViewDataDictionary
                {
                    Model = viewModel
                },
                TempData = controller.TempData,
            };
            result.ViewData.ModelState.Merge(controller.ModelState);
            // Copy ViewData/ViewBag from controller to ViewResult.
            foreach (var value in controller.ViewData)
                if (!result.ViewData.ContainsKey(value.Key))
                    result.ViewData[value.Key] = value.Value;
            return result;
        }

        public static ActionResult TiffViewResult(string Type, string TypeId, string FilePath, Controller controller, HttpResponseBase Response)
        {
            var model = new UserExceptionViewModel();
            model.TiffType = Type;
            model.TiffTypeId = TypeId;

            string extension = Path.GetExtension(FilePath);

            if (extension == ".tiff" || extension == ".tif")
            {
                TIF TheFile = new TIF(FilePath);
                model.TotalTIFPgs = TheFile.PageCount;
                TheFile.Dispose();

                var result = GetViewResult(controller, "UserException/TiffViewerModal", model);
                return result;
            }
            else
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(FilePath);
                var cd = new System.Net.Mime.ContentDisposition
                {
                    FileName = Path.GetFileName(FilePath),

                    // always prompt the user for downloading, set to true if you want 
                    // the browser to try to show the file inline
                    Inline = false,
                };
                Response.AppendHeader("Content-Disposition", cd.ToString());
                var result = new FileContentResult(fileBytes, MimeMapping.GetMimeMapping(FilePath));
                return result;
            }
        }

    }
}