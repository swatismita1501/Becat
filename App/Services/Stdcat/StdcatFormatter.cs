using System;
using System.Globalization;
using System.Text;

namespace EcatDesktop.Services.Stdcat
{
    /// <summary>
    /// Direct C# port of the reusable formatting helpers in stdcat.lsl.
    /// These helpers intentionally preserve Paradox-era padding, truncation,
    /// zero-fill, UPC, GTIN, and COBOL numeric formatting behavior.
    /// </summary>
    public static class StdcatFormatter
    {
        public static string sRemoveLeadingZeroes(string sValue)
        {
            if (string.IsNullOrEmpty(sValue))
            {
                return string.Empty;
            }

            sValue = sValue.TrimStart();
            var iIndex = 0;
            while (iIndex < sValue.Length && sValue[iIndex] == '0')
            {
                iIndex++;
            }

            return iIndex >= sValue.Length ? string.Empty : sValue.Substring(iIndex);
        }

        public static string sDeDash(string sValue)
        {
            return string.IsNullOrEmpty(sValue) ? string.Empty : sValue.Replace("-", string.Empty).Trim();
        }

        public static string sDeSpace(string sValue)
        {
            return string.IsNullOrEmpty(sValue) ? string.Empty : sValue.Replace(" ", string.Empty).Trim();
        }

        public static string sSpacePad(int iMaxSize, string sField, string sPadType)
        {
            sField = sField ?? string.Empty;
            if (sField.Length > iMaxSize)
            {
                sField = sField.Substring(0, iMaxSize);
            }

            var iPadSize = Math.Max(0, iMaxSize - sField.Length);
            var sPad = new string(' ', iPadSize);
            return string.Equals(sPadType, "L", StringComparison.OrdinalIgnoreCase) ? sPad + sField : sField + sPad;
        }

        public static string sZeroPad(int iMaxSize, string sField, string sPadType)
        {
            sField = sField ?? string.Empty;
            if (sField.Length > iMaxSize)
            {
                sField = sField.Substring(0, iMaxSize);
            }

            var iPadSize = Math.Max(0, iMaxSize - sField.Length);
            var sPad = new string('0', iPadSize);
            return string.Equals(sPadType, "L", StringComparison.OrdinalIgnoreCase) ? sPad + sField : sField + sPad;
        }

        public static string sToSize(int iMaxSize, string sField)
        {
            return sSpacePad(iMaxSize, sField ?? string.Empty, "R");
        }

        public static string sIntFill(int iMaxSize, string sField, string sDefault)
        {
            sField = (sField ?? string.Empty).TrimStart();
            sField = sRemoveLeadingZeroes(sField);

            long iValue;
            if (!long.TryParse(sField, NumberStyles.Integer, CultureInfo.InvariantCulture, out iValue))
            {
                return sDefault;
            }

            var iMaxValue = 1;
            for (var iIndex = 0; iIndex < iMaxSize; iIndex++)
            {
                iMaxValue *= 10;
            }
            iMaxValue--;

            if (iValue >= 1 && iValue < iMaxValue)
            {
                return sZeroPad(iMaxSize, iValue.ToString(CultureInfo.InvariantCulture), "L");
            }

            if (iValue >= 1 && iValue >= iMaxValue)
            {
                return iMaxValue.ToString(CultureInfo.InvariantCulture);
            }

            return sDefault;
        }

        public static string sFormatAscii(int iMaxSize, string sField, string sDefault)
        {
            if (!string.IsNullOrEmpty(sField))
            {
                sField = sField.TrimStart();
                return sField.Length > iMaxSize ? sField.Substring(0, iMaxSize) : sField;
            }

            return sDefault;
        }

        public static string sCatNumAscii(int iMaxSize, string sField, bool bKeepZeroes, bool bZeroPadNumerics)
        {
            if (string.IsNullOrEmpty(sField))
            {
                return string.Empty;
            }

            sField = sField.TrimStart();
            if (sField.Length > iMaxSize)
            {
                sField = sField.Substring(0, iMaxSize);
            }

            decimal cValue;
            if (decimal.TryParse(sToSize(iMaxSize, sField), NumberStyles.Number, CultureInfo.InvariantCulture, out cValue))
            {
                if (bZeroPadNumerics)
                {
                    return sZeroPad(iMaxSize, sField, "L");
                }

                if (!bKeepZeroes)
                {
                    sField = sRemoveLeadingZeroes(sField.TrimStart());
                }
            }
            else
            {
                sField = sField.TrimStart();
            }

            return sField;
        }

