using System.Collections.Generic;
using EcatDesktop.Models;

namespace EcatDesktop
{
    public static class AppConstants
    {
        public static readonly string[] CatalogMonths =
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        public static IList<JobStepDefinition> GetStepDefinitions(string jobType)
        {
            var normalized = string.IsNullOrEmpty(jobType) ? "CAT" : jobType.ToUpperInvariant();

            if (normalized == "EFM")
            {
                return new List<JobStepDefinition>
                {
                    new JobStepDefinition("Method1", "Setup for processing & import raw data."),
                    new JobStepDefinition("Method2", "Process EFM data."),
                    new JobStepDefinition("Method3", "Export EFM data.")
                };
            }

            if (normalized == "INH")
            {
                return new List<JobStepDefinition>
                {
                    new JobStepDefinition("Method1", "Setup for processing & import raw data."),
                    new JobStepDefinition("Method2", "Item data to catalog."),
                    new JobStepDefinition("Method3", "Massage data in catalog."),
                    new JobStepDefinition("Method4", "Mfg data to catalog."),
                    new JobStepDefinition("Method5", "Whl mfg data to catalog."),
                    new JobStepDefinition("Method6", "Long descriptions to catalog."),
                    new JobStepDefinition("Method7", "Export data."),
                    new JobStepDefinition("Method8", "Build CDA catalog.")
                };
            }

            return new List<JobStepDefinition>
            {
                new JobStepDefinition("Method1", "Setup for processing & import raw data."),
                new JobStepDefinition("Method2", "Process catalog data."),
                new JobStepDefinition("Method3", "Export catalog data.")
            };
        }
    }
}
