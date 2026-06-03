using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Services
{
    public sealed class JobSetupService
    {
        private readonly AppDataStore _dataStore;

        public JobSetupService(AppDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public IList<WholesalerConfig> GetWholesalers()
        {
            try
            {
                Log.Information("Loading wholesalers for setup.");
                return _dataStore.LoadWholesalers()
                    .OrderBy(item => item.WhlCode)
                    .ToList();
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobSetupService.GetWholesalers failed.");
                throw;
            }
        }

        public JobRecord BuildSuggestedJob(string jobType, string wholesalerCode, string catMonth)
        {
            try
            {
                Log.Information("Building suggested job for " + wholesalerCode + ".");
                var wholesalers = _dataStore.LoadWholesalers();
                var generalConfig = _dataStore.LoadGeneralConfig();
                var job = new JobRecord();
                job.Select = "1";
                job.JobType = string.IsNullOrWhiteSpace(jobType) ? "CAT" : jobType.ToUpperInvariant();
                job.WhlCode = wholesalerCode == null ? string.Empty : wholesalerCode.Trim().ToUpperInvariant();
                job.CatMonth = string.IsNullOrWhiteSpace(catMonth) ? DateTime.Today.ToString("MMM").ToUpperInvariant() : catMonth.ToUpperInvariant();

                RecalculateJob(job, wholesalers, generalConfig);
                return job;
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobSetupService.BuildSuggestedJob failed.");
                throw;
            }
        }

        public void RecalculateJob(JobRecord job)
        {
            try
            {
                RecalculateJob(job, _dataStore.LoadWholesalers(), _dataStore.LoadGeneralConfig());
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobSetupService.RecalculateJob failed.");
                throw;
            }
        }

        public void SaveJobs(IEnumerable<JobRecord> jobs)
        {
            try
            {
                Log.Information("Saving setup jobs.");
                _dataStore.SaveQueue(jobs);
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobSetupService.SaveJobs failed.");
                throw;
            }
        }

        public string Validate(JobRecord job)
        {
            if (string.IsNullOrWhiteSpace(job.WhlCode))
            {
                return "WhlCode not filled in.";
            }

            if (string.IsNullOrWhiteSpace(job.WhlDescription))
            {
                return "WhlDescription not filled in.";
            }

            if (string.IsNullOrWhiteSpace(job.JobID))
            {
                return "JobID not filled in.";
            }

            return string.Empty;
        }

        private static void RecalculateJob(JobRecord job, IList<WholesalerConfig> wholesalers, GeneralConfig generalConfig)
        {
            job.JobType = string.IsNullOrWhiteSpace(job.JobType) ? "CAT" : job.JobType.ToUpperInvariant();
            job.WhlCode = job.WhlCode == null ? string.Empty : job.WhlCode.Trim().ToUpperInvariant();
            job.CatMonth = string.IsNullOrWhiteSpace(job.CatMonth) ? "JAN" : job.CatMonth.ToUpperInvariant();

            var wholesaler = wholesalers.FirstOrDefault(
                delegate(WholesalerConfig item)
                {
                    return item.WhlCode.Equals(job.WhlCode, StringComparison.OrdinalIgnoreCase);
                });

            if (wholesaler == null)
            {
                job.WhlDescription = string.Empty;
                job.JobID = string.Empty;
                job.EFMID = string.Empty;
                job.FromDate = null;
                job.ToDate = null;
                return;
            }

            job.WhlDescription = wholesaler.WhlDescription;

            switch (job.JobType)
            {
                case "EFM":
                    var nextId = wholesaler.NextEFMId <= 0 ? 1 : wholesaler.NextEFMId;
                    var efm = nextId.ToString("000");
                    job.EFMID = efm;
                    job.JobID = job.WhlCode + efm;
                    if (wholesaler.NextEFMFromDate.HasValue)
                    {
                        job.FromDate = wholesaler.NextEFMFromDate.Value.Date;
                        job.ToDate = wholesaler.NextEFMFromDate.Value.Date.AddDays(6);
                    }
                    else
                    {
                        job.FromDate = DateTime.Today.AddDays(-8);
                        job.ToDate = DateTime.Today.AddDays(-2);
                    }
                    break;

                case "CAT":
                    job.EFMID = string.Empty;
                    job.JobID = job.WhlCode + job.CatMonth;
                    var sourceDate = FindCatalogSourceDate(job, generalConfig);
                    if (sourceDate.HasValue)
                    {
                        job.FromDate = sourceDate.Value;
                    }
                    else if (!job.FromDate.HasValue)
                    {
                        job.FromDate = DateTime.Today;
                    }

                    job.ToDate = null;
                    break;

                case "INH":
                    job.EFMID = string.Empty;
                    job.JobID = job.WhlCode + job.CatMonth;
                    job.FromDate = null;
                    job.ToDate = null;
                    break;

                default:
                    job.EFMID = string.Empty;
                    job.JobID = string.Empty;
                    job.FromDate = null;
                    job.ToDate = null;
                    break;
            }
        }

        private static DateTime? FindCatalogSourceDate(JobRecord job, GeneralConfig generalConfig)
        {
            var jobId = (job.JobID ?? string.Empty).Trim();
            var whlCode = (job.WhlCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(jobId) || string.IsNullOrWhiteSpace(whlCode))
            {
                return null;
            }

            foreach (var baseDirectory in new[] { generalConfig.RemoteArchive, generalConfig.RemoteRaw })
            {
                var normalizedBaseDirectory = NormalizeDirectory(baseDirectory);
                foreach (var candidate in BuildCatalogItemCandidates(whlCode))
                {
                    var sourcePath = Path.Combine(normalizedBaseDirectory, jobId, candidate);
                    if (File.Exists(sourcePath))
                    {
                        return File.GetLastWriteTime(sourcePath).Date;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> BuildCatalogItemCandidates(string whlCode)
        {
            if (whlCode.Equals("HHH", StringComparison.OrdinalIgnoreCase))
            {
                yield return "HHH_I.DAT";
                yield return "HHH_i.dat";
                yield break;
            }

            yield return whlCode + "_I.DAT";
            yield return whlCode + "_I.272";
            yield return whlCode + "_I.250";
            yield return whlCode + "_I.329";
        }

        private static string NormalizeDirectory(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            return configuredPath.Replace('\\', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}
