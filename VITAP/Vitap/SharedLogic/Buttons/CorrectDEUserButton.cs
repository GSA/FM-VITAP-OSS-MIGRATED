using VITAP.Data.Managers.Buttons;
using VITAP.Data;
using VITAP.Controllers;
using VITAP.Data.Models.Exceptions;
using System.Collections.Generic;
using System;

namespace VITAP.SharedLogic.Buttons
{
    public class CorrectDEUserButton : ButtonExceptionManager
    {
        /// <summary>
        /// For User Exceptions only with override code
        /// Uses Notes screen
        /// Uses InvEdit screen
        /// </summary>
        /// <param name="exc"></param>
        /// <param name="prepCode"></param>
        /// <returns></returns>
        public bool Initialize(EXCEPTION exc, string prepCode)
        {
            SetVariables(exc, prepCode, "CORRECTDE");

            return true;
        }

        public void SetNotes(NotesViewModel notes)
        {
            SetNotesValues(notes);
        }

        public PEGASYSPO_FRM GetPegasysPOFrmById(string poId)
        {
            return GetPegasysPOFrmByKey(poId);
        }

        //public void FinishCode(string sNotesType, string invKeyId, string poId)
        //{
        //    string U048Table = "", errCode = exception.ERR_CODE ;

        //    if (errCode.Right(3) == "230")
        //    {
        //        if (errCode.Left(1) == "P")
        //        {
        //            U048Table = "PEGINV";
        //            CreateU048();
        //        }
        //        else if (errCode == "D062")
        //        {
        //            notes.returnVal3 = "D062";
        //            U048Table = notes.returnValZ;
        //            CreateU048();
        //        }
        //        else if (errCode.Left(1) == "V")
        //        {
        //            //generate U048 & blank out the PO.last_status
        //            if (!String.IsNullOrWhiteSpace(invKeyId))
        //            {
        //                U048Table = "PEGINV";
        //            }
        //            else if (!String.IsNullOrWhiteSpace(poId))
        //            {
        //                U048Table = "PEGPO";
        //            }

        //            CreateU048();
        //        }
        //        else if (errCode.InList("U043,U044"))
        //        {
        //            CreateU043(sNotesType);
        //        }
        //        else if (errCode == "U049")
        //        {
        //            CreateU049(sNotesType);
        //        }
        //        else if (errCode.InList("P060,P061,P005"))
        //        {
        //            string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
        //            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes);

        //            var rtnInv = GetPegasysInvoiceByKey(invKeyId);
        //            UpdatePegasysInvoiceStatusById("MATCHREADY");

        //            //This only updates the old FoxPro tables and is not needed anymore
        //            //SET CLASS TO exceptions ADDITIVE
        //            //objChg = CREATE("InterestCorrectDE")

        //            //objChg.r_act = THISFORM.r_act

        //            //objChg.r_rr_id = THISFORM.r_rr_id

        //            //objChg.r_inv_key_id = THISFORM.r_inv_key_id

        //            //objChg.r_pegasys = THISFORM.r_pegasys

        //            //objChg.SHOW
        //        }
        //        else
        //        {
        //            string responsenotes = exception.RESPONSENOTES + "\r\n" + NewNote;
        //            UpdateException(exception, "Q", notes.returnVal7, notes.returnVal3, responsenotes);
        //        }
        //    }
        //}


