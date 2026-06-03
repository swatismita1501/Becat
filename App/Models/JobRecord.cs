namespace EcatDesktop.Models
{
    public sealed class JobRecord
    {
        public JobRecord()
        {
            Select = "1";
            JobType = "CAT";
            JobID = string.Empty;
            WhlCode = string.Empty;
            WhlDescription = string.Empty;
            EFMID = string.Empty;
            CatMonth = "JAN";
            Method1 = "N";
            Method2 = "N";
            Method3 = "N";
            Method4 = "N";
            Method5 = "N";
            Method6 = "N";
            Method7 = "N";
            Method8 = "N";
            Method9 = "N";
        }

        public string Select { get; set; }

        public string JobType { get; set; }

        public string JobID { get; set; }

        public string WhlCode { get; set; }

        public string WhlDescription { get; set; }

        public System.DateTime? FromDate { get; set; }

        public System.DateTime? ToDate { get; set; }

        public string EFMID { get; set; }

        public string CatMonth { get; set; }

        public string Method1 { get; set; }

        public string Method2 { get; set; }

        public string Method3 { get; set; }

        public string Method4 { get; set; }

        public string Method5 { get; set; }

        public string Method6 { get; set; }

        public string Method7 { get; set; }

        public string Method8 { get; set; }

        public string Method9 { get; set; }

        public JobRecord Clone()
        {
            return (JobRecord)MemberwiseClone();
        }

        public string GetMethodValue(string methodName)
        {
            switch (methodName)
            {
                case "Method1":
                    return Method1;
                case "Method2":
                    return Method2;
                case "Method3":
                    return Method3;
                case "Method4":
                    return Method4;
                case "Method5":
                    return Method5;
                case "Method6":
                    return Method6;
                case "Method7":
                    return Method7;
                case "Method8":
                    return Method8;
                case "Method9":
                    return Method9;
                default:
                    return "N";
            }
        }

        public void SetMethodValue(string methodName, string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? "N" : value.ToUpperInvariant();

            switch (methodName)
            {
                case "Method1":
                    Method1 = normalized;
                    break;
                case "Method2":
                    Method2 = normalized;
                    break;
                case "Method3":
                    Method3 = normalized;
                    break;
                case "Method4":
                    Method4 = normalized;
                    break;
                case "Method5":
                    Method5 = normalized;
                    break;
                case "Method6":
                    Method6 = normalized;
                    break;
                case "Method7":
                    Method7 = normalized;
                    break;
                case "Method8":
                    Method8 = normalized;
                    break;
                case "Method9":
                    Method9 = normalized;
                    break;
            }
        }
    }
}
