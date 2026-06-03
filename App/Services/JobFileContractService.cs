using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EcatDesktop.Common;
using EcatDesktop.Models;
using EcatDesktop.Services.Stdcat;

namespace EcatDesktop.Services
{
    public sealed class JobFileContractService
    {
        public sealed class PreparedJobFiles
        {
            public PreparedJobFiles()
            {
                InputFileCount = 0;
                OutputFileCount = 0;
                Summary = string.Empty;
            }

            public int InputFileCount { get; set; }

            public int OutputFileCount { get; set; }

            public string Summary { get; set; }
        }

        private sealed class JobPaths
        {
            public string RemoteRawDirectory { get; set; }

            public string RemoteArchiveDirectory { get; set; }

            public string RemoteReferenceDirectory { get; set; }

            public string RemoteDaprdDirectory { get; set; }

            public string LocalWorkDirectory { get; set; }

            public string LocalFinalDirectory { get; set; }

            public string InhouseCatalogDirectory { get; set; }

            public string RoverDirectory { get; set; }
        }

        private sealed class InputRule
        {
            public InputRule()
            {
                CandidateFileNames = new List<string>();
                MatchedFileNames = new List<string>();
            }

            public string Description { get; set; }

            public string DirectoryPath { get; set; }

            public IList<string> CandidateFileNames { get; private set; }

            public IList<string> MatchedFileNames { get; private set; }

            public int MinimumMatches { get; set; }

            public int MaximumMatches { get; set; }
        }

        private readonly AppDataStore _dataStore;
        private readonly StdcatCatEngine _stdcatCatEngine;

        public JobFileContractService(AppDataStore dataStore)
        {
            _dataStore = dataStore;
            _stdcatCatEngine = new StdcatCatEngine(AppDomain.CurrentDomain.BaseDirectory);
        }

        public OperationResult Prepare(JobRecord job, WholesalerConfig wholesaler, GeneralConfig config, out PreparedJobFiles prepared)
        {
            prepared = null;
            try
            {
                Log.Information("Preparing file contract for job " + (job == null ? "<null>" : job.JobID) + ".");
                var normalizedType = (job.JobType ?? "CAT").ToUpperInvariant();
                switch (normalizedType)
                {
                    case "EFM":
                    // return PrepareEfm(job, wholesaler, config, out prepared);
                    case "INH":
                        return PrepareInhouse(job, wholesaler, config, out prepared);
                    default:
                        return PrepareCatalog(job, wholesaler, config, out prepared);
                }
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "JobFileContractService.Prepare failed.");
                return OperationResult.Fail("File Contract Error", oException.Message);
            }
        }