        public void UpdateInvoice(InvEditViewModel invoice)
            {
                var rtnInv = GetPegasysInvoiceByKey(invoice.InvoiceKeyId);
            
                var properties = new List<string>
                {
                    "INVOICE",
                    "INVDATE",
                    "INVRECDATE",
                    "PONUMBER",
                    "CONTRACT",
                    "ACCOUNTNO",
                    "VENDNAME",
                    "ADDR_L1",
                    "ADDR_L2",
                    "ADDR_L3",
                    "ADDR_CITY",
                    "ADDR_STATE",
                    "ADDR_ZPCD",
                    "DISCPERCENT",
                    "DISCDAYS",
                    "NETDAYS",
                    "TAXAMOUNT",
                    "SHIPAMOUNT",
                    "MISC_CHARGES",
                    "AMOUNT",
                    "ACT"
                };
                var cMemo = "";
                if (rtnInv.INVOICE != invoice.InvoiceNumber)
                {
                    cMemo = "INVOICE: " + rtnInv.INVOICE + " changed To " + invoice.InvoiceNumber;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.INVOICE = invoice.InvoiceNumber;
                }
                if (rtnInv.INVDATE != invoice.InvoiceDate)
                {
                    cMemo = "INVOICE DATE: " + rtnInv.INVDATE + " changed To " + invoice.InvoiceDate;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.INVDATE = invoice.InvoiceDate;
                }
                if (rtnInv.INVRECDATE != invoice.InvoiceReceivedDate)
                {
                    cMemo = "INVRECDATE: " + rtnInv.INVRECDATE + " changed To " + invoice.InvoiceReceivedDate;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.INVRECDATE = invoice.InvoiceReceivedDate;
                }
                if (rtnInv.PONUMBER != invoice.PurchaseOrderNumber)
                {
                    cMemo = "PONUMBER: " + rtnInv.PONUMBER + " changed To " + invoice.PurchaseOrderNumber;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.PONUMBER = invoice.PurchaseOrderNumber;
                }
                if (rtnInv.CONTRACT != invoice.ContractNumber)
                {
                    cMemo = "CONTRACT: " + rtnInv.CONTRACT + " changed To " + invoice.ContractNumber;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.CONTRACT = invoice.ContractNumber;
                }
                if (rtnInv.ACCOUNTNO != invoice.AccountNumber)
                {
                    cMemo = "ACCOUNTNO: " + rtnInv.ACCOUNTNO + " changed To " + invoice.AccountNumber;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ACCOUNTNO = invoice.AccountNumber;
                }
                if (rtnInv.VENDNAME != invoice.VendorName)
                {
                    cMemo = "VENDNAME: " + rtnInv.VENDNAME + " changed To " + invoice.VendorName;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.VENDNAME = invoice.VendorName;
                }
                if (rtnInv.ADDR_L1 != invoice.VendorAddr1)
                {
                    cMemo = "ADDR_L1: " + rtnInv.ADDR_L1 + " changed To " + invoice.VendorAddr1;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_L1 = invoice.VendorAddr1;
                }
                if (rtnInv.ADDR_L2 != invoice.VendorAddr2)
                {
                    cMemo = "ADDR_L2: " + rtnInv.ADDR_L2 + " changed To " + invoice.VendorAddr2;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_L2 = invoice.VendorAddr2;
                }
                if (rtnInv.ADDR_L3 != invoice.VendorAddr3)
                {
                    cMemo = "ADDR_L3: " + rtnInv.ADDR_L3 + " changed To " + invoice.VendorAddr3;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_L3 = invoice.VendorAddr3;
                }            
                if (rtnInv.ADDR_CITY != invoice.RemitCity)
                {
                    cMemo = "ADDR_CITY: " + rtnInv.ADDR_CITY + " changed To " + invoice.RemitCity;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_CITY = invoice.RemitCity;
                }
                if (rtnInv.ADDR_STATE != invoice.RemitState)
                {
                    cMemo = "ADDR_STATE: " + rtnInv.ADDR_STATE + " changed To " + invoice.RemitState;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_STATE = invoice.RemitState;
                }
                if (rtnInv.ADDR_ZPCD != invoice.RemitZip)
                {
                    cMemo = "ADDR_ZPCD: " + rtnInv.ADDR_ZPCD + " changed To " + invoice.RemitZip;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.ADDR_ZPCD = invoice.RemitZip;
                }
                if (rtnInv.DISCPERCENT != invoice.DiscountPercent)
                {
                    cMemo = "DISCPERCENT: " + rtnInv.DISCPERCENT + " changed To " + invoice.DiscountPercent;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.DISCPERCENT = invoice.DiscountPercent;
                }
                if (rtnInv.DISCDAYS != invoice.DiscountDays)
                {
                    cMemo = "DISCDAYS: " + rtnInv.DISCDAYS + " changed To " + invoice.DiscountDays;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.DISCDAYS = invoice.DiscountDays;
                }
                if (rtnInv.NETDAYS != invoice.NetDays)
                {
                    cMemo = "NETDAYS: " + rtnInv.NETDAYS + " changed To " + invoice.NetDays;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.NETDAYS = invoice.NetDays;
                }
                if (rtnInv.TAXAMOUNT != invoice.TaxAmount)
                {
                    cMemo = "TAXAMOUNT: " + rtnInv.TAXAMOUNT + " changed To " + invoice.TaxAmount;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.TAXAMOUNT = invoice.TaxAmount;
                }
                if (rtnInv.SHIPAMOUNT != invoice.ShippingCharges)
                {
                    cMemo = "SHIPAMOUNT: " + rtnInv.SHIPAMOUNT + " changed To " + invoice.ShippingCharges;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.SHIPAMOUNT = invoice.ShippingCharges;
                }
                if (rtnInv.MISC_CHARGES != invoice.MiscCharge)
                {
                    cMemo = "MISC_CHARGES: " + rtnInv.MISC_CHARGES + " changed To " + invoice.MiscCharge;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.MISC_CHARGES = invoice.MiscCharge;
                }
                if (rtnInv.AMOUNT != invoice.InvoiceAmount)
                {
                    cMemo = "AMOUNT: " + rtnInv.AMOUNT + " changed To " + invoice.InvoiceAmount;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act
                    rtnInv.AMOUNT = invoice.InvoiceAmount;
                }

                if (invoice.Act != rtnInv.ACT) // IIF(ISNULL(InvEditQuery.ACT), '', ALLTRIM(InvEditQuery.ACT))
                {
                    cMemo = "ACT: " + rtnInv.ACT + " changed To " + invoice.Act;
                    InsertFieldChangeToTranshist(invoice, rtnInv, rtnInv.ACT, cMemo, "ACT"); //transhist under old act

                    var responsenotes = exception.RESPONSENOTES + "Notification Exception removed because..." + cMemo + "";
                    UpdateExceptionM(rtnInv.INV_KEY_ID, "Q", responsenotes);

                    InsertFieldChangeToTranshist(invoice, rtnInv, invoice.Act, cMemo, "ACT"); // transhist under new act
                    rtnInv.ACT = invoice.Act;
                }

                UpdatePegasysInvoice(rtnInv, properties);                
            }

        public PEGASYSINVOICE GetPegasysInvoiceByKeyId(string invKeyId)
        {
            return GetPegasysInvoiceByKey(invKeyId);
        }

        private void InsertFieldChangeToTranshist(InvEditViewModel invoice, PEGASYSINVOICE PI, string Act, string cMemo, string FieldName)
        {
            var transhist = new TRANSHIST()
            {
                ACT = Act,
                PDOCNO = PI.PDOCNOPO,
                INV_KEY_ID = PI.INV_KEY_ID,
                ERR_CODE = "D/E",
                TRANSDATE = DateTime.Now,
                PREPCODE = PrepCode,
                CUFF_MEMO = cMemo,
                ALLPROCESS = FieldName,
                CLEARED_DATE = DateTime.Now
            };

            InsertTranshist(transhist);
        }
    }
}