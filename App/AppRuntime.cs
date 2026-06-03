using EcatDesktop.Services;

namespace EcatDesktop
{
    public sealed class AppRuntime
    {
        public AppRuntime(
            AppDataStore dataStore,
            JobSetupService jobSetupService,
            JobProcessingService jobProcessingService,
            ArchiveService archiveService)
        {
            DataStore = dataStore;
            JobSetupService = jobSetupService;
            JobProcessingService = jobProcessingService;
            ArchiveService = archiveService;
        }

        public AppDataStore DataStore { get; private set; }

        public JobSetupService JobSetupService { get; private set; }

        public JobProcessingService JobProcessingService { get; private set; }

        public ArchiveService ArchiveService { get; private set; }
    }
}
