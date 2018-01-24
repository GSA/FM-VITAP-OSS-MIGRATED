using System;
using System.Reflection;
using System.Web.Mvc;

namespace VITAP.Utilities.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SubmitButtonAttribute : ActionMethodSelectorAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public override bool IsValidForRequest(ControllerContext controllerContext, MethodInfo methodInfo)
        {
            var req = controllerContext.RequestContext.HttpContext.Request;
            if (Value == null)
                return req.Form[Name] != null;
            return req.Form[this.Name] == this.Value;
        }
    }
}