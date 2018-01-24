using System;
using System.Collections.Generic;
using VITAP.Controllers;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Models.Exceptions;
using VITAP.Data;

namespace VITAP.SharedLogic.Buttons
{
    public class VendorExceptionSearchButton : ButtonExceptionManager
    {
        /// <summary>
        /// Handles Search 
        /// Need to handle a dynamic query
        /// Need to refresh the Vendor Search Grid
        /// Determines whether to enable/disable Accept button
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="prepCode"></param>
        /// <param name="SearchType1"></param>
        /// <param name="SearchType2"></param>
        /// <param name="SearchValue1"></param>
        /// <param name="SearchValue2"></param>
        /// <returns></returns>
        public VendorModel Initialize(EXCEPTION exc, string prepCode, string SearchType1, string SearchType2, string SearchValue1, string SearchValue2, string SearchVendCode, string SearchAddr_Cd)
        {
            SetVariables(exc, prepCode, "SEARCH");

            var whereClause = "";

            if (!String.IsNullOrWhiteSpace(SearchValue1))
            {
                switch (SearchType1)
                {
                    case "VendName":
                        whereClause = " and d.addr_nm LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "VendCode":
                        whereClause = " and c.uidy LIKE '&3903&" + SearchValue1.Trim() + "%'";
                        break;

                    case "DunsNumber":
                        whereClause = " and b.duns_num LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "Addr1":
                        whereClause = " and d.addr_l1 LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "Addr2":
                        whereClause = " and d.addr_l2 LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "City":
                        whereClause = " and d.addr_city LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "Zip":
                        whereClause = " and d.addr_zpcd LIKE '" + SearchValue1.Trim() + "%'";
                        break;

                    case "AddrCode":
                        whereClause = " and b.addr_cd LIKE '" + SearchValue1.Trim() + "%'";
                        break;
                }
            }

            if (!String.IsNullOrWhiteSpace(SearchValue2))
            {
                switch (SearchType2)
                {
                    case "VendName":
                        whereClause += " and d.addr_nm LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "VendCode":
                        whereClause += " and c.uidy LIKE '&3903&" + SearchValue2.Trim() + "%'";
                        break;

                    case "DunsNumber":
                        whereClause += " and b.duns_num LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "Addr1":
                        whereClause += " and d.addr_l1 LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "Addr2":
                        whereClause += " and d.addr_l2 LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "City":
                        whereClause += " and d.addr_city LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "Zip":
                        whereClause += " and d.addr_zpcd LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "AddrCode":
                        whereClause += " and b.addr_cd LIKE '" + SearchValue2.Trim() + "%'";
                        break;

                    case "AccountNumber":
                        whereClause += " and b.cust_acct_num LIKE '" + SearchValue2.Trim() + "%'";
                        break;
                }
            }


            whereClause = whereClause.Substring(5);

            //Duns Plus 4 information will be stored in edi_id.Previously it was considered as part of duns_num field.
            //Moved duns / duns plus 4 columns after the zip code information.
            //Remove Prevent New Spending Flag from the query -pvnt_new_spng = 'F'
            var rtnVendor = GetVendorData(whereClause);
             
            return rtnVendor;
        }
    }
}