using System.Collections.Generic;
using VITAP.Data.Models.Exceptions;
using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Data.Managers;
using System.Linq;

namespace VITAP.SharedLogic.Buttons
{
    public class R200SearchButton : ButtonExceptionManager
    {
        private AdoNetCalls ado = new AdoNetCalls();

        public List<X200Model> Initialize(string FieldName, string SearchValue, string poId)
        {
            var Act = "";
            var Pdocno = "";
            var Addr_nm = "";
            var Ctrc_num = "";
            var Dlvr_Ordr_Num = "";
            var Amount = "";
            var Titl = "";

            switch (FieldName)
            {
                case "ACT":
                    Act = SearchValue.Trim();
                    break;

                case "PDOCNO":

                    Pdocno = SearchValue.Trim();
                    break;

                case "VENDNAME":

                    Addr_nm = SearchValue.Trim();
                    break;

                case "CONTRACT":

                    Ctrc_num = SearchValue.Trim();
                    break;

                case "PONUMBER":

                    Dlvr_Ordr_Num = SearchValue.Trim();
                    break;

                case "AMOUNT":

                    Amount = SearchValue.Trim();
                    break;

                case "TITLE":

                    Titl = SearchValue.Trim();
                    break;
                default:
                    var X200Empty = new List<X200Model>();
                    return X200Empty;
            }

            var mgr = new ExceptionsManager();
            string X200IO = "", X200RO = "";
            var X200 = new List<X200Model>();

            X200IO = mgr.MFIO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 1);

            var rtnX200IO = mgr.RunAdoPegQuery(X200IO).ToList();
            if (rtnX200IO.Count() == 0)
            {
                X200IO = mgr.MFIO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 2);
            }
            X200RO = mgr.MFIO_RO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 1);
            
            var rtnX200RO = mgr.RunAdoPegQuery(X200RO).ToList();
            if (rtnX200RO.Count() == 0)
            {
                X200RO = mgr.MFIO_RO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 2);
            }
            X200 = mgr.RunAdoPegQuery(X200IO + " union " + X200RO).Distinct().ToList();
            
            if (X200.Count == 0)
            {
                X200 = mgr.VitapR200_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, poId);
            }

            foreach (var row in X200)
            {
                if (row.ACT.ReplaceNull("  ").Substring(0,2) == "1B")
                {
                    row.ACT = row.ACT.Substring(1);
                }
            }
            return X200.Distinct().ToList();
        }

        public List<X200Model> InitializeP200(string FieldName, string SearchValue, string poId)
        {
            var Act = "";
            var Pdocno = "";
            var Addr_nm = "";
            var Ctrc_num = "";
            var Dlvr_Ordr_Num = "";
            var Amount = "";
            var Titl = "";

            switch (FieldName.ToUpper())
            {
                case "ACT":
                    Act = SearchValue.Trim();
                    break;

                case "PDOCNO":

                    Pdocno = SearchValue.Trim();
                    break;

                case "VENDNAME":

                    Addr_nm = SearchValue.Trim();
                    break;

                case "CONTRACT":

                    Ctrc_num = SearchValue.Trim();
                    break;

                case "PONUMBER":

                    Dlvr_Ordr_Num = SearchValue.Trim();
                    break;

                case "AMOUNT":

                    Amount = SearchValue.Trim();
                    break;

                case "TITLE":

                    Titl = SearchValue.Trim();
                    break;
                default:
                    var X200Empty = new List<X200Model>();
                    return X200Empty;
            }

            var mgr = new ExceptionsManager();
            string X200IO = "", X200RO = "";
            var X200 = new List<X200Model>();

            //using (var contextPeg = new OraclePegasysContext())
            //{
                X200IO = mgr.MFIO_P200_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 1);
            //var rtnX200IO = contextPeg.Database.SqlQuery<X200Model>(X200IO).ToList();
            var rtnX200IO = mgr.RunAdoPegQuery(X200IO).ToList();
            if (rtnX200IO.Count() == 0)
                {
                    X200IO = mgr.MFIO_P200_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 2);
                //rtnX200IO = contextPeg.Database.SqlQuery<X200Model>(X200IO).ToList();
                rtnX200IO = mgr.RunAdoPegQuery(X200IO).ToList();
            }
                X200RO = mgr.MFIO_P200_RO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 1);
            //var rtnX200RO = contextPeg.Database.SqlQuery<X200Model>(X200RO).ToList();
            var rtnX200RO = mgr.RunAdoPegQuery(X200RO).ToList();
            if (rtnX200RO.Count() == 0)
                {
                    X200RO = mgr.MFIO_P200_RO_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, 2);
                //rtnX200RO = contextPeg.Database.SqlQuery<X200Model>(X200RO).ToList();
                rtnX200RO = mgr.RunAdoPegQuery(X200RO).ToList();
            }
                X200 = rtnX200IO.Concat(rtnX200RO).ToList();
            //}
            //var X200 = mgr.RunPegQuery(X200IO, X200RO);
            if (X200.Count == 0)
            {
                X200 = mgr.VitapR200_Query(Act, Pdocno, Addr_nm, Ctrc_num, Dlvr_Ordr_Num, Amount, Titl, poId);
            }

            foreach (var row in X200)
            {
                if (row.ACT.ReplaceNull("  ").Substring(0, 2) == "1B")
                {
                    row.ACT = row.ACT.Substring(1);
                }
            }
            return X200.Distinct(new P200SearchEqualityComparer()).ToList();
        }
    }
}