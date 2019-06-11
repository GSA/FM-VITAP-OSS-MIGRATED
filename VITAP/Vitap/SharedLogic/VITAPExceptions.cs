using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VITAP.Data.PegasysEntities;
using VITAP.Data.Models;
using VITAP.Data.Managers.Buttons;
using VITAP.Data.Managers;
using System.Data.Entity;

namespace VITAP.Data.Managers
{
    //using System;
    //using System.Collections.Generic;
    //using System.Linq;
    //using System.Text;
    //using System.Threading.Tasks;
    //using System.Data.OleDb;
    //using System.Data;

    public class VITAPExceptions : ButtonExceptionManager
    {
        string act = String.Empty, ex_fund = String.Empty, pdocnopo = String.Empty, orgcode = String.Empty, ba = String.Empty, vendname = String.Empty, 
            podoctype = String.Empty, pegsystem = String.Empty, allprocess = String.Empty;
        string po_id = String.Empty, rr_id = String.Empty, ae_id = String.Empty, inv_key_id = String.Empty, pa_id = String.Empty, not_key_id = String.Empty, 
            prepcode = String.Empty, imagebatch = String.Empty;
        string err_code = String.Empty, err_response = String.Empty, partial_match_vendno = String.Empty, ex_memo = String.Empty, ex_memo2 = String.Empty, 
            updstatus = "T", ex_id = String.Empty;
        double misc_amount = 0, pay_amount = 0, rramount = 0, poamount = 0;
        int netdays = 0;
        string updinvstatus = "F", updpostatus = "F", updrrstatus = "F", updaestatus = "F";
        string BCEImageBatchString = String.Empty, BCFImageBatchString = String.Empty;

        DateTime? hold_date;

        /// <summary>
        /// Set/Get the Out value
        /// </summary>

        #region AssignProperties
        public DateTime? Hold_date
        {
            get { return hold_date; }
            set { hold_date = value; }
        }

        public string ActNum
        {
            get { return act; }
            set { act = value; }
        }

        public string Ae_id
        {
            get { return ae_id; }
            set
            {
                ae_id = value;
            }
        }

        public string Pdocnopo
        {
            get { return pdocnopo; }
            set { pdocnopo = value; }
        }

        public string PoDocType
        {
            get { return podoctype; }
            set { podoctype = value; }
        }

        public String PrepCode
        {
            get { return prepcode; }
            set { prepcode = value; }
        }

        public string Err_code
        {
            get { return err_code; }
            set { err_code = value; }
        }

        public string Ex_fund
        {
            get { return ex_fund; }
            set { ex_fund = value; }
        }

        public string Ex_memo
        {
            get { return ex_memo; }
            set { ex_memo = value; }
        }

        public string Ex_memo2
        {
            get { return ex_memo2; }
            set { ex_memo2 = value; }
        }

        public string Rr_id
        {
            get { return rr_id; }
            set
            {
                rr_id = value;
            }
        }

        public string Po_id
        {
            get { return po_id; }
            set
            {
                po_id = value;
            }
        }

        public string Inv_key_id
        {
            get { return inv_key_id; }
            set
            {
                inv_key_id = value;
            }
        }

        public string Orgcode
        {
            get { return orgcode; }
            set { orgcode = value; }
        }

        public string Ba
        {
            get { return ba; }
            set { ba = value; }
        }

        public string Vendname
        {
            get { return vendname; }
            set { vendname = value; }
        }


        public string Updinvstatus
        {
            get { return updinvstatus; }
            set { updinvstatus = value; }
        }

        public string Allprocess
        {
            get { return allprocess; }
            set { allprocess = value; }
        }

        public string Err_response
        {
            get
            {
                return err_response;
            }

            set
            {
                err_response = value;
            }
        }

        public double Poamount
        {
            get
            {
                return poamount;
            }

            set
            {
                poamount = value;
            }
        }

        public string Updstatus
        {
            get
            {
                return updstatus;
            }

            set
            {
                updstatus = value;
            }
        }

        public string Ex_id
        {
            get
            {
                return ex_id;
            }

            set
            {
                ex_id = value;
            }
        }

        public string Updpostatus
        {
            get
            {
                return updpostatus;
            }

            set
            {
                updpostatus = value;
            }
        }

        public string Updrrstatus
        {
            get
            {
                return updrrstatus;
            }

            set
            {
                updrrstatus = value;
            }
        }