        public static string sCastNumCobol(int iMaxSize, int iDecimalPlaces, decimal cNumber)
        {
            if (iDecimalPlaces > iMaxSize)
            {
                throw new ArgumentException("Decimal places cannot exceed field length.");
            }

            var sSign = "+";
            if (cNumber < 0)
            {
                cNumber = Math.Abs(cNumber);
                sSign = "-";
            }

            var iIntegerPlaces = iMaxSize - iDecimalPlaces;
            var cMaxNumber = (decimal)Math.Pow(10, iIntegerPlaces) - (decimal)Math.Pow(10, -Math.Min(iDecimalPlaces, 4));
            if (cNumber > cMaxNumber)
            {
                cNumber = Math.Truncate(cMaxNumber * (decimal)Math.Pow(10, iDecimalPlaces)) / (decimal)Math.Pow(10, iDecimalPlaces);
            }

            var cScaled = Math.Round(cNumber, iDecimalPlaces, MidpointRounding.AwayFromZero) * (decimal)Math.Pow(10, iDecimalPlaces);
            var sNumber = decimal.Truncate(cScaled).ToString(CultureInfo.InvariantCulture);
            return sSign + sZeroPad(iMaxSize, sNumber, "L");
        }

        public static string sMassageUpc(string sUpc)
        {
            sUpc = sDeSpace(sUpc ?? string.Empty);
            if (sUpc.Length > 12)
            {
                sUpc = sUpc.Substring(sUpc.Length - 12);
            }

            decimal cValue;
            if (!decimal.TryParse(sUpc, NumberStyles.Number, CultureInfo.InvariantCulture, out cValue))
            {
                return string.Empty;
            }

            var sWithoutZeroes = sRemoveLeadingZeroes(sUpc);
            if (sWithoutZeroes.Length == 0)
            {
                return string.Empty;
            }

            if (sWithoutZeroes.Length > 6)
            {
                return sZeroPad(12, sUpc, "L");
            }

            return sZeroPad(12, sWithoutZeroes, "L");
        }

        public static string sMassageGtin(string sGtin)
        {
            sGtin = (sGtin ?? string.Empty).TrimStart();
            decimal cValue;
            if (!decimal.TryParse(sGtin, NumberStyles.Number, CultureInfo.InvariantCulture, out cValue))
            {
                return "00000000000000";
            }

            var sWithoutZeroes = sRemoveLeadingZeroes(sGtin);
            if (sWithoutZeroes.Length == 0)
            {
                return "00000000000000";
            }

            return sZeroPad(14, sWithoutZeroes, "L");
        }

        public static bool bCheckGtin(string sGtin)
        {
            sGtin = sGtin ?? string.Empty;
            if (sGtin == "00000000000000" || sGtin.Length != 14)
            {
                return false;
            }

            decimal cValue;
            if (!decimal.TryParse(sGtin, NumberStyles.Number, CultureInfo.InvariantCulture, out cValue))
            {
                return false;
            }

            return iComputeGtinCheckDigit(sGtin) == int.Parse(sGtin.Substring(13, 1), CultureInfo.InvariantCulture);
        }

        public static int iComputeGtinCheckDigit(string sGtin)
        {
            var iOddTotal = 0;
            var iEvenTotal = 0;
            for (var iIndex = 0; iIndex < 13; iIndex++)
            {
                var iDigit = int.Parse(sGtin.Substring(iIndex, 1), CultureInfo.InvariantCulture);
                if (iIndex % 2 == 0)
                {
                    iOddTotal += iDigit;
                }
                else
                {
                    iEvenTotal += iDigit;
                }
            }

            var iTotal = (3 * iOddTotal) + iEvenTotal;
            var iCheck = ((iTotal + 9) / 10 * 10) - iTotal;
            return iCheck == 10 ? 0 : iCheck;
        }

        public static int iAddCentury(int iYear)
        {
            if (iYear < 100)
            {
                return iYear < 80 ? iYear + 2000 : iYear + 1900;
            }

            return iYear;
        }

        public static string sReformatDate(DateTime dDate)
        {
            return dDate.Year.ToString("0000", CultureInfo.InvariantCulture)
                + dDate.Month.ToString("00", CultureInfo.InvariantCulture)
                + dDate.Day.ToString("00", CultureInfo.InvariantCulture);
        }

        public static string sNormFineCode(string sWhlCode, string sLoadCode)
        {
            sLoadCode = sLoadCode ?? string.Empty;
            if (sLoadCode.Length == 0)
            {
                return string.Empty;
            }

            sWhlCode = (sWhlCode ?? string.Empty).ToUpperInvariant();
            switch (sWhlCode)
            {
                case "ORGIL":
                    return sLoadCode.Length == 6 ? sLoadCode.Substring(1, 5) : string.Empty;
                case "HWI":
                    return sLoadCode.Length > 0 && sLoadCode.Substring(0, 1) == "0" && sLoadCode.Length >= 2
                        ? sLoadCode.Substring(2)
                        : sLoadCode;
                case "EMERY":
                case "JEN":
                    if (sLoadCode.Length == 6) return sLoadCode.Substring(2, 4);
                    if (sLoadCode.Length == 4) return sLoadCode;
                    return string.Empty;
                case "UNI":
                    if (sLoadCode.Length == 6) return sLoadCode.Substring(1, 5);
                    if (sLoadCode.Length == 5) return sLoadCode;
                    return string.Empty;
                default:
                    return sLoadCode;
            }
        }
    }
}
