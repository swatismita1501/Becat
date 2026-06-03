using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Services
{
    public sealed class ArchiveService
    {
        private readonly AppDataStore _dataStore;

        public ArchiveService(AppDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public ArchiveConfig LoadConfig()
        {
            try
            {
                Log.Information("Loading archive configuration.");
                return _dataStore.LoadArchiveConfig();
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "ArchiveService.LoadConfig failed.");
                throw;
            }
        }

        public void SaveConfig(ArchiveConfig config)
        {
            try
            {
                Log.Information("Saving archive configuration.");
                _dataStore.SaveArchiveConfig(config);
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "ArchiveService.SaveConfig failed.");
                throw;
            }
        }

        public IList<JobRecord> LoadDoneQueue()
        {
            try
            {
                Log.Information("Loading done queue.");
                return _dataStore.LoadDoneQueue()
                    .OrderBy(item => item.JobID)
                    .ToList();
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "ArchiveService.LoadDoneQueue failed.");
                throw;
            }
        }

        public OperationResult ArchiveReadyJobs()
        {
            try
            {
                Log.Information("ArchiveReadyJobs started.");
                var doneQueue = _dataStore.LoadDoneQueue().ToList();
                var archiveReady = doneQueue.Where(item => item.Select == "4").ToList();
                if (!archiveReady.Any())
                {
                    Log.Warning("ArchiveReadyJobs found no archive-ready jobs.");
                    return OperationResult.Fail("No Jobs", "There are no archive-ready jobs in the done queue.");
                }

                var archiveConfig = _dataStore.LoadArchiveConfig();
                var generalConfig = _dataStore.LoadGeneralConfig();
                var manifest = _dataStore.LoadArchiveManifest().ToList();
                var currentBatchEntries = new List<ArchiveManifestEntry>();
                var scriptLines = new List<string>();

                foreach (var job in archiveReady)
                {
                    Log.Information("Archiving job " + job.JobID + ".");
                    var yymm = FigureArchiveYymm(job.CatMonth);
                    var prefix = job.WhlCode.Length <= 4 ? job.WhlCode.ToUpperInvariant() : job.WhlCode.Substring(0, 4).ToUpperInvariant();
                    var zipFileName = prefix + yymm + ".zip";
                    var rawDirectory = Path.Combine(NormalizePath(archiveConfig.RemoteRaw), job.JobID);
                    var archiveDirectory = Path.Combine(NormalizePath(archiveConfig.RemoteArchive), job.JobID);

                    var rawEntry = new ArchiveManifestEntry { Directory = rawDirectory, ZipFile = zipFileName };
                    var archiveEntry = new ArchiveManifestEntry { Directory = archiveDirectory, ZipFile = zipFileName };

                    manifest.Add(rawEntry);
                    manifest.Add(archiveEntry);
                    currentBatchEntries.Add(rawEntry);
                    currentBatchEntries.Add(archiveEntry);

                    if (job.JobType.Equals("EFM", StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureNextEfmDirectory(job, generalConfig);
                    }

                    scriptLines.Add("ZIP " + zipFileName + " <= " + rawDirectory);
                    scriptLines.Add("ZIP " + zipFileName + " <= " + archiveDirectory);
                    job.Select = "5";
                }

                var normalizedManifest = manifest
                    .GroupBy(item => item.Directory + "|" + item.ZipFile, StringComparer.OrdinalIgnoreCase)
                    .Select(group => group.First())
                    .OrderBy(item => item.ZipFile)
                    .ThenBy(item => item.Directory)
                    .ToList();

                var affectedZipFiles = currentBatchEntries
                    .Select(item => item.ZipFile)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var manifestToRebuild = normalizedManifest
                    .Where(item => affectedZipFiles.Contains(item.ZipFile, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                _dataStore.SaveArchiveManifest(normalizedManifest);
                DeleteCurrentArchivePackages(archiveConfig, affectedZipFiles);
                RebuildArchivePackages(archiveConfig, manifestToRebuild);

                var scriptPath = Path.Combine(_dataStore.ArchiveOutputDirectory, "ECARCHIV_generated.txt");
                File.WriteAllLines(scriptPath, scriptLines.ToArray());

                doneQueue.RemoveAll(item => item.Select == "5");
                _dataStore.SaveDoneQueue(doneQueue);

                Log.Information("ArchiveReadyJobs completed. Script: " + scriptPath);
                return OperationResult.Ok("Archive Complete", "Archive manifest and package output were written to " + scriptPath + ".");
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "ArchiveReadyJobs failed.");
                return OperationResult.Fail("Archive Error", oException.Message);
            }
        }

        private void RebuildArchivePackages(ArchiveConfig archiveConfig, IList<ArchiveManifestEntry> manifest)
        {
            try
            {
                var archiveRoot = NormalizePath(archiveConfig.ZipArchive, _dataStore.ArchiveOutputDirectory);
                var groups = manifest.GroupBy(item => item.ZipFile, StringComparer.OrdinalIgnoreCase);

                foreach (var group in groups)
                {
                    var zipPath = Path.Combine(archiveRoot, group.Key);
                    var entries = new List<AppDataStore.ZipDirectoryEntry>();

                    foreach (var entry in group)
                    {
                        var entryPrefix = Path.GetFileName(entry.Directory);
                        entries.Add(new AppDataStore.ZipDirectoryEntry(entry.Directory, entryPrefix));
                    }

                    Log.Information("Creating archive package " + zipPath + ".");
                    _dataStore.CreateOrReplaceZip(zipPath, entries);
                }
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "RebuildArchivePackages failed.");
                throw;
            }
        }

        private void DeleteCurrentArchivePackages(ArchiveConfig archiveConfig, IList<string> affectedZipFiles)
        {
            try
            {
                var archiveRoot = NormalizePath(archiveConfig.ZipArchive, _dataStore.ArchiveOutputDirectory);
                if (!Directory.Exists(archiveRoot))
                {
                    return;
                }

                foreach (var zipPath in Directory.EnumerateFiles(archiveRoot, "*.zip", SearchOption.TopDirectoryOnly))
                {
                    var zipFileName = Path.GetFileName(zipPath);
                    if (affectedZipFiles.Contains(zipFileName, StringComparer.OrdinalIgnoreCase))
                    {
                        Log.Information("Deleting archive package before recreate " + zipPath + ".");
                        File.Delete(zipPath);
                    }
                }
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "DeleteCurrentArchivePackages failed.");
                throw;
            }
        }

        private void EnsureNextEfmDirectory(JobRecord job, GeneralConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.RemoteRaw))
                {
                    return;
                }

                var currentDirectory = Path.Combine(NormalizePath(config.RemoteRaw), job.JobID);
                if (!Directory.Exists(currentDirectory))
                {
                    return;
                }

                int currentId;
                var nextId = int.TryParse(job.EFMID, out currentId) ? currentId + 1 : 1;
                var nextJobId = job.WhlCode + nextId.ToString("000");
                var nextDirectory = Path.Combine(NormalizePath(config.RemoteRaw), nextJobId);

                if (!Directory.Exists(nextDirectory))
                {
                    Directory.CreateDirectory(nextDirectory);
                }

                var latestLdFile = Directory.EnumerateFiles(currentDirectory, "LD*.DAT", SearchOption.TopDirectoryOnly)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .LastOrDefault();

                if (latestLdFile == null)
                {
                    return;
                }

                var destination = Path.Combine(nextDirectory, Path.GetFileName(latestLdFile));
                if (!File.Exists(destination))
                {
                    File.Copy(latestLdFile, destination);
                }
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "EnsureNextEfmDirectory failed for " + job.JobID + ".");
                throw;
            }
        }

        private static string FigureArchiveYymm(string month)
        {
            var buildDate = DateTime.Today;
            var catalogMonth = Array.IndexOf(AppConstants.CatalogMonths, month.ToUpperInvariant()) + 1;
            if (catalogMonth <= 0)
            {
                catalogMonth = buildDate.Month;
            }

            var year = buildDate.Year % 100;
            if (catalogMonth > buildDate.Month)
            {
                year--;
            }

            return year.ToString("00") + catalogMonth.ToString("00");
        }

        private static string NormalizePath(string configuredPath, string fallback)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return fallback;
            }

            return configuredPath.Replace('\\', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }

        private static string NormalizePath(string configuredPath)
        {
            return NormalizePath(configuredPath, string.Empty);
        }
    }
}
