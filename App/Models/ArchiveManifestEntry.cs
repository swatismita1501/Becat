namespace EcatDesktop.Models
{
    public sealed class ArchiveManifestEntry
    {
        public ArchiveManifestEntry()
        {
            Directory = string.Empty;
            ZipFile = string.Empty;
        }

        public string Directory { get; set; }

        public string ZipFile { get; set; }
    }
}
