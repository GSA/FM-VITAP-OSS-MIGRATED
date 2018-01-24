using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using GSA.R7BD.Utility;
using VITAP.Library.Strings;

namespace VITAP.Utilities
{
    public class Logging
    {
        public static void AddWebError(string ErrDesc, [CallerFilePath] string ComponentName = "", [CallerMemberName] string MethodName = "")
        {
            EventLog.AddWebErrors(Login.APPNAME, ComponentName, MethodName, ErrDesc);
        }
    }
}