        private OperationResult PrepareCatalog(JobRecord job, WholesalerConfig wholesaler, GeneralConfig config, out PreparedJobFiles prepared)
        {
            var paths = BuildPaths(job, config);
            var inputs = BuildCatalogProfile(job, paths);

            var validation = ValidateInputs(inputs);
            if (validation != null)
            {
                prepared = null;
                return validation;
            }

            EnsureDirectories(paths.RemoteRawDirectory, paths.RemoteArchiveDirectory, paths.LocalWorkDirectory, paths.LocalFinalDirectory);
            StageMatchedInputs(inputs, paths.LocalWorkDirectory);

            if ((job.WhlCode ?? string.Empty).Equals("HHH", StringComparison.OrdinalIgnoreCase))
            {
                return PrepareHouseHassonCatalog(job, paths, inputs, out prepared);
            }

            if ((job.WhlCode ?? string.Empty).Equals("EJD", StringComparison.OrdinalIgnoreCase))
            {
                return PrepareEjdCatalog(job, paths, inputs, out prepared);
            }

            var itemInput = FindMatchedInputPath(inputs, "Catalog item raw input");
            var upcInput = FindMatchedInputPath(inputs, "Catalog UPC raw input");
            var longDescriptionInput = FindMatchedInputPath(inputs, "Catalog long-description input");
            var summaryPath = Path.Combine(paths.LocalFinalDirectory, job.JobID + ".DB");
            var itemRawPath = Path.Combine(paths.LocalFinalDirectory, "ITEM_RAW.DB");
            var upcRawPath = Path.Combine(paths.LocalFinalDirectory, "UPC_RAW.DB");
            var itemExtPath = Path.Combine(paths.LocalFinalDirectory, "ITEMEXT.DB");
            var itemDbPath = Path.Combine(paths.LocalFinalDirectory, "ITEM.DB");
            var deptDbPath = Path.Combine(paths.LocalFinalDirectory, "DEPT.DB");
            var clasDbPath = Path.Combine(paths.LocalFinalDirectory, "CLAS.DB");
            var fineDbPath = Path.Combine(paths.LocalFinalDirectory, "FINE.DB");
            var vendDbPath = Path.Combine(paths.LocalFinalDirectory, "VEND.DB");

            CopySourceOrPlaceholder(itemInput, itemRawPath, job, "Catalog Data", inputs);
            CopySourceOrPlaceholder(upcInput, upcRawPath, job, "Catalog Data", inputs);
            CopySourceOrPlaceholder(longDescriptionInput, itemExtPath, job, "Catalog Data", inputs);
            CopySeedTemplateOrPlaceholder("ITEM_OUT.DB", itemDbPath, job, "Catalog Output Template", inputs);
            CopySeedTemplateOrPlaceholder("DEPT_OUT.DB", deptDbPath, job, "Catalog Output Template", inputs);
            CopySeedTemplateOrPlaceholder("CLAS_OUT.DB", clasDbPath, job, "Catalog Output Template", inputs);
            CopySeedTemplateOrPlaceholder("FINE_OUT.DB", fineDbPath, job, "Catalog Output Template", inputs);
            CopySeedTemplateOrPlaceholder("VEND_OUT.DB", vendDbPath, job, "Catalog Output Template", inputs);
            WritePlaceholder(summaryPath, BuildPlaceholderContents(job, "Catalog Data", Path.GetFileName(summaryPath), inputs));

            _stdcatCatEngine.GenerateGenericCatalog(
                itemInput,
                upcInput,
                longDescriptionInput,
                job.WhlCode,
                paths.LocalFinalDirectory);

            var localDbFiles = new[]
            {
                itemRawPath,
                upcRawPath,
                itemExtPath,
                itemDbPath,
                deptDbPath,
                clasDbPath,
                fineDbPath,
                vendDbPath,
                summaryPath
            };

            var exportFiles = new[]
            {
                CopyGeneratedExport(paths.LocalFinalDirectory, "ITEM.TXT", "item.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "DEPT.TXT", "dept.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "CLAS.TXT", "clas.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "FINE.TXT", "fine.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "VEND.TXT", "vend.txt")
            };

            var zipPath = Path.Combine(paths.LocalFinalDirectory, BuildCatalogZipFileName(job.JobID));
            CreateZipFromFiles(zipPath, exportFiles, paths.LocalWorkDirectory);

            foreach (var exportFile in exportFiles)
            {
                CopyFile(exportFile, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(exportFile)));
            }

