namespace EcatDesktop.Models
{
    public sealed class GeneralConfig
    {
        public GeneralConfig()
        {
            DAPrd = string.Empty;
            RemoteRaw = string.Empty;
            LocalWork = string.Empty;
            Reference = string.Empty;
            RemoteArchive = string.Empty;
            ZipArchive = string.Empty;
            CDADir = string.Empty;
            CDATemp = string.Empty;
            CDACatalog = string.Empty;
            CallCDABat = string.Empty;
        }

        public string DAPrd { get; set; }

        public string RemoteRaw { get; set; }

        public string LocalWork { get; set; }

        public string Reference { get; set; }

        public string RemoteArchive { get; set; }

        public string ZipArchive { get; set; }

        public string CDADir { get; set; }

        public string CDATemp { get; set; }

        public string CDACatalog { get; set; }

        public string CallCDABat { get; set; }
    }
}
