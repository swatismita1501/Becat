using System;
using System.Collections.Generic;
using System.Text;

namespace EcatDesktop.Services.Stdcat
{
    public sealed class StdcatExportField
    {
        public StdcatExportField(string sName, int iLength)
        {
            Name = sName;
            Length = iLength;
        }

        public string Name { get; private set; }
        public int Length { get; private set; }
    }

    /// <summary>
    /// Data-driven export layouts derived from the Paradox SYSTEM export templates.
    /// The generated fixed-width rows preserve the Paradox export field order.
    /// </summary>
    public static class StdcatExportLayout
    {
        public static readonly IList<StdcatExportField> gItemFields = new List<StdcatExportField>
        {
            new StdcatExportField("Whl_Sku", 16),
            new StdcatExportField("Whl_Dept", 2),
            new StdcatExportField("Whl_Class", 3),
            new StdcatExportField("Whl_FineLine", 6),
            new StdcatExportField("Whl_Dept_Alt", 2),
            new StdcatExportField("Whl_Class_Alt", 3),
            new StdcatExportField("Whl_Fineline_Alt", 6),
            new StdcatExportField("Short_Description", 15),
            new StdcatExportField("Whl_Description", 50),
            new StdcatExportField("Whl_UPC", 12),
            new StdcatExportField("Date_Added", 8),
            new StdcatExportField("Whl_Cat", 3),
            new StdcatExportField("Whl_Cat_Page", 8),
            new StdcatExportField("Whl_Cat_Page_Alt", 8),
            new StdcatExportField("Whl_Code", 5),
            new StdcatExportField("Whl_Pur_UOM", 2),
            new StdcatExportField("Whl_Pur_UOM_Qty", 4),
            new StdcatExportField("Whl_Pur_Min_Qty", 4),
            new StdcatExportField("Whl_Cost", 8),
            new StdcatExportField("Whl_Cost_Date", 8),
            new StdcatExportField("Whl_Breakpack_Ind", 1),
            new StdcatExportField("Whl_Mfg_Code", 5),
            new StdcatExportField("Whl_Mfg_Part_No", 16),
            new StdcatExportField("Whl_Mfg_Pur_UOM", 2),
            new StdcatExportField("Whl_Mfg_Pur_UOM_Qty", 4),
            new StdcatExportField("Whl_Mfg_Pur_Min_Qty", 4),
            new StdcatExportField("Whl_Mfg_Cost", 8),
            new StdcatExportField("Whl_Mfg_Cost_Date", 8),
            new StdcatExportField("Whl_Mfg_Breakpack_Ind", 1),
            new StdcatExportField("Item_Status_Ind", 1),
            new StdcatExportField("Return_Type_Ind", 1),
            new StdcatExportField("Date_Disc", 8),
            new StdcatExportField("Whl_Stk_UOM", 2),
            new StdcatExportField("Whl_Stk_UOM_Qty", 4),
            new StdcatExportField("Whl_Retail", 8),
            new StdcatExportField("Whl_Retail_Date", 8),
            new StdcatExportField("Whl_Stk_UOM_Alt1", 2),
            new StdcatExportField("Whl_Stk_UOM_Alt1_Qty", 4),
            new StdcatExportField("Whl_Retail_Alt1", 8),
            new StdcatExportField("Whl_Retail_Alt1_Date", 8),
            new StdcatExportField("Whl_Stk_UOM_Alt2", 2),
            new StdcatExportField("Whl_Stk_UOM_Alt2_Qty", 4),
            new StdcatExportField("Whl_Retail_Alt2", 8),
            new StdcatExportField("Whl_Retail_Alt2_Date", 8),
            new StdcatExportField("Whl_List", 8),
            new StdcatExportField("Whl_List_Date", 8),
            new StdcatExportField("Weight", 8),
            new StdcatExportField("Closeout_Type_Ind", 1),
            new StdcatExportField("Closeout_Amt", 8),
            new StdcatExportField("Closeout_Date_From", 8),
            new StdcatExportField("Closeout_Date_To", 8),
            new StdcatExportField("Pricing_Code", 5),
            new StdcatExportField("Long_Description_A", 250),
            new StdcatExportField("Long_Description_B", 250),
            new StdcatExportField("Long_Description_C", 250),
            new StdcatExportField("Long_Description_D", 250),
            new StdcatExportField("Long_Description_E", 250),
            new StdcatExportField("R1_Vendor", 5),
            new StdcatExportField("R1_Number", 16),
            new StdcatExportField("R1_Type_Ind", 1),
            new StdcatExportField("R2_Vendor", 5),
            new StdcatExportField("R2_Number", 16),
            new StdcatExportField("R2_Type_Ind", 1),
            new StdcatExportField("R3_Vendor", 5),
            new StdcatExportField("R3_Number", 16),
            new StdcatExportField("R3_Type_Ind", 1),
            new StdcatExportField("Rdc_List", 24),
            new StdcatExportField("Rdc_Central_Ship", 4),
            new StdcatExportField("Flags", 50),
            new StdcatExportField("E1_Type", 2),
            new StdcatExportField("E1_Id", 8),
            new StdcatExportField("E2_Type", 2),
            new StdcatExportField("E2_Id", 8),
            new StdcatExportField("E3_Type", 2),
            new StdcatExportField("E3_Id", 8),
            new StdcatExportField("E4_Type", 2),
            new StdcatExportField("E4_Id", 8),
            new StdcatExportField("GTIN", 14),
            new StdcatExportField("Filler", 6)
        }.AsReadOnly();

