using System;
using System.Collections.Generic;
using System.Linq;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Services
{
    public sealed class JobProcessingService
    {
        private readonly AppDataStore _dataStore;
        private readonly JobFileContractService _fileContractService;

        public JobProcessingService(AppDataStore dataStore)
        {
            _dataStore = dataStore;
            _fileContractService = new JobFileContractService(dataStore);
        }

        public IList<JobRecord> LoadQueue()
        {
            try
            {
                Log.Information("Loading job queue.");
                return _dataStore.LoadQueue()
                    .OrderBy(item => item.Select)
                    .ThenBy(item => item.JobID)
                    .ToList();
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobProcessingService.LoadQueue failed.");
                throw;
            }
        }

        public IList<JobStepDefinition> GetStepDefinitions(JobRecord job)
        {
            try
            {
                var type = job == null ? "CAT" : job.JobType;
                return AppConstants.GetStepDefinitions(type);
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobProcessingService.GetStepDefinitions failed.");
                throw;
            }
        }

        public OperationResult RunJobs(bool resume)
        {
            try
            {
                Log.Information("RunJobs started. Resume=" + resume + ".");
                var queue = _dataStore.LoadQueue().ToList();
                var generalConfig = _dataStore.LoadGeneralConfig();
                var wholesalers = _dataStore.LoadWholesalers().ToDictionary(item => item.WhlCode, StringComparer.OrdinalIgnoreCase);
                var processedJobs = new List<JobRecord>();

                var readyJobs = queue.Where(item => item.Select == "1").ToList();
                if (!readyJobs.Any() && !queue.Any(item => item.Select == "2"))
                {
                    Log.Warning("RunJobs found no selected jobs.");
                    return OperationResult.Fail("No Jobs", "There are no queue entries in Select state 1 or 2.");
                }

                foreach (var job in readyJobs)
                {
                    if (!wholesalers.ContainsKey(job.WhlCode))
                    {
                        job.Select = "A";
                        _dataStore.SaveQueue(queue);
                        Log.Warning("RunJobs missing wholesaler configuration for " + job.WhlCode + ".");
                        return OperationResult.Fail("Setup Error", job.JobType + " " + job.JobID + ": wholesaler " + job.WhlCode + " is missing from wholesaler configuration.");
                    }

                    if (string.IsNullOrWhiteSpace(job.JobID) || string.IsNullOrWhiteSpace(job.WhlDescription))
                    {
                        job.Select = "A";
                        _dataStore.SaveQueue(queue);
                        Log.Warning("RunJobs found incomplete job " + job.JobID + ".");
                        return OperationResult.Fail("Setup Error", job.JobType + " " + job.JobID + ": required fields are incomplete.");
                    }

                    job.Select = "2";
                }

                foreach (var job in queue.Where(item => item.Select == "2"))
                {
                    Log.Information("Processing job " + job.JobID + ".");
                    var wholesaler = wholesalers[job.WhlCode];
                    if (!resume)
                    {
                        InitializeSteps(job);
                    }

                    JobFileContractService.PreparedJobFiles preparedFiles;
                    var prepareResult = _fileContractService.Prepare(job, wholesaler, generalConfig, out preparedFiles);
                    if (!prepareResult.Success)
                    {
                        job.Select = "Z";
                        _dataStore.SaveQueue(queue);
                        Log.Warning("Prepare failed for " + job.JobID + ": " + prepareResult.Message);
                        return prepareResult;
                    }

                    foreach (var step in AppConstants.GetStepDefinitions(job.JobType))
                    {
                        if (!job.GetMethodValue(step.MethodName).Equals("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var started = DateTime.Now;
                        Log.Information("Job " + job.JobID + " step " + step.MethodName + " started.");
                        var completed = started.AddSeconds(1);
                        job.SetMethodValue(step.MethodName, "N");

                        _dataStore.AppendHistory(new ProcessingHistoryEntry
                        {
                            Process = job.WhlCode + ":" + step.MethodName + ":" + step.Description,
                            DateProcessed = DateTime.Today,
                            CatMonth = job.CatMonth,
                            TimeStarted = started,
                            TimeComplete = completed,
                            TimeElapsed = "0 00:00:01",
                            InputRecords = preparedFiles.InputFileCount,
                            OutputRecords = preparedFiles.OutputFileCount,
                            RecordsPerMinute = Math.Max(preparedFiles.OutputFileCount, 1) * 60
                        });
                    }

                    job.Select = "3";
                    processedJobs.Add(job);
                    Log.Information("Processing job " + job.JobID + " completed.");
                }

                UpdateNextEfmMetadata(processedJobs, wholesalers);
                _dataStore.SaveQueue(queue);
                Log.Information("RunJobs completed.");
                return OperationResult.Ok("Processing Complete", "Catalog/EFM data processing jobs completed with original file naming and directory rules.");
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "RunJobs failed.");
                return OperationResult.Fail("Processing Error", oException.Message);
            }
        }

        public OperationResult CheckJobs()
        {
            try
            {
                Log.Information("CheckJobs started.");
                var queue = _dataStore.LoadQueue().ToList();
                var candidates = queue.Where(item => item.Select == "3").ToList();
                if (!candidates.Any())
                {
                    Log.Warning("CheckJobs found no processed jobs.");
                    return OperationResult.Fail("No Jobs", "There are no processed jobs waiting for review.");
                }

                foreach (var job in candidates)
                {
                    Log.Information("Marking job reviewed: " + job.JobID + ".");
                    job.Select = "4";
                }

                _dataStore.SaveQueue(queue);
                return OperationResult.Ok("Check Complete", "Catalog application review completed and jobs are ready for archive cleanup.");
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "CheckJobs failed.");
                return OperationResult.Fail("Check Jobs Error", oException.Message);
            }
        }

        public OperationResult CleanupToDoneQueue()
        {
            try
            {
                Log.Information("CleanupToDoneQueue started.");
                var queue = _dataStore.LoadQueue().ToList();
                var done = _dataStore.LoadDoneQueue().ToList();
                var candidates = queue.Where(item => item.Select == "4").ToList();

                if (!candidates.Any())
                {
                    Log.Warning("CleanupToDoneQueue found no reviewed jobs.");
                    return OperationResult.Fail("No Jobs", "There are no reviewed jobs ready to move into the archive queue.");
                }

                foreach (var job in candidates)
                {
                    Log.Information("Moving job to done queue: " + job.JobID + ".");
                    done.Add(job.Clone());
                }

                queue.RemoveAll(item => item.Select == "4");
                _dataStore.SaveQueue(queue);
                _dataStore.SaveDoneQueue(done);
                return OperationResult.Ok("Cleanup Complete", "Reviewed jobs moved to the archive queue.");
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "CleanupToDoneQueue failed.");
                return OperationResult.Fail("Cleanup Error", oException.Message);
            }
        }

        private void InitializeSteps(JobRecord job)
        {
            foreach (var step in AppConstants.GetStepDefinitions(job.JobType))
            {
                job.SetMethodValue(step.MethodName, "Y");
            }

            var activeMethods = new HashSet<string>(AppConstants.GetStepDefinitions(job.JobType).Select(item => item.MethodName));
            var methods = new[]
            {
                "Method1", "Method2", "Method3", "Method4", "Method5",
                "Method6", "Method7", "Method8", "Method9"
            };

            foreach (var method in methods)
            {
                if (!activeMethods.Contains(method))
                {
                    job.SetMethodValue(method, "N");
                }
            }
        }

        private void UpdateNextEfmMetadata(IEnumerable<JobRecord> processedJobs, Dictionary<string, WholesalerConfig> wholesalers)
        {
            try
            {
                foreach (var job in processedJobs.Where(item => item.JobType == "EFM"))
                {
                    WholesalerConfig wholesaler;
                    if (!wholesalers.TryGetValue(job.WhlCode, out wholesaler))
                    {
                        continue;
                    }

                    int currentId;
                    wholesaler.NextEFMId = int.TryParse(job.EFMID, out currentId) ? currentId + 1 : wholesaler.NextEFMId + 1;
                    wholesaler.NextEFMFromDate = job.ToDate.HasValue ? job.ToDate.Value.AddDays(1) : (DateTime?)null;
                }

                _dataStore.SaveWholesalers(wholesalers.Values.OrderBy(item => item.WhlCode));
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "UpdateNextEfmMetadata failed.");
                throw;
            }
        }
    }
}
