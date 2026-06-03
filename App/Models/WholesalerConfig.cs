using System;

namespace EcatDesktop.Models
{
    public sealed class WholesalerConfig
    {
        public WholesalerConfig()
        {
            WhlCode = string.Empty;
            WhlDescription = string.Empty;
            Debug = "N";
            EFMPrefix = string.Empty;
        }

        public string WhlCode { get; set; }

        public string WhlDescription { get; set; }

        public int NextEFMId { get; set; }

        public DateTime? NextEFMFromDate { get; set; }

        public string Debug { get; set; }

        public string EFMPrefix { get; set; }
    }
}