        public static readonly IList<StdcatExportField> gVendorFields = new List<StdcatExportField>
        {
            new StdcatExportField("Whl_Mfg_Code", 5),
            new StdcatExportField("Date_Added", 8),
            new StdcatExportField("Name", 50),
            new StdcatExportField("Address1", 50),
            new StdcatExportField("Address2", 50),
            new StdcatExportField("City", 25),
            new StdcatExportField("State", 2),
            new StdcatExportField("Zip", 10),
            new StdcatExportField("Country", 25),
            new StdcatExportField("Phone", 10),
            new StdcatExportField("Fax", 10),
            new StdcatExportField("Contact", 50),
            new StdcatExportField("Freight", 50),
            new StdcatExportField("Terms", 50),
            new StdcatExportField("E1_type", 2),
            new StdcatExportField("E1_id", 8),
            new StdcatExportField("E2_type", 2),
            new StdcatExportField("E2_id", 8),
            new StdcatExportField("Min_PO_Amt", 8),
            new StdcatExportField("Min_PO_Wt", 8),
            new StdcatExportField("Min_PO_Units", 4),
            new StdcatExportField("Min_PO_Item", 4)
        }.AsReadOnly();

        public static readonly IList<StdcatExportField> gDepartmentFields = new List<StdcatExportField>
        {
            new StdcatExportField("Alt_Ind", 1),
            new StdcatExportField("Filler", 1),
            new StdcatExportField("Dept", 2),
            new StdcatExportField("Description", 50)
        }.AsReadOnly();

        public static readonly IList<StdcatExportField> gClassFields = new List<StdcatExportField>
        {
            new StdcatExportField("Alt_Ind", 1),
            new StdcatExportField("Class", 3),
            new StdcatExportField("Dept", 2),
            new StdcatExportField("Description", 50)
        }.AsReadOnly();

        public static readonly IList<StdcatExportField> gFinelineFields = new List<StdcatExportField>
        {
            new StdcatExportField("Alt_Ind", 1),
            new StdcatExportField("Fineline", 6),
            new StdcatExportField("Class", 3),
            new StdcatExportField("Description", 50)
        }.AsReadOnly();

        public static string sBuildRow(IList<StdcatExportField> oFields, IDictionary<string, string> oValues)
        {
            var rBuilder = new StringBuilder();
            foreach (var oField in oFields)
            {
                string sValue;
                if (!oValues.TryGetValue(oField.Name, out sValue))
                {
                    sValue = string.Empty;
                }

                rBuilder.Append(StdcatFormatter.sSpacePad(oField.Length, sValue, "R"));
            }

            return rBuilder.ToString();
        }
    }
}