        public string Updaestatus
        {
            get
            {
                return updaestatus;
            }

            set
            {
                updaestatus = value;
            }
        }
        #endregion

        /// <summary>
        /// The main job to add an exception (after setting some variables)
        /// </summary>
        /// <returns></returns>
        public bool AddException()
        {
            //Lookup the Exception Memo if empty (fox table now)
            //lookup the poamount if 0 and have po_id
            //lookup the rramount if 0 and have rr_id

            if (err_code == "U063" || err_code == "U065" || err_code == "U066")
            {
                // indicator for returning status to "Z" (or whatever) when exception is cleared.
                updstatus = "F";
            }

            // Get pdocno from po_id if available.
            if (String.IsNullOrWhiteSpace(pdocnopo) && !String.IsNullOrWhiteSpace(po_id))
            {
                pdocnopo = po_id;
            }

            //pull imagebatch, vendname and pdocnopo from invoice if inv_key_id
            if (inv_key_id != String.Empty)
            {
                var rtnInv = GetPegasysInvoiceByKey(inv_key_id);

                if (rtnInv != null)
                {
                    if (String.IsNullOrWhiteSpace(pdocnopo))
                    {
                        pdocnopo = rtnInv.PDOCNOPO;
                    }
                    if (String.IsNullOrWhiteSpace(imagebatch))
                    {
                        imagebatch = rtnInv.IMAGEBATCH;
                    }
                    if (String.IsNullOrWhiteSpace(vendname))
                    {
                        vendname = rtnInv.VENDNAME;
                    }
                }
            } //end ! empty inv_key_id

            //remove & from pdocnopo if needed
            if (pdocnopo.IndexOf("&") > -1)
            {
                pdocnopo = pdocnopo.Substring(0, pdocnopo.IndexOf("&") + 1);
            }

            using (var contextVitap = new OracleVitapContext())
            {
                contextVitap.Configuration.AutoDetectChangesEnabled = true;

                using (var contextPeg = new PegasysEntities.OraclePegasysContext())
                {
                    //add fund, orgcode and BA information to the exception record
                    if (String.IsNullOrEmpty(orgcode) && !String.IsNullOrEmpty(pdocnopo))
                    {
                        bool inKeys = false;        // Are values stored in keys?

                        // Start with PEGASYSPOACCT_FRM
                        var poAcctRows = contextVitap.PEGASYSPOACCT_FRM.Where(x => x.PO_ID.Contains(pdocnopo))
                                         .Select(x => new { Fund = x.FUND, OrgCode = x.ORGCODE, Ba = x.BA_PROG, Lnum = x.LNUM }).OrderBy(x => x.Lnum).ToList();

                        // Check MF_IO_HDAL with pdocnopo.
                        if (poAcctRows == null || poAcctRows.Count() == 0) {
                            string pdocLookup = "&1423&1609&" + pdocnopo.Left(2) + '&' + pdocnopo + '&';
                            poAcctRows = contextPeg.MF_IO_HDAL.Where(x => x.PARN_OF_LINE_ID.Contains(pdocLookup))
                                             .Select(x => new { Fund = x.FUND_ID, OrgCode = x.ORGN_ID, Ba = x.PROG_ID, Lnum = x.LNUM }).OrderBy(x => x.Lnum).ToList();
                            inKeys = poAcctRows != null;
                        }

                        // Check MF_IO_HDAL with act.
                        if ((poAcctRows == null || poAcctRows.Count() == 0) && !String.IsNullOrEmpty(act))
                        {
                            var actTrim = act.Trim();
                            var actLookup = contextPeg.MF_IO.Where(y => y.DTYP_ID != "&1609&4B" && y.XSYS_DOC_NUM == actTrim && y.DOC_STUS != "CANCELLED").FirstOrDefault().UIDY;

                            if (!string.IsNullOrEmpty(actLookup)) {
                                poAcctRows = contextPeg.MF_IO_HDAL.Where(x => x.PARN_OF_LINE_ID.Contains(actLookup))
                                                 .Select(x => new { Fund = x.FUND_ID, OrgCode = x.ORGN_ID, Ba = x.PROG_ID, Lnum = x.LNUM }).OrderBy(x => x.Lnum).ToList();
                                inKeys = poAcctRows != null;
                            }
                        }

                        if (poAcctRows != null && poAcctRows.Count() > 0) {
                            var poAcctRow = poAcctRows.FirstOrDefault();

                            // Pull values out of keys.
                            if (inKeys) {
                                poAcctRow = new { Fund = poAcctRow.Fund.GetPegIdPart(2), OrgCode = poAcctRow.OrgCode.GetPegIdPart(2), Ba = poAcctRow.Ba.GetPegIdPart(2), Lnum = poAcctRow.Lnum };
                            }

                            orgcode = poAcctRow.OrgCode == null ? "" : poAcctRow.OrgCode.Trim();
                            ba = poAcctRow.Ba == null ? "" : poAcctRow.Ba.Trim().Length >= 2 ? poAcctRow.Ba.Trim().Right(2) : poAcctRow.Ba.Trim();
                            if (String.IsNullOrEmpty(ex_fund)) {
                                var fund442 = poAcctRows.Where(x => x.Fund.Contains("442")).FirstOrDefault();
                                if (fund442 != null) {
                                    ex_fund = fund442.Fund == null ? "" : fund442.Fund.Trim();
                                }
                            }
                        }
                    }

                    //ex_fund
                    if (String.IsNullOrWhiteSpace(ex_fund))
                    {
                        if (rr_id.ReplaceNull("").Length >= 2)
                        {
                            if (rr_id.Substring(0, 2) == "HC" || rr_id.Substring(0, 2) == "2C")
                                ex_fund = "299X";
                        }
                        if (ae_id.ReplaceNull("").Length >= 2)
                        {
                            if (ae_id.Substring(0, 2) == "HE" || ae_id.Substring(0, 2) == "2E")
                                ex_fund = "299X";
                        }
                        if (pdocnopo.ReplaceNull("").Length >= 2)
                        {
                            if (pdocnopo.Substring(0, 2) == "HB" || pdocnopo.Substring(0, 2) == "2B")
                                ex_fund = "299X";
                        }
                        if (ex_fund == String.Empty && pdocnopo != String.Empty)
                        {
                            if (pdocnopo.Substring(0, 2) == "1B" || pdocnopo.Substring(0, 2) == "PJ" ||
                                pdocnopo.Substring(0, 2) == "PS" || pdocnopo.Substring(0, 2) == "RO" || pdocnopo.Substring(0, 2) == "EP")
                                ex_fund = "192X";
                        }
                        if (ex_fund == String.Empty && pdocnopo != String.Empty)
                        {
                            List<String> rtnPOFund = contextPeg.MF_IO_HDAL.Where(x => x.PARN_OF_LINE_ID == "&1423&1609&" + pdocnopo.Substring(0, 2) + "&" + pdocnopo + "&")
                                                     .Select(x => x.FUND_ID).ToList();
                            
                            if (rtnPOFund.Count == 0)
                            {
                                // Check Itemized PO
                                rtnPOFund = (from actg in contextPeg.MF_ORD_ACTG_LN
                                             join itmz in contextPeg.MF_IO_ITMZ_LN
                                                on actg.PARN_OF_LINE_ID equals itmz.UIDY
                                             where itmz.PARN_OF_LINE_ID == "&1423&1609&" + pdocnopo.Substring(0, 2) + "&" + pdocnopo + "&"
                                             select actg.FUND_ID).ToList();
                            }
                            if (rtnPOFund.Count > 0)
                            {
                                // If any PO accounting line has a fund 442 line, then mark the document as 442.
                                bool b442 = false;
                                for (int row = 0; row < rtnPOFund.Count; row++)
                                {
                                    if (rtnPOFund[row].Contains("442"))
                                    {
                                        b442 = true;
                                    }
                                }

                                if (b442)
                                {
                                    ex_fund = "442";
                                }
                                else
                                {
                                    string[] fundid = rtnPOFund[0].Split('&');
                                    ex_fund = fundid[1];
                                }
                            }
                        }
                    } //end ex_fund



                    //lookup pdocnopo if only act (ONLY workd for OLD orders that were processed in VITAP)
                    if (pdocnopo == String.Empty & !String.IsNullOrEmpty(act))
                    {
                        List<string> rtnPegPO = (from temp in contextVitap.PEGASYSPOes
                                        where temp.ACT == act
                                        select temp.PO_ID).ToList();

                        if (rtnPegPO.Count > 0)
                        {
                            po_id = rtnPegPO[0];
                        }
                    }

                    //lookup pegsystem
                    if (pegsystem == String.Empty)
                    {
                        if (ex_fund == "442")
                            pegsystem = "ARRA";
                        if (pa_id.ReplaceNull("").Length >= 2)
                        {
                            if (pa_id.Substring(0, 2) == "UD")
                                pegsystem = "UPPS";
                        }
                        if (ae_id.ReplaceNull("").Length >= 2)
                        {
                            if (ae_id.Substring(0, 2) == "UE")
                                pegsystem = "UPPS";
                            if (ae_id.Substring(0, 2) == "HE")
                                pegsystem = "TOPS";
                            if (ae_id.Substring(0, 2) == "2E")
                                pegsystem = "FTS";
                        }
                        if (po_id.ReplaceNull("").Length >= 2)
                        {
                            if (po_id.Substring(0, 2) == "HB")
                                pegsystem = "TOPS";
                            if (po_id.Substring(0, 2) == "2B")
                                pegsystem = "FTS";
                        }
                        if (rr_id.ReplaceNull("").Length >= 2)
                        {
                            if (rr_id.Substring(0, 2) == "HC")
                                pegsystem = "TOPS";
                            if (rr_id.Substring(0, 2) == "2C")
                                pegsystem = "FTS";
                        }
                        if (pdocnopo.ReplaceNull("").Length >= 2)
                        {
                            if (pdocnopo.Substring(0, 2) == "HB")
                                pegsystem = "TOPS";
                            if (pdocnopo.Substring(0, 2) == "2B")
                                pegsystem = "FTS";
                            if (pdocnopo.Substring(0, 2) == "RO")
                                pegsystem = "VCPO";
                            if (pdocnopo.Substring(0, 2) == "1B" || pdocnopo.Substring(0, 2) == "PJ" || pdocnopo.Substring(0, 2) == "EP" ||
                            pdocnopo.Substring(0, 2) == "PP" || pdocnopo.Substring(0, 2) == "PS" || pdocnopo.Substring(0, 2) == "PN")
                                pegsystem = "PBS";
                        }
                        if (ex_fund.ReplaceNull("").Length >= 3)
                        {
                            if (ex_fund.Substring(0, 3) == "299" || ex_fund.Substring(0, 3) == "285" || ex_fund.Substring(0, 3) == "295")
                                pegsystem = "FTS";
                            if (err_code.Substring(0, 1) == "R")
                                pegsystem = "VCPO";
                            if (ex_fund.Substring(0, 3) == "192")
                                pegsystem = "PBS";
                        }
                        if (pegsystem == String.Empty && imagebatch != String.Empty)
                        {
                            if (BCEImageBatchString == String.Empty || BCFImageBatchString == String.Empty)
                            {
                                ConfigManager cfg = new ConfigManager();
                                BCEImageBatchString = cfg.GetConfigValue("VITAP", "BCE_INVOICE_IMAGEBATCH_PREFIXES");
                                BCFImageBatchString = cfg.GetConfigValue("VITAP", "BCF_INVOICE_IMAGEBATCH_PREFIXES");

                            } // end empty image strings

                            if (imagebatch.ReplaceNull("").Length >= 3 && BCEImageBatchString != String.Empty &&
                                BCEImageBatchString.IndexOf(imagebatch.Substring(0, 3)) != -1)
                                pegsystem = "06";
                            if (imagebatch.ReplaceNull("").Length >= 2 && (imagebatch.Substring(0, 2) == "R6" || imagebatch.Substring(0, 2) == "P6"))
                                pegsystem = "06";
                            if (imagebatch.ReplaceNull("").Length >= 3 && imagebatch.Substring(0, 3) == "IN6")
                                pegsystem = "06";
                            if (imagebatch.ReplaceNull("").Length >= 3 & BCFImageBatchString != String.Empty &&
                                BCFImageBatchString.IndexOf(imagebatch.Substring(0, 3)) != -1)
                                pegsystem = "07";
                            if (imagebatch.ReplaceNull("").Length >= 2 && (imagebatch.Substring(0, 2) == "R7" || imagebatch.Substring(0, 2) == "P7"))
                                pegsystem = "07";
                            if (imagebatch.ReplaceNull("").Length >= 3 && imagebatch.Substring(0, 3) == "IN7")
                                pegsystem = "07";
                            if (pegsystem == String.Empty)
                                pegsystem = "07";

                        }

                    } //end if empty pegasystem

                    if (ex_memo.ReplaceNull("").Length > 200)
                        ex_memo = ex_memo.Substring(0, 200);
                    if (ex_memo2.ReplaceNull("").Length > 500)
                        ex_memo2 = ex_memo2.Substring(0, 500);
                    if (vendname.ReplaceNull("").Length > 35)
                        vendname = vendname.Substring(0, 35);
                    if (allprocess.ReplaceNull("").Length > 30)
                        allprocess = allprocess.Substring(0, 30);

                    EXCEPTION exc = new EXCEPTION();

                    exc.EX_ID = ""; 
                    exc.ACT = act;
                    exc.PDOCNO = pdocnopo;
                    exc.PO_ID = po_id;
                    exc.RR_ID = rr_id;
                    exc.AE_ID = ae_id;
                    exc.PA_ID = pa_id;
                    exc.INV_KEY_ID = inv_key_id;
                    exc.EX_FUND = ex_fund;
                    exc.ERR_CODE = err_code;
                    exc.ERR_RESPONSE = Err_response;
                    exc.PARTIAL_MATCH_VENDNO = partial_match_vendno;
                    exc.EX_DATE = DateTime.Now;
                    exc.EX_MEMO = ex_memo;
                    exc.EX_MEMO2 = ex_memo2;
                    exc.MISC_AMOUNT = (Decimal?)misc_amount;
                    exc.PAY_AMOUNT = (Decimal?)pay_amount;
                    exc.RRAMOUNT = (Decimal?)rramount;
                    exc.POAMOUNT = (Decimal?)poamount;
                    exc.HOLD_DATE = hold_date;
                    exc.UPDSTATUS = updstatus;
                    exc.NOT_KEY_ID = not_key_id;
                    exc.ORGCODE = orgcode;
                    exc.BA = ba;
                    exc.NETDAYS = (byte)netdays;
                    exc.VENDNAME = vendname;
                    exc.PODOCTYPE = podoctype;
                    exc.PEGSYSTEM = pegsystem;
                    exc.ALLPROCESS = allprocess;
                    exc.ADDPC = prepcode;
                    exc.OUT = "F";

                    contextVitap.EXCEPTIONS.Add(exc);
                    contextVitap.SaveChanges();

                    //Repull Exception to get it's ID
                    Ex_id = contextVitap.EXCEPTIONS.OrderByDescending(x => x.EX_DATE).FirstOrDefault(x => x.INV_KEY_ID == exc.INV_KEY_ID && x.ERR_CODE == exc.ERR_CODE).EX_ID; ;

                    if (updinvstatus == "T" && inv_key_id != String.Empty)
                    {
                        var fieldsToUpdate = new List<string>
                        {
                            "INV_STATUS",
                            "ERR_CODE"
                        };

                        var rtnInv = GetPegasysInvoiceByKey(inv_key_id);

                        if (rtnInv != null)
                        {
                            rtnInv.INV_STATUS = "EXCEPTION";
                            rtnInv.ERR_CODE = err_code;

                            UpdatePegasysInvoice(rtnInv, fieldsToUpdate);
                        }                    
                    }
                    if (updrrstatus == "T" && Rr_id != String.Empty)
                    {
                        var fieldsToUpdate = new List<string>
                        {
                            "RR_STATUS",
                            "ERR_CODE"
                        };

                        var rtnrr = GetPegasysRRByKey(Rr_id);

                        if (rtnrr != null)
                        {
                            rtnrr.RR_STATUS = "EXCEPTION";
                            rtnrr.ERR_CODE = err_code;

                          UpdatePegasysRR(rtnrr, fieldsToUpdate);
                        }
                    }
                    return true;
                }
            }
        } //end AddException

        /// <summary>
        /// Clears 
        /// </summary>
        public void ClearVariables()
        {
            act = String.Empty; ex_fund = String.Empty; pdocnopo = String.Empty; orgcode = String.Empty;
            ba = String.Empty; vendname = String.Empty; podoctype = String.Empty; pegsystem = String.Empty;
            allprocess = String.Empty;
            po_id = String.Empty; rr_id = String.Empty; ae_id = String.Empty; inv_key_id = String.Empty;
            pa_id = String.Empty; not_key_id = String.Empty; prepcode = String.Empty; imagebatch = String.Empty;
            err_code = String.Empty; Err_response = String.Empty; partial_match_vendno = String.Empty;
            ex_memo = String.Empty; ex_memo2 = String.Empty; updstatus = "T";
            misc_amount = 0; pay_amount = 0; rramount = 0; poamount = 0;
            netdays = 1;
            updinvstatus = "F";
        }
    }
}