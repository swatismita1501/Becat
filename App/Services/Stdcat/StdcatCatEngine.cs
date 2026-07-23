using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EcatDesktop.Services.Stdcat
{
    /// <summary>
    /// Partial CAT transformation engine for House-Hasson fixed-width source packs.
    /// This replaces placeholder/raw-copy fallback behavior with real record parsing
    /// and fixed-width export generation while the broader stdcat.lsl port continues.
    /// </summary>
    public sealed class StdcatCatEngine
    {
        private sealed class HouseHassonItemRecord
        {
            public HouseHassonItemRecord()
            {
                DepartmentCode = string.Empty;
                ClassCode = string.Empty;
                FinelineCode = string.Empty;
                Sku = string.Empty;
                Description = string.Empty;
                ShortDescription = string.Empty;
                VendorCode = string.Empty;
                ManufacturerPartNumber = string.Empty;
                UnitOfMeasure = "EA";
                Cost = "00000000";
                ManufacturerCost = "00000000";
                Retail = "00000000";
                ListPrice = "00000000";
                PurchaseQuantity = "0001";
                ManufacturerPurchaseQuantity = "0001";
                Upc = string.Empty;
                Gtin = "00000000000000";
                StatusFlag = string.Empty;
                LongDescriptionA = string.Empty;
                LongDescriptionB = string.Empty;
                LongDescriptionC = string.Empty;
                LongDescriptionD = string.Empty;
            }

            public string DepartmentCode { get; set; }
            public string ClassCode { get; set; }
            public string FinelineCode { get; set; }
            public string Sku { get; set; }
            public string Description { get; set; }
            public string ShortDescription { get; set; }
            public string VendorCode { get; set; }
            public string ManufacturerPartNumber { get; set; }
            public string UnitOfMeasure { get; set; }
            public string Cost { get; set; }
            public string ManufacturerCost { get; set; }
            public string Retail { get; set; }
            public string ListPrice { get; set; }
            public string PurchaseQuantity { get; set; }
            public string ManufacturerPurchaseQuantity { get; set; }
            public string Upc { get; set; }
            public string Gtin { get; set; }
            public string StatusFlag { get; set; }
            public string LongDescriptionA { get; set; }
            public string LongDescriptionB { get; set; }
            public string LongDescriptionC { get; set; }
            public string LongDescriptionD { get; set; }
        }

        private sealed class HouseHassonVendorRecord
        {
            public HouseHassonVendorRecord()
            {
                VendorCode = string.Empty;
                Name = string.Empty;
                Address1 = string.Empty;
                Address2 = string.Empty;
                City = string.Empty;
                State = string.Empty;
                Zip = string.Empty;
                Terms = string.Empty;
            }

            public string VendorCode { get; set; }
            public string Name { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string City { get; set; }
            public string State { get; set; }
            public string Zip { get; set; }
            public string Terms { get; set; }
        }

        private sealed class HouseHassonCodeRecord
        {
            public HouseHassonCodeRecord()
            {
                Code = string.Empty;
                Description = string.Empty;
            }

            public string Code { get; set; }
            public string Description { get; set; }
        }

        private readonly IList<StdcatExportField> moItemFields;

        public StdcatCatEngine(string sApplicationBaseDirectory)
        {
            moItemFields = StdcatExportLayout.gItemFields;
        }
        public void GenerateHouseHassonCatalog(
            string sItemInputPath,
            string sLongDescriptionInputPath,
            string sDepartmentInputPath,
            string sClassInputPath,
            string sFineInputPath,
            string sVendorInputPath,
            string sWhlCode,
            string sOutputFolderPath,
            DateTime? dCatRawDate)
        {
            Directory.CreateDirectory(sOutputFolderPath);

            var dDateAdded = dCatRawDate.HasValue ? dCatRawDate.Value : File.GetLastWriteTime(sItemInputPath);
            var sDateAdded = StdcatFormatter.sReformatDate(dDateAdded);
            var oLongDescriptions = oLoadLongDescriptions(sLongDescriptionInputPath);
            var oItems = oLoadHouseHassonItems(sItemInputPath, sWhlCode, sDateAdded, oLongDescriptions);
            var oDepartments = oLoadCodeFile(sDepartmentInputPath, 2, 2, 32);
            var oClasses = oLoadCodeFile(sClassInputPath, 3, 3, 32);
            var oFines = oLoadCodeFile(sFineInputPath, 6, 6, 32);
            var oVendors = oLoadVendors(sVendorInputPath);

            oWriteLines(Path.Combine(sOutputFolderPath, "ITEM.TXT"), oItems.Select(oItem => sBuildItemRow(oItem, sWhlCode, sDateAdded)));
            oWriteLines(Path.Combine(sOutputFolderPath, "DEPT.TXT"), oBuildDepartmentRows(oItems, oDepartments));
            oWriteLines(Path.Combine(sOutputFolderPath, "CLAS.TXT"), oBuildClassRows(oItems, oClasses));
            oWriteLines(Path.Combine(sOutputFolderPath, "FINE.TXT"), oBuildFineRows(oItems, oFines));
            oWriteLines(Path.Combine(sOutputFolderPath, "VEND.TXT"), oBuildVendorRows(oItems, oVendors, sDateAdded));
        }

        public void GenerateGenericCatalog(
            string sItemInputPath,
            string sUpcInputPath,
            string sLongDescriptionInputPath,
            string sWhlCode,
            string sOutputFolderPath)
        {
            Directory.CreateDirectory(sOutputFolderPath);

            var sDateAdded = StdcatFormatter.sReformatDate(File.GetLastWriteTime(sItemInputPath));
            var oLongDescriptions = oLoadGenericLongDescriptions(sLongDescriptionInputPath);
            var oUpcMap = oLoadGenericUpcBySku(sUpcInputPath);
            var oItems = oLoadGenericItems(sItemInputPath, oLongDescriptions, oUpcMap);
            var oEmptyCodes = new Dictionary<string, HouseHassonCodeRecord>(StringComparer.OrdinalIgnoreCase);
            var oEmptyVendors = new Dictionary<string, HouseHassonVendorRecord>(StringComparer.OrdinalIgnoreCase);

            oWriteLines(Path.Combine(sOutputFolderPath, "ITEM.TXT"), oItems.Select(oItem => sBuildItemRow(oItem, sWhlCode, sDateAdded)));
            oWriteLines(Path.Combine(sOutputFolderPath, "DEPT.TXT"), oBuildDepartmentRows(oItems, oEmptyCodes));
            oWriteLines(Path.Combine(sOutputFolderPath, "CLAS.TXT"), oBuildClassRows(oItems, oEmptyCodes));
            oWriteLines(Path.Combine(sOutputFolderPath, "FINE.TXT"), oBuildFineRows(oItems, oEmptyCodes));
            oWriteLines(Path.Combine(sOutputFolderPath, "VEND.TXT"), oBuildVendorRows(oItems, oEmptyVendors, sDateAdded));
        }

        public void GenerateEjdCatalog(
            string sItemInputPath,
            string sLongDescriptionInputPath,
            string sDepartmentInputPath,
            string sClassInputPath,
            string sVendorInputPath,
            string sWhlCode,
            string sOutputFolderPath)
        {
            Directory.CreateDirectory(sOutputFolderPath);

            var sDateAdded = StdcatFormatter.sReformatDate(File.GetLastWriteTime(sItemInputPath));
            var oLongDescriptions = oLoadLongDescriptions(sLongDescriptionInputPath);
            var oItems = oItemIntoOutEjd(sItemInputPath, sWhlCode, oLongDescriptions);
            var oDepartments = oLoadCodeFile(sDepartmentInputPath, 2, 2, 32);
            var oClasses = oLoadCodeFile(sClassInputPath, 3, 3, 32);
            var oVendors = oLoadVendors(sVendorInputPath);

            oWriteLines(Path.Combine(sOutputFolderPath, "ITEM.TXT"), oItems.Select(delegate (HouseHassonItemRecord oItem) { return sBuildItemRow(oItem, sWhlCode, sDateAdded); }));
            oWriteLines(Path.Combine(sOutputFolderPath, "VEND.TXT"), oStdVndOutEjd(oItems, oVendors, sDateAdded));
            oWriteLines(Path.Combine(sOutputFolderPath, "DEPT.TXT"), oMakeecdeEjd(oItems, oDepartments));
            oWriteLines(Path.Combine(sOutputFolderPath, "CLAS.TXT"), oMakeecclEjd(oItems, oClasses));
            oWriteLines(Path.Combine(sOutputFolderPath, "FINE.TXT"), oMakeecfiEjd(oItems));
        }

        public IList<string> oGetExpectedCatEntries()
        {
            return new List<string> { "ITEM.TXT", "VEND.TXT", "DEPT.TXT", "CLAS.TXT", "FINE.TXT" };
        }

        private IList<HouseHassonItemRecord> oLoadHouseHassonItems(string sItemInputPath, string sWhlCode, string sDateAdded, IDictionary<string, string[]> oLongDescriptions)
        {
            var oItems = new List<HouseHassonItemRecord>();
            foreach (var sLine in File.ReadAllLines(sItemInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var sSku = sSafeSubstring(sLine, 0, 8).Trim();
                if (sSku.Length == 0)
                {
                    continue;
                }

                var sClassCode = sSafeSubstring(sLine, 20, 3).Trim();
                var sDepartmentCode = sHouseHassonLoadTapeField(sLine, "Whl_Dept");
                var sFineline = sSafeSubstring(sLine, 30, 5).Trim();
                // Paradox HHH uses the imported STDL_II Whl_Description field, not the full
                // trailing raw text. In Paradox itemIntoOut this is documented as:
                //   Whl_Description X(50)
                //   LT Description X(32)
                // and then processed by tascii(..., 50, "Whl_Description", "").
                // tascii left-trims and caps to MaxS; it does not pull characters beyond
                // the 32-byte LT Description field.
                var sRawDescription = sSafeSubstring(sLine, 35, 32);
                var sStatusFlag = sExtractHouseHassonStatusFlag(sRawDescription);
                var sDescription = sNormalizeHouseHassonDescription(sParadoxTasci(sRawDescription, 50, string.Empty), sStatusFlag);
                var sVendorCode = sSafeSubstring(sLine, 87, 5).Trim();
                var sManufacturerPart = sSafeSubstring(sLine, 98, 16).Trim();
                var sUnitOfMeasure = sSafeSubstring(sLine, 145, 2).Trim();
                var sManufacturerCost = "00000000";
                var sCost = StdcatFormatter.sIntFill(8, sDigitsOnly(sSafeSubstring(sLine, 147, 8)), sManufacturerCost);
                if (sManufacturerCost == "00000000")
                {
                    sManufacturerCost = sCost;
                }
                var sRetail = StdcatFormatter.sIntFill(8, sDigitsOnly(sSafeSubstring(sLine, 155, 8)), "00000000");
                var sMultiple = StdcatFormatter.sIntFill(4, sHouseHassonLoadTapeField(sLine, "Multiple"), "0001");
                var sStandardPack = StdcatFormatter.sIntFill(4, sHouseHassonLoadTapeField(sLine, "Std_Pack"), "0001");
                var iMultiple = iParsePositiveInt(sMultiple);
                var iStandardPack = iParsePositiveInt(sStandardPack);
                var sManufacturerPurchaseQuantity =
                    sVendorCode.Length > 0 && iStandardPack < iMultiple
                        ? sMultiple
                        : sStandardPack;
                var sTailSegment = sSafeSubstring(sLine, 282, sLine.Length - 282);

                string sListPrice;
                string sUpc;
                string sGtin;
                oParseTailCodes(sTailSegment, out sListPrice, out sUpc, out sGtin);
                // In the HHH STDL_II raw feed, the Whl_List slot is blank; the tail digits
                // carry UPC/GTIN data, not the catalog list price used by itemIntoOut.
                sListPrice = "00000000";

                var oItem = new HouseHassonItemRecord();
                oItem.Sku = sSku;
                oItem.ClassCode = sClassCode;
                oItem.DepartmentCode = sDepartmentCode;
                oItem.FinelineCode = StdcatFormatter.sSpacePad(6, sFineline, "R");
                oItem.Description = sDescription;
                oItem.ShortDescription = sDescription.Length > 15 ? sDescription.Substring(0, 15) : sDescription;
                oItem.VendorCode = sVendorCode;
                oItem.ManufacturerPartNumber = sManufacturerPart;
                oItem.UnitOfMeasure = sUnitOfMeasure.Length == 0 ? "EA" : sUnitOfMeasure;
                oItem.Cost = sCost;
                oItem.ManufacturerCost = sManufacturerCost;
                oItem.Retail = sRetail;
                oItem.ListPrice = sListPrice;
                oItem.PurchaseQuantity = sMultiple;
                oItem.ManufacturerPurchaseQuantity = sManufacturerPurchaseQuantity;
                oItem.Upc = sUpc;
                oItem.Gtin = sGtin;
                oItem.StatusFlag = sStatusFlag;

                string[] rLongDescriptions;
                if (oLongDescriptions.TryGetValue(sSku, out rLongDescriptions))
                {
                    oItem.LongDescriptionA = rLongDescriptions[0];
                    oItem.LongDescriptionB = rLongDescriptions[1];
                    oItem.LongDescriptionC = rLongDescriptions[2];
                    oItem.LongDescriptionD = rLongDescriptions[3];
                }
                else
                {
                    oItem.LongDescriptionA = sDescription;
                }

                oItems.Add(oItem);
            }

            return oItems
                .OrderBy(oItem => oItem.Sku, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }



        // Direct EJD item transform modeled on the Paradox itemIntoOut flow.
        private static IList<HouseHassonItemRecord> oItemIntoOutEjd(
            string sItemInputPath,
            string sWhlCode,
            IDictionary<string, string[]> oLongDescriptions)
        {
            var oItems = new List<HouseHassonItemRecord>();
            foreach (var sLine in File.ReadAllLines(sItemInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var sSku = sSafeSubstring(sLine, 0, 14).Trim();
                if (sSku.Length == 0)
                {
                    continue;
                }

                var sDeptClass = sSafeSubstring(sLine, 20, 5).Trim();
                var sDescription = sNormalizeDescription(sSafeSubstring(sLine, 36, 38));
                var sVendorCode = sSafeSubstring(sLine, 87, 5).Trim();
                var sManufacturerPart = sSafeSubstring(sLine, 92, 16).Replace("+", string.Empty).Trim().TrimStart('0');
                var sUnitOfMeasure = sSafeSubstring(sLine, 145, 2).Trim();
                var sPurchaseQuantity = sDigitsOnly(sSafeSubstring(sLine, 164, 6));
                var sCost = StdcatFormatter.sIntFill(8, sDigitsOnly(sSafeSubstring(sLine, 147, 8)), "00000000");
                var sRetail = StdcatFormatter.sIntFill(8, sDigitsOnly(sSafeSubstring(sLine, 155, 8)), "00000000");

                var oItem = new HouseHassonItemRecord();
                oItem.Sku = sSku;
                oItem.ClassCode = sDeptClass.Length >= 3 ? sDeptClass.Substring(0, 3) : string.Empty;
                oItem.DepartmentCode = sDeptClass.Length >= 5 ? sDeptClass.Substring(3, 2) : string.Empty;
                oItem.FinelineCode = string.Empty;
                oItem.Description = sDescription;
                oItem.ShortDescription = sDescription.Length > 15 ? sDescription.Substring(0, 15) : sDescription;
                oItem.VendorCode = sVendorCode;
                oItem.ManufacturerPartNumber = sManufacturerPart.Length == 0 ? sSku : sManufacturerPart;
                oItem.UnitOfMeasure = sUnitOfMeasure.Length == 0 ? "EA" : sUnitOfMeasure;
                oItem.Cost = sCost;
                oItem.ManufacturerCost = sCost;
                oItem.Retail = sRetail;
                oItem.ListPrice = sRetail;
                oItem.Upc = string.Empty;
                oItem.Gtin = string.Empty;

                var sQuantity = StdcatFormatter.sIntFill(4, sPurchaseQuantity, "0001");

                oItem.LongDescriptionA = sDescription;
                oItem.LongDescriptionB = string.Empty;
                oItem.LongDescriptionC = string.Empty;
                oItem.LongDescriptionD = string.Empty;
                oItem.PurchaseQuantity = sQuantity;
                oItem.ManufacturerPurchaseQuantity = sQuantity;

                string[] rLongDescriptions;
                if (oLongDescriptions.TryGetValue(sSku, out rLongDescriptions))
                {
                    oItem.LongDescriptionA = rLongDescriptions[0];
                    oItem.LongDescriptionB = rLongDescriptions[1];
                    oItem.LongDescriptionC = rLongDescriptions[2];
                    oItem.LongDescriptionD = rLongDescriptions[3];
                }

                oItems.Add(oItem);
            }

            return oItems
                .OrderBy(oItem => oItem.Sku, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Direct EJD vendor export modeled on the Paradox StdVndOut flow.
        private IEnumerable<string> oStdVndOutEjd(
            IEnumerable<HouseHassonItemRecord> oItems,
            IDictionary<string, HouseHassonVendorRecord> oVendors,
            string sDateAdded)
        {
            return oBuildVendorRows(oItems, oVendors, sDateAdded);
        }

        // Direct EJD department export modeled on the Paradox makeecde flow.
        private IEnumerable<string> oMakeecdeEjd(
            IEnumerable<HouseHassonItemRecord> oItems,
            IDictionary<string, HouseHassonCodeRecord> oDepartments)
        {
            return oBuildDepartmentRows(oItems, oDepartments);
        }

        // Direct EJD class export modeled on the Paradox makeeccl flow.
        private IEnumerable<string> oMakeecclEjd(
            IEnumerable<HouseHassonItemRecord> oItems,
            IDictionary<string, HouseHassonCodeRecord> oClasses)
        {
            return oBuildClassRows(oItems, oClasses);
        }

        // Direct EJD fineline export modeled on the Paradox makeecfi flow.
        // The EJD source pack in this environment does not include a fineline master,
        // so this stage intentionally emits the same empty output shape observed in the
        // known-good EJD reference zips.
        private IEnumerable<string> oMakeecfiEjd(IEnumerable<HouseHassonItemRecord> oItems)
        {
            return Enumerable.Empty<string>();
        }

        private static IDictionary<string, string[]> oLoadLongDescriptions(string sLongDescriptionInputPath)
        {
            var oDescriptions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            foreach (var sLine in File.ReadAllLines(sLongDescriptionInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var sSku = sSafeSubstring(sLine, 0, 8).Trim();
                if (sSku.Length == 0)
                {
                    continue;
                }

                var sDescription = sNormalizeDescription(sSafeSubstring(sLine, 14, Math.Max(0, sLine.Length - 14)));
                var rChunks = new[] { string.Empty, string.Empty, string.Empty, string.Empty };
                for (var iIndex = 0; iIndex < rChunks.Length; iIndex++)
                {
                    var iStart = iIndex * 250;
                    if (iStart >= sDescription.Length)
                    {
                        break;
                    }

                    var iLength = Math.Min(250, sDescription.Length - iStart);
                    rChunks[iIndex] = sDescription.Substring(iStart, iLength);
                }

                oDescriptions[sSku] = rChunks;
            }

            return oDescriptions;
        }

        private static IDictionary<string, string[]> oLoadGenericLongDescriptions(string sLongDescriptionInputPath)
        {
            var oDescriptions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(sLongDescriptionInputPath) || !File.Exists(sLongDescriptionInputPath))
            {
                return oDescriptions;
            }

            foreach (var sLine in File.ReadAllLines(sLongDescriptionInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var oMatch = Regex.Match(sLine, @"^\s*(?<sku>[A-Z0-9\-/]{4,14})\s+(?<desc>.+?)\s*$");
                string sSku;
                string sDescription;
                if (oMatch.Success)
                {
                    sSku = oMatch.Groups["sku"].Value.Trim();
                    sDescription = sNormalizeDescription(oMatch.Groups["desc"].Value);
                }
                else
                {
                    sSku = sSafeSubstring(sLine, 0, 14).Trim();
                    sDescription = sNormalizeDescription(sSafeSubstring(sLine, 14, Math.Max(0, sLine.Length - 14)));
                }

                if (sSku.Length == 0 || sDescription.Length == 0)
                {
                    continue;
                }

                var rChunks = new[] { string.Empty, string.Empty, string.Empty, string.Empty };
                for (var iIndex = 0; iIndex < rChunks.Length; iIndex++)
                {
                    var iStart = iIndex * 250;
                    if (iStart >= sDescription.Length)
                    {
                        break;
                    }

                    rChunks[iIndex] = sDescription.Substring(iStart, Math.Min(250, sDescription.Length - iStart));
                }

                oDescriptions[sSku] = rChunks;
            }

            return oDescriptions;
        }

        private static IDictionary<string, string> oLoadGenericUpcBySku(string sUpcInputPath)
        {
            var oUpcs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(sUpcInputPath) || !File.Exists(sUpcInputPath))
            {
                return oUpcs;
            }

            foreach (var sLine in File.ReadAllLines(sUpcInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var oSkuMatch = Regex.Match(sLine, @"^\s*(?<sku>[A-Z0-9\-/]{4,14})");
                if (!oSkuMatch.Success)
                {
                    continue;
                }

                var oCodeMatches = Regex.Matches(sLine, @"\d{12,14}");
                if (oCodeMatches.Count == 0)
                {
                    continue;
                }

                oUpcs[oSkuMatch.Groups["sku"].Value.Trim()] = oCodeMatches[oCodeMatches.Count - 1].Value;
            }

            return oUpcs;
        }

        private static IList<HouseHassonItemRecord> oLoadGenericItems(
            string sItemInputPath,
            IDictionary<string, string[]> oLongDescriptions,
            IDictionary<string, string> oUpcMap)
        {
            var oItems = new List<HouseHassonItemRecord>();
            foreach (var sLine in File.ReadAllLines(sItemInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var oItem = oParseGenericItemLine(sLine);
                if (oItem == null)
                {
                    continue;
                }

                string sUpcFromMap;
                if (oUpcMap.TryGetValue(oItem.Sku, out sUpcFromMap))
                {
                    oItem.Upc = StdcatFormatter.sMassageUpc(sUpcFromMap);
                    oItem.Gtin = StdcatFormatter.sMassageGtin(sUpcFromMap);
                }

                string[] rLongDescriptions;
                if (oLongDescriptions.TryGetValue(oItem.Sku, out rLongDescriptions))
                {
                    oItem.LongDescriptionA = rLongDescriptions[0];
                    oItem.LongDescriptionB = rLongDescriptions[1];
                    oItem.LongDescriptionC = rLongDescriptions[2];
                    oItem.LongDescriptionD = rLongDescriptions[3];
                }
                else
                {
                    oItem.LongDescriptionA = oItem.Description;
                }

                oItems.Add(oItem);
            }

            return oItems
                .OrderBy(oItem => oItem.Sku, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static HouseHassonItemRecord oParseGenericItemLine(string sLine)
        {
            var oSkuMatch = Regex.Match(sLine, @"^\s*(?<sku>[A-Z0-9\-/]{4,14})");
            if (!oSkuMatch.Success)
            {
                return null;
            }

            var sSku = oSkuMatch.Groups["sku"].Value.Trim();
            var sDeptClass = new string(sSafeSubstring(sLine, 20, 5).Where(char.IsDigit).ToArray());
            if (sDeptClass.Length < 5)
            {
                var oFiveDigits = Regex.Matches(sLine, @"\d{5}")
                    .Cast<Match>()
                    .Select(oMatch => oMatch.Value)
                    .Where(sValue => !sValue.Equals(sSku, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (oFiveDigits.Count > 0)
                {
                    sDeptClass = oFiveDigits[0];
                }
            }

            var sFineline = new string(sSafeSubstring(sLine, 30, 6).Where(char.IsDigit).ToArray());
            if (sFineline.Length == 0)
            {
                var oFineMatch = Regex.Match(sLine, @"^\s*[A-Z0-9\-/]{4,14}\s+\d{5}\s+(?<fine>\d{4,6})");
                if (oFineMatch.Success)
                {
                    sFineline = oFineMatch.Groups["fine"].Value;
                }
            }

            var sDescription = sNormalizeDescription(sSafeSubstring(sLine, 35, 60));
            if (sDescription.Length == 0)
            {
                var sFallback = Regex.Replace(sLine, @"^\s*[A-Z0-9\-/]{4,14}\s*", string.Empty);
                sDescription = sNormalizeDescription(sFallback);
            }

            var sVendorCode = new string(sSafeSubstring(sLine, 87, 5).Where(char.IsDigit).ToArray());
            if (sVendorCode.Length == 0)
            {
                var oFiveDigits = Regex.Matches(sLine, @"\d{5}")
                    .Cast<Match>()
                    .Select(oMatch => oMatch.Value)
                    .Where(sValue => !sValue.Equals(sDeptClass, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                sVendorCode = oFiveDigits.Count > 1 ? oFiveDigits[oFiveDigits.Count - 1] : string.Empty;
            }

            var sManufacturerPart = sSafeSubstring(sLine, 98, 16).Trim();
            var sUnitOfMeasure = sSafeSubstring(sLine, 145, 2).Trim();
            if (sUnitOfMeasure.Length == 0)
            {
                var oUomMatch = Regex.Match(sLine, @"\b(?<uom>EA|BX|PK|PR|CS|CT|BG)\b", RegexOptions.IgnoreCase);
                sUnitOfMeasure = oUomMatch.Success ? oUomMatch.Groups["uom"].Value.ToUpperInvariant() : "EA";
            }

            var oFourDigitMatches = Regex.Matches(sLine, @"\d{4}")
                .Cast<Match>()
                .Select(oMatch => oMatch.Value)
                .ToList();
            var sCostSeed = new string(sSafeSubstring(sLine, 151, 4).Where(char.IsDigit).ToArray());
            var sRetailSeed = new string(sSafeSubstring(sLine, 159, 4).Where(char.IsDigit).ToArray());
            if (sCostSeed.Length == 0 && oFourDigitMatches.Count > 0)
            {
                sCostSeed = oFourDigitMatches[Math.Max(0, oFourDigitMatches.Count - 2)];
            }

            if (sRetailSeed.Length == 0 && oFourDigitMatches.Count > 0)
            {
                sRetailSeed = oFourDigitMatches[oFourDigitMatches.Count - 1];
            }

            var sTailSegment = sSafeSubstring(sLine, Math.Max(0, sLine.Length - 40), Math.Min(40, sLine.Length));
            string sListPrice;
            string sUpc;
            string sGtin;
            oParseTailCodes(sTailSegment, out sListPrice, out sUpc, out sGtin);

            var oItem = new HouseHassonItemRecord();
            oItem.Sku = sSku;
            oItem.ClassCode = sDeptClass.Length >= 3 ? sDeptClass.Substring(0, 3) : "000";
            oItem.DepartmentCode = sDeptClass.Length >= 5 ? sDeptClass.Substring(3, 2) : "00";
            oItem.FinelineCode = StdcatFormatter.sSpacePad(6, sFineline.Length == 0 ? oItem.ClassCode : sFineline, "R");
            oItem.Description = sDescription.Length == 0 ? sSku : sDescription;
            oItem.ShortDescription = oItem.Description.Length > 15 ? oItem.Description.Substring(0, 15) : oItem.Description;
            oItem.VendorCode = sVendorCode;
            oItem.ManufacturerPartNumber = sManufacturerPart.Length == 0 ? sSku : sManufacturerPart;
            oItem.UnitOfMeasure = sUnitOfMeasure.Length == 0 ? "EA" : sUnitOfMeasure;
            oItem.Cost = StdcatFormatter.sIntFill(8, sCostSeed, "00000000");
            oItem.ManufacturerCost = oItem.Cost;
            oItem.Retail = StdcatFormatter.sIntFill(8, sRetailSeed, oItem.Cost);
            oItem.ListPrice = sListPrice;
            oItem.Upc = sUpc;
            oItem.Gtin = sGtin;
            return oItem;
        }

        private static IDictionary<string, HouseHassonCodeRecord> oLoadCodeFile(
             string sInputPath,
             int iCodeLength,
             int iDescriptionStart,
             int iDescriptionLength)
        {
            var oCodes = new Dictionary<string, HouseHassonCodeRecord>(StringComparer.OrdinalIgnoreCase);
            foreach (var sLine in File.ReadAllLines(sInputPath, Encoding.Default))
            {
                if (string.IsNullOrWhiteSpace(sLine))
                {
                    continue;
                }

                var sCode = sSafeSubstring(sLine, 0, iCodeLength).Trim();
                if (sCode.Length == 0)
                {
                    continue;
                }

                var sDescription = sSafeSubstring(sLine, iDescriptionStart, iDescriptionLength);

                oCodes[sCode] = new HouseHassonCodeRecord
                {
                    Code = sCode,
                    Description = sNormalizeDescription(sDescription)
                };
            }

            return oCodes;
        }

        private static IDictionary<string, HouseHassonVendorRecord> oLoadVendors(string sVendorInputPath)
        {
            var oVendors = new Dictionary<string, HouseHassonVendorRecord>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(sVendorInputPath) || !File.Exists(sVendorInputPath))
            {
                return oVendors;
            }

            var oBytes = File.ReadAllBytes(sVendorInputPath);
            IEnumerable<HouseHassonVendorRecord> oRecords = oLooksLikeText(oBytes)
                ? oLoadLtvmTextRecords(sVendorInputPath)
                : oLoadLtvmDbRecords(oBytes);

            foreach (var oVendor in oRecords)
            {
                if (oVendor == null || string.IsNullOrWhiteSpace(oVendor.VendorCode))
                {
                    continue;
                }

                var sCode = sLeftTrimToSize(5, oVendor.VendorCode);
                if (!oVendors.ContainsKey(sCode))
                {
                    oVendor.VendorCode = sCode;
                    oVendors.Add(sCode, oVendor);
                }
            }

            return oVendors;
        }

        private static IEnumerable<HouseHassonVendorRecord> oLoadLtvmTextRecords(string sVendorInputPath)
        {
            foreach (var sLine in File.ReadAllLines(sVendorInputPath, Encoding.Default))
            {
                var oVendor = oTryParseLtvmTextRecord(sLine);
                if (oVendor != null)
                {
                    yield return oVendor;
                }
            }
        }

        private static IEnumerable<HouseHassonVendorRecord> oLoadLtvmDbRecords(byte[] oBytes)
        {
            // Native C# extraction for Ltvm.db.  This intentionally does not use BDE/ODBC.
            // The visible LTVM row payload contains:
            // VmVendorCode(5), VmStore(1), VmShortName(10), VmPayToVendID(5),
            // VmPayToVendStore(1), VmVendorName(32), VmAddr1(32), VmAddr2(32),
            // VmCity(16), VmState(2), VmZip5(5), VmZip4(4), ... VmTermsDesc(15).
            for (var iOffset = 0; iOffset < oBytes.Length - 300; iOffset++)
            {
                if (!oIsLtvmRecordStart(oBytes, iOffset))
                {
                    continue;
                }

                var sRecord = Encoding.Default.GetString(oBytes, iOffset, 300);
                var oVendor = oTryParseLtvmRecordPayload(sRecord);
                if (oVendor != null)
                {
                    yield return oVendor;
                }
            }
        }

        private static bool oLooksLikeText(byte[] oBytes)
        {
            var iLength = Math.Min(oBytes.Length, 512);
            for (var iIndex = 0; iIndex < iLength; iIndex++)
            {
                if (oBytes[iIndex] == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool oIsLtvmRecordStart(byte[] oBytes, int iOffset)
        {
            if (iOffset < 0 || iOffset + 260 >= oBytes.Length)
            {
                return false;
            }

            for (var iIndex = 0; iIndex < 5; iIndex++)
            {
                if (!char.IsDigit((char)oBytes[iOffset + iIndex]))
                {
                    return false;
                }
            }

            if (!char.IsDigit((char)oBytes[iOffset + 5]))
            {
                return false;
            }

            for (var iIndex = 0; iIndex < 5; iIndex++)
            {
                if (oBytes[iOffset + 16 + iIndex] != oBytes[iOffset + iIndex])
                {
                    return false;
                }
            }

            if (oBytes[iOffset + 21] != oBytes[iOffset + 5])
            {
                return false;
            }

            for (var iIndex = iOffset; iIndex < iOffset + 260; iIndex++)
            {
                var bValue = oBytes[iIndex];
                if (bValue != 0 && (bValue < 32 || bValue > 126))
                {
                    return false;
                }
            }

            return true;
        }

        private static HouseHassonVendorRecord oTryParseLtvmTextRecord(string sLine)
        {
            if (string.IsNullOrWhiteSpace(sLine))
            {
                return null;
            }

            var sVendorCode = sSafeSubstring(sLine, 1, 5).Trim();
            if (!oIsFiveDigitCode(sVendorCode))
            {
                return null;
            }

            var sZip5 = sSafeSubstring(sLine, 126, 5).Trim();
            var sZip4 = sSafeSubstring(sLine, 131, 4).Trim();

            return new HouseHassonVendorRecord
            {
                VendorCode = sVendorCode,
                Name = sLeftTrimToSize(50, sSafeSubstring(sLine, 10, 32)),
                Address1 = sLeftTrimToSize(50, sSafeSubstring(sLine, 40, 32)),
                Address2 = sLeftTrimToSize(50, sSafeSubstring(sLine, 72, 32)),
                City = sLeftTrimToSize(25, sSafeSubstring(sLine, 104, 16)),
                State = sLeftTrimToSize(2, sSafeSubstring(sLine, 124, 2)),
                Zip = sZip4.Length == 0 ? sZip5 : sZip5 + "-" + sZip4,
                Terms = sLeftTrimToSize(50, sSafeSubstring(sLine, 251, 15))
            };
        }

        private static HouseHassonVendorRecord oTryParseLtvmRecordPayload(string sRecord)
        {
            var sVendorCode = sCleanLtvmField(sSafeSubstring(sRecord, 0, 5));
            if (!oIsFiveDigitCode(sVendorCode))
            {
                return null;
            }

            var sZip5 = sCleanLtvmField(sSafeSubstring(sRecord, 136, 5));
            var sZip4 = sCleanLtvmField(sSafeSubstring(sRecord, 141, 4));

            return new HouseHassonVendorRecord
            {
                VendorCode = sVendorCode,
                Name = sLeftTrimToSize(50, sCleanLtvmField(sSafeSubstring(sRecord, 22, 32))),
                Address1 = sLeftTrimToSize(50, sCleanLtvmField(sSafeSubstring(sRecord, 54, 32))),
                Address2 = sLeftTrimToSize(50, sCleanLtvmField(sSafeSubstring(sRecord, 86, 32))),
                City = sLeftTrimToSize(25, sCleanLtvmField(sSafeSubstring(sRecord, 118, 16))),
                State = sLeftTrimToSize(2, sCleanLtvmField(sSafeSubstring(sRecord, 134, 2))),
                Zip = sZip4.Length == 0 ? sZip5 : sZip5 + "-" + sZip4,
                Terms = sLeftTrimToSize(50, sCleanLtvmField(sSafeSubstring(sRecord, 239, 15)))
            };
        }

        private static bool oIsFiveDigitCode(string sCode)
        {
            return !string.IsNullOrWhiteSpace(sCode)
                && sCode.Length == 5
                && sCode.All(char.IsDigit);
        }

        private static string sCleanLtvmField(string sValue)
        {
            return (sValue ?? string.Empty).Replace("\0", string.Empty).TrimStart();
        }

        private static string sLeftTrimToSize(int iMaxSize, string sValue)
        {
            sValue = (sValue ?? string.Empty).TrimStart();
            return sValue.Length > iMaxSize ? sValue.Substring(0, iMaxSize) : sValue;
        }

        private IEnumerable<string> oBuildDepartmentRows(IEnumerable<HouseHassonItemRecord> oItems, IDictionary<string, HouseHassonCodeRecord> oDepartments)
        {
            return oItems.Select(oItem => sNormalizeDepartmentCode(oItem.DepartmentCode))
                .Where(sCode => !string.IsNullOrWhiteSpace(sCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(sCode => sCode, StringComparer.OrdinalIgnoreCase)
                .Select(
                    delegate (string sCode)
                    {
                        HouseHassonCodeRecord oDepartment;
                        var sDescription = oDepartments.TryGetValue(sCode, out oDepartment) ? oDepartment.Description : "UNDEFINED";
                        return StdcatFormatter.sSpacePad(1, "R", "R")
                            + StdcatFormatter.sSpacePad(1, string.Empty, "R")
                            + StdcatFormatter.sSpacePad(2, sCode, "R")
                            + StdcatFormatter.sSpacePad(50, sDescription, "R");
                    });
        }

        private IEnumerable<string> oBuildClassRows(IEnumerable<HouseHassonItemRecord> oItems, IDictionary<string, HouseHassonCodeRecord> oClasses)
        {
            return oItems.Where(oItem => !string.IsNullOrWhiteSpace(oItem.ClassCode))
                .GroupBy(oItem => oItem.ClassCode, StringComparer.OrdinalIgnoreCase)
                .Select(
                    delegate (IGrouping<string, HouseHassonItemRecord> oClassGroup)
                    {
                        var oPreferredDepartment = oClassGroup
                            .GroupBy(oItem => sNormalizeDepartmentCode(oItem.DepartmentCode), StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(oDeptGroup => oDeptGroup.Count())
                            .ThenBy(oDeptGroup => oDeptGroup.Key, StringComparer.OrdinalIgnoreCase)
                            .First();

                        var oRepresentativeItem = oClassGroup.First();
                        oRepresentativeItem.DepartmentCode = oPreferredDepartment.Key;
                        return oRepresentativeItem;
                    })
                .OrderBy(oItem => oItem.ClassCode, StringComparer.OrdinalIgnoreCase)
                .Select(
                    delegate (HouseHassonItemRecord oItem)
                    {
                        var sCode = oItem.ClassCode;
                        HouseHassonCodeRecord oClass;
                        var sDescription = oClasses.TryGetValue(sCode, out oClass) ? oClass.Description : "UNDEFINED";
                        return StdcatFormatter.sSpacePad(1, "R", "R")
                            + StdcatFormatter.sSpacePad(3, sCode, "R")
                            + StdcatFormatter.sSpacePad(2, sNormalizeDepartmentCode(oItem.DepartmentCode), "R")
                            + StdcatFormatter.sSpacePad(50, sDescription, "R");
                    });
        }

        private IEnumerable<string> oBuildFineRows(IEnumerable<HouseHassonItemRecord> oItems, IDictionary<string, HouseHassonCodeRecord> oFines)
        {
            return oItems.Where(oItem => !string.IsNullOrWhiteSpace(oItem.FinelineCode))
                .GroupBy(oItem => oItem.FinelineCode.TrimEnd() + "|" + oItem.ClassCode, StringComparer.OrdinalIgnoreCase)
                .Select(oGroup => oGroup.First())
                .OrderBy(oItem => oItem.FinelineCode, StringComparer.OrdinalIgnoreCase)
                .Select(
                    delegate (HouseHassonItemRecord oItem)
                    {
                        var sTrimmedCode = oItem.FinelineCode.TrimEnd();
                        HouseHassonCodeRecord oFine;
                        var sDescription = oFines.TryGetValue(sTrimmedCode, out oFine) ? oFine.Description : "UNDEFINED";
                        return StdcatFormatter.sSpacePad(1, "R", "R")
                            + StdcatFormatter.sSpacePad(6, sTrimmedCode, "R")
                            + StdcatFormatter.sSpacePad(3, oItem.ClassCode, "R")
                            + StdcatFormatter.sSpacePad(50, sDescription, "R");
                    });
        }

        private IEnumerable<string> oBuildVendorRows(IEnumerable<HouseHassonItemRecord> oItems, IDictionary<string, HouseHassonVendorRecord> oVendors, string sDateAdded)
        {
            return oItems.Select(oItem => sLeftTrimToSize(5, oItem.VendorCode))
                .Where(sCode => !string.IsNullOrWhiteSpace(sCode))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(sCode => sCode, StringComparer.OrdinalIgnoreCase)
                .Select(
                    delegate (string sCode)
                    {
                        HouseHassonVendorRecord oVendor;
                        if (!oVendors.TryGetValue(sCode, out oVendor))
                        {
                            oVendor = new HouseHassonVendorRecord
                            {
                                VendorCode = sCode,
                                Name = "UNDEFINED"
                            };
                        }

                        var oValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        oValues["Whl_Mfg_Code"] = sLeftTrimToSize(5, oVendor.VendorCode);
                        oValues["Date_Added"] = sDateAdded;
                        oValues["Name"] = sLeftTrimToSize(50, oVendor.Name);
                        oValues["Address1"] = sLeftTrimToSize(50, oVendor.Address1);
                        oValues["Address2"] = sLeftTrimToSize(50, oVendor.Address2);
                        oValues["City"] = sLeftTrimToSize(25, oVendor.City);
                        oValues["State"] = sLeftTrimToSize(2, oVendor.State);
                        oValues["Zip"] = sLeftTrimToSize(10, oVendor.Zip);
                        oValues["Terms"] = sLeftTrimToSize(50, oVendor.Terms);

                        return StdcatExportLayout.sBuildRow(StdcatExportLayout.gVendorFields, oValues);
                    });
        }

        private string sBuildItemRow(HouseHassonItemRecord oItem, string sWhlCode, string sDateAdded)
        {
            var oValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            oValues["Whl_Sku"] = oItem.Sku;
            oValues["Whl_Dept"] = sNormalizeDepartmentCode(oItem.DepartmentCode);
            oValues["Whl_Class"] = oItem.ClassCode;
            oValues["Whl_FineLine"] = oItem.FinelineCode.TrimEnd();
            oValues["Whl_Dept_Alt"] = string.Empty;
            oValues["Whl_Class_Alt"] = string.Empty;
            oValues["Whl_Fineline_Alt"] = string.Empty;
            oValues["Short_Description"] = oItem.ShortDescription;
            oValues["Whl_Description"] = oItem.Description;
            oValues["Whl_UPC"] = oItem.Upc;
            oValues["Date_Added"] = sDateAdded;
            oValues["Whl_Cat"] = sWhlCode;
            oValues["Whl_Cat_Page"] = string.Empty;
            oValues["Whl_Cat_Page_Alt"] = string.Empty;
            oValues["Whl_Code"] = sWhlCode;
            oValues["Whl_Pur_UOM"] = oItem.UnitOfMeasure;
            oValues["Whl_Pur_UOM_Qty"] = oItem.PurchaseQuantity;
            oValues["Whl_Pur_Min_Qty"] = "0001";
            oValues["Whl_Cost"] = oItem.Cost;
            oValues["Whl_Cost_Date"] = "00000000";
            oValues["Whl_Breakpack_Ind"] = string.Empty;
            oValues["Whl_Mfg_Code"] = oItem.VendorCode;
            oValues["Whl_Mfg_Part_No"] = oItem.ManufacturerPartNumber;
            oValues["Whl_Mfg_Pur_UOM"] = oItem.UnitOfMeasure;
            oValues["Whl_Mfg_Pur_UOM_Qty"] = oItem.ManufacturerPurchaseQuantity;
            oValues["Whl_Mfg_Pur_Min_Qty"] = "0001";
            oValues["Whl_Mfg_Cost"] = oItem.ManufacturerCost;
            oValues["Whl_Mfg_Cost_Date"] = "00000000";
            oValues["Whl_Mfg_Breakpack_Ind"] = string.Empty;
            oValues["Item_Status_Ind"] = "A";
            oValues["Return_Type_Ind"] = string.Empty;
            oValues["Date_Disc"] = "00000000";
            oValues["Whl_Stk_UOM"] = oItem.UnitOfMeasure;
            oValues["Whl_Stk_UOM_Qty"] = "0001";
            oValues["Whl_Retail"] = oItem.Retail;
            oValues["Whl_Retail_Date"] = "00000000";
            oValues["Whl_Stk_UOM_Alt1"] = "EA";
            oValues["Whl_Stk_UOM_Alt1_Qty"] = "0000";
            oValues["Whl_Retail_Alt1"] = "00000000";
            oValues["Whl_Retail_Alt1_Date"] = "00000000";
            oValues["Whl_Stk_UOM_Alt2"] = "EA";
            oValues["Whl_Stk_UOM_Alt2_Qty"] = "0000";
            oValues["Whl_Retail_Alt2"] = "00000000";
            oValues["Whl_Retail_Alt2_Date"] = "00000000";
            oValues["Whl_List"] = oItem.ListPrice;
            oValues["Whl_List_Date"] = "00000000";
            oValues["Weight"] = "00000000";
            oValues["Closeout_Type_Ind"] = string.Empty;
            oValues["Closeout_Amt"] = "00000000";
            oValues["Closeout_Date_From"] = "00000000";
            oValues["Closeout_Date_To"] = "00000000";
            oValues["Pricing_Code"] = string.Empty;
            // Paradox ITEM.TXT repeats the 50-char item description into Long_Description_A
            // and leaves the remaining long-description segments blank.
            oValues["Long_Description_A"] = oItem.Description;
            oValues["Long_Description_B"] = string.Empty;
            oValues["Long_Description_C"] = string.Empty;
            oValues["Long_Description_D"] = string.Empty;
            oValues["Long_Description_E"] = string.Empty;
            oValues["R1_Vendor"] = string.Empty;
            oValues["R1_Number"] = string.Empty;
            oValues["R1_Type_Ind"] = string.Empty;
            oValues["R2_Vendor"] = string.Empty;
            oValues["R2_Number"] = string.Empty;
            oValues["R2_Type_Ind"] = string.Empty;
            oValues["R3_Vendor"] = string.Empty;
            oValues["R3_Number"] = string.Empty;
            oValues["R3_Type_Ind"] = string.Empty;
            oValues["Rdc_List"] = string.Empty;
            oValues["Rdc_Central_Ship"] = "0000";
            oValues["Flags"] = string.Empty;
            oValues["E1_Type"] = string.Empty;
            oValues["E1_Id"] = string.Empty;
            oValues["E2_Type"] = string.Empty;
            oValues["E2_Id"] = string.Empty;
            oValues["E3_Type"] = string.Empty;
            oValues["E3_Id"] = string.Empty;
            oValues["E4_Type"] = string.Empty;
            oValues["E4_Id"] = string.Empty;
            oValues["GTIN"] = oItem.Gtin;
            oValues["Filler"] = string.Empty;
            return StdcatExportLayout.sBuildRow(moItemFields, oValues);
        }

        private static string sNormalizeDepartmentCode(string sDepartmentCode)
        {
            sDepartmentCode = (sDepartmentCode ?? string.Empty).Trim();
            if (sDepartmentCode.Length == 0 ||
                sDepartmentCode == "00" ||
                sDepartmentCode == "0" ||
                sDepartmentCode == "0  " ||
                sDepartmentCode == " 0")
            {
                return "99";
            }

            return StdcatFormatter.sSpacePad(2, sDepartmentCode, "R");
        }

        private static void oParseTailCodes(string sTailSegment, out string sListPrice, out string sUpc, out string sGtin)
        {
            sListPrice = "00000000";
            sUpc = string.Empty;
            sGtin = "00000000000000";

            var sTailDigits = sDigitsOnly(sTailSegment);
            if (string.IsNullOrWhiteSpace(sTailDigits) || sTailDigits.Length < 4)
            {
                return;
            }

            sListPrice = StdcatFormatter.sIntFill(8, sTailDigits.Substring(0, 4), "00000000");

            var oDigitGroups = Regex.Matches(sTailSegment ?? string.Empty, @"\d+")
                .Cast<Match>()
                .Select(oMatch => oMatch.Value)
                .ToList();

            if (oDigitGroups.Count > 0)
            {
                var sTrailingCode = oDigitGroups[oDigitGroups.Count - 1];
                if (sTrailingCode.Length >= 12 && sTrailingCode.Length <= 14)
                {
                    sGtin = StdcatFormatter.sMassageGtin(sTrailingCode);
                    sUpc = StdcatFormatter.sMassageUpc(
                        sTrailingCode.Length > 12
                            ? sTrailingCode.Substring(sTrailingCode.Length - 12)
                            : sTrailingCode);
                    return;
                }
            }

            var sCodeRun = sTailDigits.Substring(4);
            var sCode = string.Empty;

            if (sCodeRun.Length >= 24 && sCodeRun.Length % 2 == 0)
            {
                var iHalf = sCodeRun.Length / 2;
                var sFirstHalf = sCodeRun.Substring(0, iHalf);
                var sSecondHalf = sCodeRun.Substring(iHalf);
                if (sFirstHalf.Equals(sSecondHalf, StringComparison.Ordinal))
                {
                    sCode = sFirstHalf;
                }
            }

            if (sCode.Length == 0)
            {
                if (sCodeRun.Length >= 13)
                {
                    sCode = sCodeRun.Substring(sCodeRun.Length - 13);
                }
                else if (sCodeRun.Length >= 12)
                {
                    sCode = sCodeRun.Substring(sCodeRun.Length - 12);
                }
                else
                {
                    sCode = sCodeRun;
                }
            }

            sGtin = StdcatFormatter.sMassageGtin(sCode);
            sUpc = StdcatFormatter.sMassageUpc(
                sCode.Length > 12
                    ? sCode.Substring(sCode.Length - 12)
                    : sCode);
        }

        private static string sHouseHassonLoadTapeField(string sLine, string sFieldName)
        {
            switch ((sFieldName ?? string.Empty).ToUpperInvariant())
            {
                case "MULTIPLE":
                    // Paradox itemIntoOut reads Whl_Pur_UOM_Qty from tclt."Multiple".
                    // The HHH fixed-width item feed parsed here does not supply that
                    // field in the Std_Pack slot, so intfill applies the Paradox default.
                    return string.Empty;
                case "STD_PACK":
                    return sDigitsOnly(sSafeSubstring(sLine, 164, 6));
                case "WHL_DEPT":
                    // Paradox itemIntoOut reads Whl_Dept as its own load-tape field.
                    // For this HHH fixed-width item feed it is not supplied in the
                    // class-code slice, so sNormalizeDepartmentCode later applies "99".
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }

        private static int iParsePositiveInt(string sValue)
        {
            int iValue;
            return int.TryParse(sDigitsOnly(sValue), out iValue) ? iValue : 0;
        }

        private static string sParadoxTasci(string sValue, int iMaxSize, string sDefault)
        {
            sValue = sValue ?? string.Empty;
            if (sValue.Length != 0)
            {
                sValue = sValue.TrimStart();
                if (sValue.Length > iMaxSize)
                {
                    sValue = sValue.Substring(0, iMaxSize);
                }
                return sValue;
            }

            return sDefault ?? string.Empty;
        }
        private static string sNormalizeDescription(string sValue)
        {
            sValue = (sValue ?? string.Empty).Trim();
            if (sValue.Length == 0)
            {
                return string.Empty;
            }

            sValue = Regex.Replace(sValue, @"\s*[\+\-]\d+(?:\.\d+)?\s*$", string.Empty);
            sValue = Regex.Replace(sValue, @"\s{2,}", " ");
            return sValue.Trim();
        }

        private static string sExtractHouseHassonStatusFlag(string sRawDescription)
        {
            if (string.IsNullOrWhiteSpace(sRawDescription))
            {
                return string.Empty;
            }

            var sWithoutPrice = Regex.Replace(sRawDescription, @"\s*[\+\-]\d+(?:\.\d+)?\s*$", string.Empty).TrimEnd();
            var oMatch = Regex.Match(
                sWithoutPrice,
                @"\*{3}(?:\s+(?:DROP|DR|D))?(?:\s+\*{3})?\s*$",
                RegexOptions.IgnoreCase);

            return oMatch.Success ? Regex.Replace(oMatch.Value.Trim(), @"\s{2,}", " ").ToUpperInvariant() : string.Empty;
        }

        private static string sNormalizeHouseHassonDescription(string sRawDescription, string sStatusFlag)
        {
            var sWithoutPrice = Regex.Replace((sRawDescription ?? string.Empty), @"\s*[\+\-]\d+(?:\.\d+)?\s*$", string.Empty).TrimEnd();
            if (sStatusFlag.Length == 0)
            {
                return sNormalizeDescription(sWithoutPrice);
            }

            var iFlagIndex = sWithoutPrice.LastIndexOf(sStatusFlag, StringComparison.OrdinalIgnoreCase);
            var sBaseDescription = iFlagIndex >= 0
                ? sWithoutPrice.Substring(0, iFlagIndex)
                : sWithoutPrice;

            sBaseDescription = sNormalizeDescription(sBaseDescription);
            if (sBaseDescription.Length == 0)
            {
                return sStatusFlag;
            }

            return sBaseDescription + " " + sStatusFlag;
        }

        private static string sDigitsOnly(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
            {
                return string.Empty;
            }

            return new string(sValue.Where(char.IsDigit).ToArray());
        }

        private static void oWriteLines(string sPath, IEnumerable<string> oLines)
        {
            File.WriteAllLines(sPath, oLines, Encoding.Default);
        }

        private static void ExtractZipFiles(string zipPath, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);
            using (var stream = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position + 30 <= stream.Length)
                {
                    var signature = reader.ReadUInt32();
                    if (signature != 0x04034b50)
                    {
                        break;
                    }

                    reader.ReadUInt16();
                    var flags = reader.ReadUInt16();
                    var method = reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadUInt16();
                    reader.ReadUInt32();
                    var compressedSize = reader.ReadUInt32();
                    var uncompressedSize = reader.ReadUInt32();
                    var nameLength = reader.ReadUInt16();
                    var extraLength = reader.ReadUInt16();
                    var nameBytes = reader.ReadBytes(nameLength);
                    if (extraLength > 0)
                    {
                        reader.ReadBytes(extraLength);
                    }

                    var entryName = Encoding.ASCII.GetString(nameBytes).Replace('/', Path.DirectorySeparatorChar);
                    if ((flags & 0x0008) != 0 || entryName.EndsWith("\\") || entryName.EndsWith("/"))
                    {
                        stream.Position += compressedSize;
                        continue;
                    }

                    var outputPath = Path.Combine(targetDirectory, Path.GetFileName(entryName).ToUpperInvariant());
                    var data = reader.ReadBytes((int)compressedSize);
                    if (method == 0)
                    {
                        File.WriteAllBytes(outputPath, data);
                    }
                    else if (method == 8)
                    {
                        using (var compressed = new MemoryStream(data))
                        using (var deflate = new System.IO.Compression.DeflateStream(compressed, System.IO.Compression.CompressionMode.Decompress))
                        using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            deflate.CopyTo(output);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported zip compression method " + method + " in " + zipPath + ".");
                    }

                    if (uncompressedSize == 0 && File.Exists(outputPath))
                    {
                        File.WriteAllBytes(outputPath, new byte[0]);
                    }
                }
            }
        }

        private static string sSafeSubstring(string sValue, int iStartIndex, int iLength)
        {
            if (string.IsNullOrEmpty(sValue) || iStartIndex >= sValue.Length || iLength <= 0)
            {
                return string.Empty;
            }

            var iSafeLength = Math.Min(iLength, sValue.Length - iStartIndex);
            return sValue.Substring(iStartIndex, iSafeLength);
        }
    }
}