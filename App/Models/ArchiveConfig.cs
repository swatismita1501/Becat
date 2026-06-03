namespace EcatDesktop.Models
{
    public sealed class ArchiveConfig
    {
        public ArchiveConfig()
        {
            RemoteRaw = string.Empty;
            RemoteArchive = string.Empty;
            ZipArchive = string.Empty;
        }

        public string RemoteRaw { get; set; }

        public string RemoteArchive { get; set; }

        public string ZipArchive { get; set; }
    }
}