            CopyFile(summaryPath, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(summaryPath)));
            CopyFile(summaryPath, Path.Combine(paths.RemoteRawDirectory, Path.GetFileName(summaryPath)));
            CopyFile(zipPath, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(zipPath)));

            prepared = new PreparedJobFiles
            {
                InputFileCount = CountMatches(inputs),
                OutputFileCount = localDbFiles.Length + exportFiles.Length + 1,
                Summary = "Catalog files validated and created using the original job folder and file names."
            };

            return OperationResult.Ok("Ready", prepared.Summary);
        }

        private OperationResult PrepareEjdCatalog(JobRecord job, JobPaths paths, IList<InputRule> inputs, out PreparedJobFiles prepared)
        {
            var itemInput = FindMatchedInputPath(inputs, "EJD item input");
            var longDescriptionInput = FindMatchedInputPath(inputs, "EJD long-description input");
            var departmentInput = FindMatchedInputPath(inputs, "EJD department input");
            var classInput = FindMatchedInputPath(inputs, "EJD class input");
            var vendorInput = FindMatchedInputPath(inputs, "EJD vendor input");
            var summaryPath = Path.Combine(paths.LocalFinalDirectory, job.JobID + ".DB");
            var localDbFiles = new[]
            {
                Path.Combine(paths.LocalFinalDirectory, "ITEM_RAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "ITEMEXT.DB"),
                Path.Combine(paths.LocalFinalDirectory, "DEPT.DB"),
                Path.Combine(paths.LocalFinalDirectory, "CLAS.DB"),
                Path.Combine(paths.LocalFinalDirectory, "FINE.DB"),
                Path.Combine(paths.LocalFinalDirectory, "VEND.DB"),
                summaryPath
            };

            _stdcatCatEngine.GenerateEjdCatalog(
                itemInput,
                longDescriptionInput,
                departmentInput,
                classInput,
                vendorInput,
                job.WhlCode,
                paths.LocalFinalDirectory);

            CopyBinaryFile(itemInput, localDbFiles[0]);
            CopyBinaryFile(longDescriptionInput, localDbFiles[1]);
            CopyBinaryFile(departmentInput, localDbFiles[2]);
            CopyBinaryFile(classInput, localDbFiles[3]);
            WritePlaceholder(localDbFiles[4], string.Empty);
            if (!string.IsNullOrWhiteSpace(vendorInput) && File.Exists(vendorInput))
            {
                CopyBinaryFile(vendorInput, localDbFiles[5]);
            }
            else
            {
                WritePlaceholder(localDbFiles[5], string.Empty);
            }
            WritePlaceholder(summaryPath, BuildPlaceholderContents(job, "EJD Catalog Summary", Path.GetFileName(summaryPath), inputs));

            var exportFiles = new[]
            {
                CopyGeneratedExport(paths.LocalFinalDirectory, "ITEM.TXT", "item.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "DEPT.TXT", "dept.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "CLAS.TXT", "clas.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "FINE.TXT", "fine.txt"),
                CopyGeneratedExport(paths.LocalFinalDirectory, "VEND.TXT", "vend.txt")
            };

            var zipPath = Path.Combine(paths.LocalFinalDirectory, BuildCatalogZipFileName(job.JobID));
            CreateZipFromFiles(zipPath, exportFiles, paths.LocalWorkDirectory);

            foreach (var exportFile in exportFiles)
            {
                CopyFile(exportFile, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(exportFile)));
            }

            foreach (var dbFile in localDbFiles)
            {
                CopyFile(dbFile, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(dbFile)));
            }

            CopyFile(summaryPath, Path.Combine(paths.RemoteRawDirectory, Path.GetFileName(summaryPath)));
            CopyFile(zipPath, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(zipPath)));

            prepared = new PreparedJobFiles
            {
                InputFileCount = CountMatches(inputs),
                OutputFileCount = localDbFiles.Length + exportFiles.Length + 1,
                Summary = "EJD catalog files validated and generated from the staged EJD source pack."
            };

            return OperationResult.Ok("Ready", prepared.Summary);
        }

        private OperationResult PrepareHouseHassonCatalog(JobRecord job, JobPaths paths, IList<InputRule> inputs, out PreparedJobFiles prepared)
        {
            var itemInput = FindMatchedInputPath(inputs, "HHH item input");
            var longDescriptionInput = FindMatchedInputPath(inputs, "HHH long-description input");
            var departmentInput = FindMatchedInputPath(inputs, "HHH department input");
            var classInput = FindMatchedInputPath(inputs, "HHH class input");
            var fineInput = FindMatchedInputPath(inputs, "HHH fineline input");
            var vendorInput = FindMatchedInputPath(inputs, "HHH vendor input");
            var localLtvmPath = StageVendorInputAsLtvm(paths, vendorInput);


            var itemText = Path.Combine(paths.LocalFinalDirectory, "ITEM.TXT");
            var deptText = Path.Combine(paths.LocalFinalDirectory, "DEPT.TXT");
            var clasText = Path.Combine(paths.LocalFinalDirectory, "CLAS.TXT");
            var fineText = Path.Combine(paths.LocalFinalDirectory, "FINE.TXT");
            var vendText = Path.Combine(paths.LocalFinalDirectory, "VEND.TXT");

            _stdcatCatEngine.GenerateHouseHassonCatalog(
                itemInput,
                longDescriptionInput,
                departmentInput,
                classInput,
                fineInput,
                localLtvmPath,
                job.WhlCode,
                paths.LocalFinalDirectory,
                job.FromDate);

            var summaryPath = Path.Combine(paths.LocalFinalDirectory, job.JobID + ".DB");
            var skusDbPath = Path.Combine(paths.LocalFinalDirectory, BuildCatalogSkuDbFileName(job));
            var localDbFiles = new[]
            {
                Path.Combine(paths.LocalFinalDirectory, "CLAS.DB"),
                Path.Combine(paths.LocalFinalDirectory, "DEPT.DB"),
                Path.Combine(paths.LocalFinalDirectory, "FINE.DB"),
                summaryPath,
                Path.Combine(paths.LocalFinalDirectory, "ITEM.DB"),
                skusDbPath,
                Path.Combine(paths.LocalFinalDirectory, "VEND.DB")
            };

            CopyBinaryFile(classInput, localDbFiles[0]);
            CopyBinaryFile(departmentInput, localDbFiles[1]);
            CopyBinaryFile(fineInput, localDbFiles[2]);
            WritePlaceholder(summaryPath, BuildPlaceholderContents(job, "Catalog Summary", Path.GetFileName(summaryPath), inputs));
            CopyBinaryFile(itemText, localDbFiles[4]);
            CopyBinaryFile(itemInput, localDbFiles[5]);
            CopyBinaryFile(vendText, localDbFiles[6]);

            var exportFiles = new[] { itemText, deptText, clasText, fineText, vendText };
            var zipPath = Path.Combine(paths.LocalFinalDirectory, BuildCatalogZipFileName(job.JobID));
            CreateZipFromFiles(zipPath, exportFiles, paths.LocalWorkDirectory);

            // The individual TXT exports stay inside the inner job zip.
            // The archive directory receives the DB artifacts plus the inner zip, matching the Paradox archive package shape.

            foreach (var dbFile in localDbFiles)
            {
                CopyFile(dbFile, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(dbFile)));
            }

            CopyFile(summaryPath, Path.Combine(paths.RemoteRawDirectory, Path.GetFileName(summaryPath)));
            CopyFile(zipPath, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(zipPath)));

            prepared = new PreparedJobFiles
            {
                InputFileCount = CountMatches(inputs),
                OutputFileCount = localDbFiles.Length + exportFiles.Length + 1,
                Summary = "HHH catalog files validated and generated from the staged House-Hasson CAT input pack."
            };

            return OperationResult.Ok("Ready", prepared.Summary);
        }

        private OperationResult PrepareInhouse(JobRecord job, WholesalerConfig wholesaler, GeneralConfig config, out PreparedJobFiles prepared)
        {
            var paths = BuildPaths(job, config);
            var profile = BuildInhouseProfile(job, paths);
            var validation = ValidateInputs(profile);
            if (validation != null)
            {
                prepared = null;
                return validation;
            }

            EnsureDirectories(
                paths.RemoteRawDirectory,
                paths.RemoteArchiveDirectory,
                paths.LocalWorkDirectory,
                paths.LocalFinalDirectory,
                paths.InhouseCatalogDirectory,
                paths.RoverDirectory);
            StageMatchedInputs(profile, paths.LocalWorkDirectory);

            var itemInput = FindMatchedInputPath(profile, "InHouse item input");
            var upcInput = FindMatchedInputPath(profile, "InHouse UPC input");
            var vendorInput = FindMatchedInputPath(profile, "InHouse vendor input");
            var longDescriptionInput = FindMatchedInputPath(profile, "InHouse long-description input");
            var departmentInput = FindMatchedInputPath(profile, "InHouse department input");
            var classInput = FindMatchedInputPath(profile, "InHouse class input");
            var finelineInput = FindMatchedInputPath(profile, "InHouse fineline input");

            var localFiles = new List<string>
            {
                Path.Combine(paths.LocalFinalDirectory, "ITEM_RAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "UPC_RAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "VEND_RAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "ITEMEXT.DB"),
                Path.Combine(paths.LocalFinalDirectory, "DERAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "CLRAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, "FIRAW.DB"),
                Path.Combine(paths.LocalFinalDirectory, job.WhlCode + "_I.DB"),
                Path.Combine(paths.LocalFinalDirectory, job.WhlCode + "_C.DB")
            };

            CopySourceOrPlaceholder(itemInput, localFiles[0], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(upcInput, localFiles[1], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(vendorInput, localFiles[2], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(longDescriptionInput, localFiles[3], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(departmentInput, localFiles[4], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(classInput, localFiles[5], job, "InHouse Data", profile);
            CopySourceOrPlaceholder(finelineInput, localFiles[6], job, "InHouse Data", profile);
            CopySeedTemplateOrPlaceholder("ITEM_OUT.DB", localFiles[7], job, "InHouse Catalog Template", profile);
            WritePlaceholder(localFiles[8], BuildPlaceholderContents(job, "InHouse Data", Path.GetFileName(localFiles[8]), profile));

            var summaryPath = Path.Combine(paths.InhouseCatalogDirectory, job.JobID + ".DB");
            var catalogTextPath = Path.Combine(paths.InhouseCatalogDirectory, job.WhlCode + "_C.TXT");
            var roverPath = Path.Combine(paths.RoverDirectory, job.WhlCode + "_ROVER.DB");

            WritePlaceholder(summaryPath, BuildPlaceholderContents(job, "InHouse Summary", Path.GetFileName(summaryPath), profile));
            WritePlaceholder(catalogTextPath, BuildPlaceholderContents(job, "InHouse Export", Path.GetFileName(catalogTextPath), profile));
            WritePlaceholder(roverPath, BuildPlaceholderContents(job, "Rover Export", Path.GetFileName(roverPath), profile));

            CopyFile(summaryPath, Path.Combine(paths.RemoteArchiveDirectory, Path.GetFileName(summaryPath)));

            prepared = new PreparedJobFiles
            {
                InputFileCount = CountMatches(profile),
                OutputFileCount = localFiles.Count + 3,
                Summary = "InHouse catalog files validated and created with the original wholesaler-specific file names."
            };

            return OperationResult.Ok("Ready", prepared.Summary);
        }


        private IList<InputRule> BuildCatalogProfile(JobRecord job, JobPaths paths)
        {
            var rules = new List<InputRule>();
            var whlCode = (job.WhlCode ?? string.Empty).ToUpperInvariant();

            if (whlCode == "HHH")
            {
                rules.Add(CreateRule("HHH item input", paths.RemoteRawDirectory, 1, 1, "HHH_I.DAT", "HHH_i.dat"));
                rules.Add(CreateRule("HHH long-description input", paths.RemoteRawDirectory, 1, 1, "HHH_LDESC.DAT", "HHH_ldesc.dat"));
                rules.Add(CreateRule("HHH department input", paths.RemoteRawDirectory, 1, 1, "HHH_DE.DAT", "HHH_de.dat"));
                rules.Add(CreateRule("HHH class input", paths.RemoteRawDirectory, 1, 1, "HHH_CL.DAT", "HHH_cl.dat"));
                rules.Add(CreateRule("HHH fineline input", paths.RemoteRawDirectory, 1, 1, "HHH_FI.DAT", "HHH_fi.dat"));
                rules.Add(CreateRule("HHH vendor input", paths.RemoteReferenceDirectory, 1, 1, "Ltvm.db", "LTVM.DB"));
                return rules;
            }

            if (whlCode == "EJD")
            {
                rules.Add(CreateRule("EJD item input", paths.RemoteRawDirectory, 1, 1, "EJD_I.DAT", "ejd_i.dat"));
                rules.Add(CreateRule("EJD long-description input", paths.RemoteRawDirectory, 1, 1, "EJD_LDESC.DAT", "ejd_ldesc.dat", "ITEMEXT.DAT", "itemext.dat"));
                rules.Add(CreateRule("EJD department input", paths.RemoteRawDirectory, 1, 1, "EJD_DE.DAT", "ejd_de.dat"));
                rules.Add(CreateRule("EJD class input", paths.RemoteRawDirectory, 1, 1, "EJD_CL.DAT", "ejd_cl.dat"));
                rules.Add(CreateRule("EJD vendor input", paths.RemoteReferenceDirectory, 0, 1, "Ltvm.db", "LTVM.DB"));
                return rules;
            }

            rules.Add(CreateRule("Catalog item raw input", paths.RemoteRawDirectory, 1, 1, whlCode + "_I.272", whlCode + "_I.DAT"));
            rules.Add(CreateRule("Catalog UPC raw input", paths.RemoteRawDirectory, 0, 1, whlCode + "_U.052", whlCode + "_U.DAT"));
            rules.Add(CreateRule("Catalog long-description input", paths.RemoteRawDirectory, 0, 1, "ITEMEXT.DAT"));
            return rules;
        }

        private static string BuildCatalogZipFileName(string jobId)
        {
            var normalized = (jobId ?? string.Empty).Trim();
            if (normalized.Length == 0)
            {
                normalized = "catalog";
            }

            return normalized.ToLowerInvariant() + ".zip";
        }

        private static string BuildCatalogSkuDbFileName(JobRecord job)
        {
            var month = (job == null ? string.Empty : (job.CatMonth ?? string.Empty)).Trim().ToUpperInvariant();
            if (month.Length == 0 && job != null && !string.IsNullOrWhiteSpace(job.JobID) && job.JobID.Length >= 3)
            {
                month = job.JobID.Substring(job.JobID.Length - 3).ToUpperInvariant();
            }

            if (month.Length == 0)
            {
                month = "CAT";
            }

            return month + "SKUS.DB";
        }

        private static InputRule CreateRule(string description, string directoryPath, int minimumMatches, int maximumMatches, params string[] candidates)
        {
            var rule = new InputRule();
            rule.Description = description;
            rule.DirectoryPath = directoryPath;
            rule.MinimumMatches = minimumMatches;
            rule.MaximumMatches = maximumMatches;

            foreach (var candidate in candidates.Where(delegate (string item) { return !string.IsNullOrWhiteSpace(item) && !item.Equals("N/A", StringComparison.OrdinalIgnoreCase); }))
            {
                rule.CandidateFileNames.Add(candidate);
            }

            return rule;
        }

        private OperationResult ValidateInputs(IList<InputRule> rules)
        {
            var errors = new List<string>();

            foreach (var rule in rules)
            {
                rule.MatchedFileNames.Clear();
                foreach (var candidate in rule.CandidateFileNames)
                {
                    var fullPath = Path.Combine(rule.DirectoryPath, candidate);
                    if (File.Exists(fullPath))
                    {
                        rule.MatchedFileNames.Add(candidate);
                    }
                }

                var hasTooFew = rule.MatchedFileNames.Count < rule.MinimumMatches;
                var requiresExactMatchCount = rule.MinimumMatches > 1 && rule.MinimumMatches == rule.MaximumMatches;
                var hasTooMany = requiresExactMatchCount && rule.MatchedFileNames.Count > rule.MaximumMatches;

                if (hasTooFew || hasTooMany)
                {
                    errors.Add(rule.Description + ": expected " + DescribeCandidates(rule) + " in " + rule.DirectoryPath + ".");
                }
            }

            if (!errors.Any())
            {
                return null;
            }

            return OperationResult.Fail("Missing Files", string.Join(Environment.NewLine, errors.ToArray()));
        }

        private JobPaths BuildPaths(JobRecord job, GeneralConfig config)
        {
            var paths = new JobPaths();
            var normalizedJobId = (job.JobID ?? string.Empty).Trim();
            var normalizedCode = (job.WhlCode ?? string.Empty).Trim().ToUpperInvariant();
            var catalogYymm = FigureCatalogYymm(job.CatMonth);

            paths.RemoteRawDirectory = CombinePath(config.RemoteRaw, normalizedJobId);
            paths.RemoteArchiveDirectory = CombinePath(config.RemoteArchive, normalizedJobId);
            paths.RemoteReferenceDirectory = CombinePath(config.Reference, normalizedCode);
            paths.RemoteDaprdDirectory = CombinePath(config.DAPrd, "IT_DATA", normalizedCode);
            paths.LocalFinalDirectory = CombinePath(config.LocalWork, normalizedJobId);
            paths.LocalWorkDirectory = BuildLocalWorkDirectory(job, config.LocalWork);
            paths.InhouseCatalogDirectory = CombinePath(config.CDACatalog, "INDDATA");
            paths.RoverDirectory = CombinePath(config.CDACatalog, "Indpendent", "Ind_" + catalogYymm + "rvr");

            return paths;
        }

        private IList<InputRule> BuildInhouseProfile(JobRecord job, JobPaths paths)
        {
            var yymm = FigureCatalogYymm(job.CatMonth);
            var rules = new List<InputRule>();
            var whlCode = (job.WhlCode ?? string.Empty).ToUpperInvariant();

            rules.Add(CreateRule("InHouse item input", paths.RemoteRawDirectory, 1, 1, whlCode + "_I.272", whlCode + "_I.250", whlCode + "_I.329", whlCode + "_I.DAT"));

            switch (whlCode)
            {
                case "COT":
                    rules.Add(CreateRule("InHouse vendor input", paths.RemoteArchiveDirectory, 0, 1, "VEND" + yymm + ".TXT"));
                    rules.Add(CreateRule("InHouse long-description input", paths.RemoteRawDirectory, 1, 1, "SUPLXREF.DAT"));
                    rules.Add(CreateRule("InHouse department input", paths.RemoteArchiveDirectory, 1, 1, "DEPT" + yymm + ".TXT"));
                    rules.Add(CreateRule("InHouse class input", paths.RemoteArchiveDirectory, 1, 1, "CLAS" + yymm + ".TXT"));
                    rules.Add(CreateRule("InHouse fineline input", paths.RemoteArchiveDirectory, 1, 1, "FINE" + yymm + ".TXT"));
                    break;

                case "HWI":
                case "DIB":
                    rules.Add(CreateRule("InHouse vendor input", paths.RemoteReferenceDirectory, 0, 1, whlCode + "_V.DAT"));
                    rules.Add(CreateRule("InHouse long-description input", paths.RemoteRawDirectory, 1, 1, "ITEMEXT.DAT"));
                    rules.Add(CreateRule("InHouse department input", paths.RemoteArchiveDirectory, 1, 1, "LTDE.TXT"));
                    rules.Add(CreateRule("InHouse class input", paths.RemoteArchiveDirectory, 1, 1, "LTCL.TXT"));
                    rules.Add(CreateRule("InHouse fineline input", paths.RemoteArchiveDirectory, 1, 1, "LTFI.TXT"));
                    break;

                default:
                    rules.Add(CreateRule("InHouse UPC input", paths.RemoteRawDirectory, 1, 1, whlCode + "_U.800", whlCode + "_U.052", whlCode + "_U.DAT"));
                    rules.Add(CreateRule("InHouse vendor input", paths.RemoteRawDirectory, 1, 1, whlCode + "_VM.315", whlCode + "_VM.286", whlCode + "_VM.DAT"));
                    rules.Add(CreateRule("InHouse long-description input", paths.RemoteRawDirectory, 1, 1, "ITEMEXT.DAT"));
                    rules.Add(CreateRule("InHouse department input", paths.RemoteArchiveDirectory, 1, 1, "LTDE.TXT"));
                    rules.Add(CreateRule("InHouse class input", paths.RemoteArchiveDirectory, 1, 1, "LTCL.TXT"));
                    rules.Add(CreateRule("InHouse fineline input", paths.RemoteArchiveDirectory, 1, 1, "LTFI.TXT"));
                    break;
            }

            return rules;
        }


        private static string FindMatchedInputPath(IEnumerable<InputRule> inputs, string description)
        {
            var rule = inputs.FirstOrDefault(delegate (InputRule item)
            {
                return item.Description.Equals(description, StringComparison.OrdinalIgnoreCase);
            });

            if (rule == null || rule.MatchedFileNames.Count == 0)
            {
                return string.Empty;
            }

            return Path.Combine(rule.DirectoryPath, rule.MatchedFileNames[0]);
        }

        private static void CopyTextPreservingRows(string sourcePath, string destinationPath)
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var reader = new StreamReader(sourcePath, System.Text.Encoding.Default, true))
            using (var writer = new StreamWriter(destinationPath, false, System.Text.Encoding.Default))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.WriteLine(line.TrimEnd());
                }
            }
        }

        private static void CopyBinaryFile(string sourcePath, string destinationPath)
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, destinationPath, true);
        }

        private static IEnumerable<string> BuildEfmCandidateNames(DateTime fromDate, DateTime toDate)
        {
            var candidates = new List<string>();
            var start = fromDate.Date.AddDays(-14);
            var finish = toDate.Date;

            while (start <= finish)
            {
                candidates.Add("LD" + start.ToString("yyMMdd") + ".DAT");
                start = start.AddDays(1);
            }

            return candidates;
        }

        private static string StageVendorInputAsLtvm(JobPaths paths, string vendorInputPath)
        {
            if (string.IsNullOrWhiteSpace(vendorInputPath) || !File.Exists(vendorInputPath))
            {
                return string.Empty;
            }

            var localLtvmPath = Path.Combine(paths.LocalWorkDirectory, "Ltvm.db");
            CopyBinaryFile(vendorInputPath, localLtvmPath);
            return localLtvmPath;
        }

        private static int CountMatches(IEnumerable<InputRule> rules)
        {
            return rules.Sum(delegate (InputRule item) { return item.MatchedFileNames.Count; });
        }

        private static void StageMatchedInputs(IEnumerable<InputRule> rules, string workingDirectory)
        {
            foreach (var rule in rules)
            {
                foreach (var matchedFileName in rule.MatchedFileNames)
                {
                    var sourcePath = Path.Combine(rule.DirectoryPath, matchedFileName);
                    var destinationPath = Path.Combine(workingDirectory, matchedFileName);
                    CopyBinaryFile(sourcePath, destinationPath);
                }
            }
        }

        private static string DescribeCandidates(InputRule rule)
        {
            if (rule.MinimumMatches > 1 && rule.MinimumMatches == rule.MaximumMatches)
            {
                return "exactly " + rule.MinimumMatches + " files matching " + string.Join(", ", rule.CandidateFileNames.ToArray());
            }

            return string.Join(" or ", rule.CandidateFileNames.ToArray());
        }

        private void CreateZipFromFiles(string zipPath, IEnumerable<string> sourceFiles, string workingDirectory)
        {
            var stageDirectory = Path.Combine(workingDirectory, "ZIP_STAGE");
            if (Directory.Exists(stageDirectory))
            {
                Directory.Delete(stageDirectory, true);
            }

            Directory.CreateDirectory(stageDirectory);
            foreach (var sourceFile in sourceFiles)
            {
                CopyFile(sourceFile, Path.Combine(stageDirectory, Path.GetFileName(sourceFile)));
            }

            _dataStore.CreateOrReplaceZip(zipPath, new[]
            {
                new AppDataStore.ZipDirectoryEntry(stageDirectory, string.Empty)
            });

            Directory.Delete(stageDirectory, true);
        }

        private static void EnsureDirectories(params string[] directories)
        {
            foreach (var directory in directories.Where(delegate (string item) { return !string.IsNullOrWhiteSpace(item); }).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(directory);
            }
        }

        private static void WritePlaceholder(string path, string contents)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, contents);
        }

        private static void CopyFile(string sourcePath, string destinationPath)
        {
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(sourcePath, destinationPath, true);
        }

        private static string CopyGeneratedExport(string directoryPath, string generatedName, string targetName)
        {
            var sourcePath = Path.Combine(directoryPath, generatedName);
            var destinationPath = Path.Combine(directoryPath, targetName);
            if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(sourcePath, destinationPath, StringComparison.Ordinal))
                {
                    return destinationPath;
                }

                var tempPath = Path.Combine(directoryPath, Guid.NewGuid().ToString("N") + ".tmp");
                CopyBinaryFile(sourcePath, tempPath);
                File.Delete(sourcePath);
                File.Move(tempPath, destinationPath);
                return destinationPath;
            }

            CopyBinaryFile(sourcePath, destinationPath);
            return destinationPath;
        }

        private static void CopySourceOrPlaceholder(string sourcePath, string destinationPath, JobRecord job, string artifactType, IEnumerable<InputRule> inputs)
        {
            if (!string.IsNullOrWhiteSpace(sourcePath) && File.Exists(sourcePath))
            {
                CopyBinaryFile(sourcePath, destinationPath);
                return;
            }

            WritePlaceholder(destinationPath, BuildPlaceholderContents(job, artifactType, Path.GetFileName(destinationPath), inputs));
        }

        private static void CopySeedTemplateOrPlaceholder(string templateName, string destinationPath, JobRecord job, string artifactType, IEnumerable<InputRule> inputs)
        {
            var templatePath = FindSeedTemplate(templateName);
            if (!string.IsNullOrWhiteSpace(templatePath))
            {
                CopyBinaryFile(templatePath, destinationPath);
                return;
            }

            WritePlaceholder(destinationPath, BuildPlaceholderContents(job, artifactType, Path.GetFileName(destinationPath), inputs));
        }

        private static string BuildPlaceholderContents(JobRecord job, string artifactType, string fileName, IEnumerable<InputRule> inputs)
        {
            var lines = new List<string>();
            lines.Add("ArtifactType=" + artifactType);
            lines.Add("FileName=" + fileName);
            lines.Add("JobType=" + job.JobType);
            lines.Add("JobID=" + job.JobID);
            lines.Add("WhlCode=" + job.WhlCode);
            lines.Add("WhlDescription=" + job.WhlDescription);
            lines.Add("CatMonth=" + job.CatMonth);
            lines.Add("GeneratedOn=" + DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));

            foreach (var input in inputs)
            {
                lines.Add(input.Description + "=" + string.Join(";", input.MatchedFileNames.ToArray()));
            }

            return string.Join(Environment.NewLine, lines.ToArray()) + Environment.NewLine;
        }

        private static string FindSeedTemplate(string templateName)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDirectory, "Data", "ParadoxTemplates", templateName),
                Path.Combine(baseDirectory, "Data", "Seeds", "ParadoxTemplates", templateName)
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static string BuildLocalWorkDirectory(JobRecord job, string configuredRoot)
        {
            var normalizedRoot = NormalizeDirectory(configuredRoot);
            var normalizedJobId = (job.JobID ?? string.Empty).Trim();
            var normalizedCode = (job.WhlCode ?? string.Empty).Trim().ToUpperInvariant();
            var month = (job.CatMonth ?? string.Empty).Trim().ToUpperInvariant();

            if ((job.JobType ?? string.Empty).Equals("INH", StringComparison.OrdinalIgnoreCase))
            {
                var code = normalizedCode.Length > 4 ? normalizedCode.Substring(0, 4) : normalizedCode;
                return Path.Combine(normalizedRoot, code + month + "W");
            }

            if (normalizedJobId.Length > 7)
            {
                return Path.Combine(normalizedRoot, normalizedJobId.Substring(0, 4) + normalizedJobId.Substring(5, 3) + "W");
            }

            return Path.Combine(normalizedRoot, normalizedJobId + "W");
        }

        private static string FigureCatalogYymm(string month)
        {
            var buildDate = DateTime.Today;
            var catalogMonth = Array.IndexOf(AppConstants.CatalogMonths, (month ?? string.Empty).ToUpperInvariant()) + 1;
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

        private static string CombinePath(string basePath, params string[] segments)
        {
            var path = NormalizeDirectory(basePath);
            foreach (var segment in segments.Where(delegate (string item) { return !string.IsNullOrWhiteSpace(item); }))
            {
                path = Path.Combine(path, segment);
            }

            return path;
        }

        private static string NormalizeDirectory(string configuredPath)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedFiles");
            }

            return configuredPath.Replace('\\', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }
    }
}
