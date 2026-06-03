using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EcatDesktop.Common;
using EcatDesktop.Models;

namespace EcatDesktop.Services
{
    public sealed class AppDataStore
    {
        public sealed class ZipDirectoryEntry
        {
            public ZipDirectoryEntry(string baseDirectory, string entryPrefix)
            {
                BaseDirectory = baseDirectory;
                EntryPrefix = entryPrefix;
            }

            public string BaseDirectory { get; private set; }

            public string EntryPrefix { get; private set; }
        }

        private readonly string _rootDirectory;
        private readonly string _dataDirectory;
        private readonly string _seedDirectory;

        public AppDataStore()
        {
            try
            {
                _rootDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _dataDirectory = Path.Combine(_rootDirectory, "Data");
                _seedDirectory = Path.Combine(_dataDirectory, "Seeds");

                Directory.CreateDirectory(_dataDirectory);
                EnsureSeededFile("queue.csv", "queue.seed.csv");
                EnsureSeededFile("done.csv", "done.seed.csv");
                EnsureSeededFile("wholesalers.csv", "wholesalers.seed.csv");
                MergeMissingWholesalersFromSeed();
                EnsureSeededFile("general-config.csv", "general-config.seed.csv");
                EnsureSeededFile("archive-config.csv", "archive-config.seed.csv");
                EnsureSeededFile("archive-manifest.csv", "archive-manifest.seed.csv");
                EnsureSeededFile("history.csv", "history.seed.csv");
                Log.Information("Data store initialized at " + _dataDirectory + ".");
            }
            catch (Exception oException)
            {
                Log.LogError(oException, "AppDataStore initialization failed.");
                throw;
            }
        }

        public string DataDirectory
        {
            get { return _dataDirectory; }
        }

        public string ArchiveOutputDirectory
        {
            get
            {
                var path = Path.Combine(_rootDirectory, "GeneratedArchive");
                Directory.CreateDirectory(path);
                return path;
            }
        }

        public IList<JobRecord> LoadQueue()
        {
            return ReadCsv(Path.Combine(_dataDirectory, "queue.csv"), MapJobRecord).ToList();
        }

        public void SaveQueue(IEnumerable<JobRecord> records)
        {
            WriteCsv(
                Path.Combine(_dataDirectory, "queue.csv"),
                "Select,JobType,JobID,WhlCode,WhlDescription,FromDate,ToDate,EFMID,CatMonth,Method1,Method2,Method3,Method4,Method5,Method6,Method7,Method8,Method9",
                records.Select(
                    delegate(JobRecord job)
                    {
                        return string.Join(",",
                            CsvUtility.Escape(job.Select),
                            CsvUtility.Escape(job.JobType),
                            CsvUtility.Escape(job.JobID),
                            CsvUtility.Escape(job.WhlCode),
                            CsvUtility.Escape(job.WhlDescription),
                            CsvUtility.Escape(CsvUtility.DateOrEmpty(job.FromDate)),
                            CsvUtility.Escape(CsvUtility.DateOrEmpty(job.ToDate)),
                            CsvUtility.Escape(job.EFMID),
                            CsvUtility.Escape(job.CatMonth),
                            CsvUtility.Escape(job.Method1),
                            CsvUtility.Escape(job.Method2),
                            CsvUtility.Escape(job.Method3),
                            CsvUtility.Escape(job.Method4),
                            CsvUtility.Escape(job.Method5),
                            CsvUtility.Escape(job.Method6),
                            CsvUtility.Escape(job.Method7),
                            CsvUtility.Escape(job.Method8),
                            CsvUtility.Escape(job.Method9));
                    }));
        }

        public IList<JobRecord> LoadDoneQueue()
        {
            return ReadCsv(Path.Combine(_dataDirectory, "done.csv"), MapJobRecord).ToList();
        }

        public void SaveDoneQueue(IEnumerable<JobRecord> records)
        {
            WriteCsv(
                Path.Combine(_dataDirectory, "done.csv"),
                "Select,JobType,JobID,WhlCode,WhlDescription,FromDate,ToDate,EFMID,CatMonth,Method1,Method2,Method3,Method4,Method5,Method6,Method7,Method8,Method9",
                records.Select(
                    delegate(JobRecord job)
                    {
                        return string.Join(",",
                            CsvUtility.Escape(job.Select),
                            CsvUtility.Escape(job.JobType),
                            CsvUtility.Escape(job.JobID),
                            CsvUtility.Escape(job.WhlCode),
                            CsvUtility.Escape(job.WhlDescription),
                            CsvUtility.Escape(CsvUtility.DateOrEmpty(job.FromDate)),
                            CsvUtility.Escape(CsvUtility.DateOrEmpty(job.ToDate)),
                            CsvUtility.Escape(job.EFMID),
                            CsvUtility.Escape(job.CatMonth),
                            CsvUtility.Escape(job.Method1),
                            CsvUtility.Escape(job.Method2),
                            CsvUtility.Escape(job.Method3),
                            CsvUtility.Escape(job.Method4),
                            CsvUtility.Escape(job.Method5),
                            CsvUtility.Escape(job.Method6),
                            CsvUtility.Escape(job.Method7),
                            CsvUtility.Escape(job.Method8),
                            CsvUtility.Escape(job.Method9));
                    }));
        }

        public IList<WholesalerConfig> LoadWholesalers()
        {
            return ReadCsv(
                Path.Combine(_dataDirectory, "wholesalers.csv"),
                delegate(string[] row)
                {
                    int nextId;
                    return new WholesalerConfig
                    {
                        WhlCode = GetValue(row, 0),
                        WhlDescription = GetValue(row, 1),
                        NextEFMId = int.TryParse(GetValue(row, 2), out nextId) ? nextId : 1,
                        NextEFMFromDate = CsvUtility.ParseDate(GetValue(row, 3)),
                        Debug = GetValue(row, 4),
                        EFMPrefix = GetValue(row, 5)
                    };
                }).ToList();
        }

        public void SaveWholesalers(IEnumerable<WholesalerConfig> records)
        {
            WriteCsv(
                Path.Combine(_dataDirectory, "wholesalers.csv"),
                "WhlCode,WhlDescription,NextEFMId,NextEFMFromDate,Debug,EFMPrefix",
                records.Select(
                    delegate(WholesalerConfig record)
                    {
                        return string.Join(",",
                            CsvUtility.Escape(record.WhlCode),
                            CsvUtility.Escape(record.WhlDescription),
                            CsvUtility.Escape(record.NextEFMId.ToString()),
                            CsvUtility.Escape(CsvUtility.DateOrEmpty(record.NextEFMFromDate)),
                            CsvUtility.Escape(record.Debug),
                            CsvUtility.Escape(record.EFMPrefix));
                    }));
        }

        public GeneralConfig LoadGeneralConfig()
        {
            var config = ReadCsv(
                Path.Combine(_dataDirectory, "general-config.csv"),
                delegate(string[] row)
                {
                    return new GeneralConfig
                    {
                        DAPrd = GetValue(row, 0),
                        RemoteRaw = GetValue(row, 1),
                        LocalWork = GetValue(row, 2),
                        Reference = GetValue(row, 3),
                        RemoteArchive = GetValue(row, 4),
                        ZipArchive = GetValue(row, 5),
                        CDADir = GetValue(row, 6),
                        CDATemp = GetValue(row, 7),
                        CDACatalog = GetValue(row, 8),
                        CallCDABat = GetValue(row, 9)
                    };
                }).FirstOrDefault();

            return config ?? new GeneralConfig();
        }

        public ArchiveConfig LoadArchiveConfig()
        {
            var config = ReadCsv(
                Path.Combine(_dataDirectory, "archive-config.csv"),
                delegate(string[] row)
                {
                    return new ArchiveConfig
                    {
                        RemoteRaw = GetValue(row, 0),
                        RemoteArchive = GetValue(row, 1),
                        ZipArchive = GetValue(row, 2)
                    };
                }).FirstOrDefault();

            return config ?? new ArchiveConfig();
        }

        public void SaveArchiveConfig(ArchiveConfig config)
        {
            WriteCsv(
                Path.Combine(_dataDirectory, "archive-config.csv"),
                "RemoteRaw,RemoteArchive,ZipArchive",
                new[]
                {
                    string.Join(",",
                        CsvUtility.Escape(config.RemoteRaw),
                        CsvUtility.Escape(config.RemoteArchive),
                        CsvUtility.Escape(config.ZipArchive))
                });
        }

        public IList<ArchiveManifestEntry> LoadArchiveManifest()
        {
            return ReadCsv(
                Path.Combine(_dataDirectory, "archive-manifest.csv"),
                delegate(string[] row)
                {
                    return new ArchiveManifestEntry
                    {
                        Directory = GetValue(row, 0),
                        ZipFile = GetValue(row, 1)
                    };
                }).ToList();
        }

        public void SaveArchiveManifest(IEnumerable<ArchiveManifestEntry> manifest)
        {
            WriteCsv(
                Path.Combine(_dataDirectory, "archive-manifest.csv"),
                "Directory,ZipFile",
                manifest.Select(
                    delegate(ArchiveManifestEntry entry)
                    {
                        return string.Join(",",
                            CsvUtility.Escape(entry.Directory),
                            CsvUtility.Escape(entry.ZipFile));
                    }));
        }

        public IList<ProcessingHistoryEntry> LoadHistory()
        {
            return ReadCsv(
                Path.Combine(_dataDirectory, "history.csv"),
                delegate(string[] row)
                {
                    int inCount;
                    int outCount;
                    int rpm;
                    DateTime started;
                    DateTime complete;

                    return new ProcessingHistoryEntry
                    {
                        Process = GetValue(row, 0),
                        DateProcessed = CsvUtility.ParseDate(GetValue(row, 1)) ?? DateTime.Today,
                        CatMonth = GetValue(row, 2),
                        TimeStarted = DateTime.TryParse(GetValue(row, 3), out started) ? started : DateTime.Now,
                        TimeComplete = DateTime.TryParse(GetValue(row, 4), out complete) ? complete : DateTime.Now,
                        TimeElapsed = GetValue(row, 5),
                        InputRecords = int.TryParse(GetValue(row, 6), out inCount) ? inCount : 0,
                        OutputRecords = int.TryParse(GetValue(row, 7), out outCount) ? outCount : 0,
                        RecordsPerMinute = int.TryParse(GetValue(row, 8), out rpm) ? rpm : 0
                    };
                }).ToList();
        }

        public void AppendHistory(ProcessingHistoryEntry entry)
        {
            var history = LoadHistory().ToList();
            history.Add(entry);
            WriteCsv(
                Path.Combine(_dataDirectory, "history.csv"),
                "Process,DateProcessed,CatMonth,TimeStarted,TimeComplete,TimeElapsed,InputRecords,OutputRecords,RecordsPerMinute",
                history.Select(
                    delegate(ProcessingHistoryEntry item)
                    {
                        return string.Join(",",
                            CsvUtility.Escape(item.Process),
                            CsvUtility.Escape(item.DateProcessed.ToString("MM/dd/yyyy")),
                            CsvUtility.Escape(item.CatMonth),
                            CsvUtility.Escape(item.TimeStarted.ToString("MM/dd/yyyy HH:mm:ss")),
                            CsvUtility.Escape(item.TimeComplete.ToString("MM/dd/yyyy HH:mm:ss")),
                            CsvUtility.Escape(item.TimeElapsed),
                            CsvUtility.Escape(item.InputRecords.ToString()),
                            CsvUtility.Escape(item.OutputRecords.ToString()),
                            CsvUtility.Escape(item.RecordsPerMinute.ToString()));
                    }));
        }

        public void CreateOrReplaceZip(string zipFilePath, IEnumerable<ZipDirectoryEntry> directories)
        {
            var parent = Path.GetDirectoryName(zipFilePath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            var entries = new List<SimpleZipEntry>();
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory.BaseDirectory))
                {
                    continue;
                }

                foreach (var file in Directory.GetFiles(directory.BaseDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativePath = GetRelativePath(directory.BaseDirectory, file).Replace('\\', '/');
                    var entryName = string.IsNullOrWhiteSpace(directory.EntryPrefix)
                        ? relativePath
                        : directory.EntryPrefix.TrimEnd('/') + "/" + relativePath;

                    entries.Add(new SimpleZipEntry(file, entryName));
                }
            }

            WriteStoredZip(zipFilePath, entries);
        }

        private sealed class SimpleZipEntry
        {
            public SimpleZipEntry(string sourcePath, string entryName)
            {
                SourcePath = sourcePath;
                EntryName = entryName.Replace('\\', '/');
            }

            public string SourcePath { get; private set; }

            public string EntryName { get; private set; }
        }

        private static void WriteStoredZip(string zipFilePath, IList<SimpleZipEntry> entries)
        {
            var centralDirectory = new List<byte[]>();
            using (var output = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                foreach (var entry in entries)
                {
                    var data = File.ReadAllBytes(entry.SourcePath);
                    var nameBytes = Encoding.ASCII.GetBytes(entry.EntryName);
                    var crc = CalculateCrc32(data);
                    var localHeaderOffset = output.Position;
                    var modified = File.GetLastWriteTime(entry.SourcePath);
                    ushort dosTime;
                    ushort dosDate;
                    GetDosDateTime(modified, out dosTime, out dosDate);

                    WriteUInt32(output, 0x04034b50);
                    WriteUInt16(output, 20);
                    WriteUInt16(output, 0);
                    WriteUInt16(output, 0);
                    WriteUInt16(output, dosTime);
                    WriteUInt16(output, dosDate);
                    WriteUInt32(output, crc);
                    WriteUInt32(output, (uint)data.Length);
                    WriteUInt32(output, (uint)data.Length);
                    WriteUInt16(output, (ushort)nameBytes.Length);
                    WriteUInt16(output, 0);
                    output.Write(nameBytes, 0, nameBytes.Length);
                    output.Write(data, 0, data.Length);

                    using (var central = new MemoryStream())
                    {
                        WriteUInt32(central, 0x02014b50);
                        WriteUInt16(central, 20);
                        WriteUInt16(central, 20);
                        WriteUInt16(central, 0);
                        WriteUInt16(central, 0);
                        WriteUInt16(central, dosTime);
                        WriteUInt16(central, dosDate);
                        WriteUInt32(central, crc);
                        WriteUInt32(central, (uint)data.Length);
                        WriteUInt32(central, (uint)data.Length);
                        WriteUInt16(central, (ushort)nameBytes.Length);
                        WriteUInt16(central, 0);
                        WriteUInt16(central, 0);
                        WriteUInt16(central, 0);
                        WriteUInt16(central, 0);
                        WriteUInt32(central, 0);
                        WriteUInt32(central, (uint)localHeaderOffset);
                        central.Write(nameBytes, 0, nameBytes.Length);
                        centralDirectory.Add(central.ToArray());
                    }
                }

                var centralDirectoryOffset = output.Position;
                var centralDirectorySize = 0;
                foreach (var central in centralDirectory)
                {
                    output.Write(central, 0, central.Length);
                    centralDirectorySize += central.Length;
                }

                WriteUInt32(output, 0x06054b50);
                WriteUInt16(output, 0);
                WriteUInt16(output, 0);
                WriteUInt16(output, (ushort)centralDirectory.Count);
                WriteUInt16(output, (ushort)centralDirectory.Count);
                WriteUInt32(output, (uint)centralDirectorySize);
                WriteUInt32(output, (uint)centralDirectoryOffset);
                WriteUInt16(output, 0);
            }
        }

        private static uint CalculateCrc32(byte[] data)
        {
            uint crc = 0xffffffff;
            for (var i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xedb88320 : crc >> 1;
                }
            }

            return ~crc;
        }

        private static void GetDosDateTime(DateTime dateTime, out ushort dosTime, out ushort dosDate)
        {
            var year = Math.Max(1980, dateTime.Year);
            dosTime = (ushort)((dateTime.Hour << 11) | (dateTime.Minute << 5) | (dateTime.Second / 2));
            dosDate = (ushort)(((year - 1980) << 9) | (dateTime.Month << 5) | dateTime.Day);
        }

        private static void WriteUInt16(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value & 0xff));
            stream.WriteByte((byte)((value >> 8) & 0xff));
        }

        private static void WriteUInt32(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value & 0xff));
            stream.WriteByte((byte)((value >> 8) & 0xff));
            stream.WriteByte((byte)((value >> 16) & 0xff));
            stream.WriteByte((byte)((value >> 24) & 0xff));
        }

        private static JobRecord MapJobRecord(string[] row)
        {
            return new JobRecord
            {
                Select = GetValue(row, 0),
                JobType = GetValue(row, 1),
                JobID = GetValue(row, 2),
                WhlCode = GetValue(row, 3),
                WhlDescription = GetValue(row, 4),
                FromDate = CsvUtility.ParseDate(GetValue(row, 5)),
                ToDate = CsvUtility.ParseDate(GetValue(row, 6)),
                EFMID = GetValue(row, 7),
                CatMonth = GetValue(row, 8),
                Method1 = GetValue(row, 9, "N"),
                Method2 = GetValue(row, 10, "N"),
                Method3 = GetValue(row, 11, "N"),
                Method4 = GetValue(row, 12, "N"),
                Method5 = GetValue(row, 13, "N"),
                Method6 = GetValue(row, 14, "N"),
                Method7 = GetValue(row, 15, "N"),
                Method8 = GetValue(row, 16, "N"),
                Method9 = GetValue(row, 17, "N")
            };
        }

        private static IEnumerable<T> ReadCsv<T>(string path, Func<string[], T> map)
        {
            if (!File.Exists(path))
            {
                yield break;
            }

            using (var reader = new StreamReader(path))
            {
                reader.ReadLine();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    yield return map(CsvUtility.ParseLine(line));
                }
            }
        }

        private static void WriteCsv(string path, string header, IEnumerable<string> rows)
        {
            var builder = new StringBuilder();
            builder.AppendLine(header);

            foreach (var row in rows)
            {
                builder.AppendLine(row);
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
        }

        private static string GetValue(string[] row, int index)
        {
            return GetValue(row, index, string.Empty);
        }

        private static string GetValue(string[] row, int index, string fallback)
        {
            return index < row.Length ? row[index] : fallback;
        }

        private static string GetRelativePath(string baseDirectory, string fullPath)
        {
            var basePath = baseDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var baseUri = new Uri(basePath, UriKind.Absolute);
            var fullUri = new Uri(fullPath, UriKind.Absolute);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private void EnsureSeededFile(string outputName, string seedName)
        {
            var outputPath = Path.Combine(_dataDirectory, outputName);
            var seedPath = Path.Combine(_seedDirectory, seedName);

            if (!File.Exists(outputPath) && File.Exists(seedPath))
            {
                File.Copy(seedPath, outputPath);
            }
        }

        private void MergeMissingWholesalersFromSeed()
        {
            var runtimePath = Path.Combine(_dataDirectory, "wholesalers.csv");
            var seedPath = Path.Combine(_seedDirectory, "wholesalers.seed.csv");

            if (!File.Exists(runtimePath) || !File.Exists(seedPath))
            {
                return;
            }

            var runtimeRows = ReadWholesalersFromPath(runtimePath).ToList();
            var seedRows = ReadWholesalersFromPath(seedPath).ToList();
            var existingCodes = new HashSet<string>(
                runtimeRows.Select(delegate(WholesalerConfig item) { return item.WhlCode; }),
                StringComparer.OrdinalIgnoreCase);
            var changed = false;

            foreach (var seedRow in seedRows)
            {
                if (string.IsNullOrWhiteSpace(seedRow.WhlCode) || existingCodes.Contains(seedRow.WhlCode))
                {
                    continue;
                }

                runtimeRows.Add(seedRow);
                existingCodes.Add(seedRow.WhlCode);
                changed = true;
            }

            foreach (var runtimeRow in runtimeRows)
            {
                var seedMatch = seedRows.FirstOrDefault(
                    delegate(WholesalerConfig item)
                    {
                        return item.WhlCode.Equals(runtimeRow.WhlCode, StringComparison.OrdinalIgnoreCase);
                    });

                if (seedMatch == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(runtimeRow.EFMPrefix) && !string.IsNullOrWhiteSpace(seedMatch.EFMPrefix))
                {
                    runtimeRow.EFMPrefix = seedMatch.EFMPrefix;
                    changed = true;
                }
            }

            if (changed)
            {
                SaveWholesalers(runtimeRows.OrderBy(delegate(WholesalerConfig item) { return item.WhlCode; }));
            }
        }

        private static IEnumerable<WholesalerConfig> ReadWholesalersFromPath(string path)
        {
            return ReadCsv(
                path,
                delegate(string[] row)
                {
                    int nextId;
                    return new WholesalerConfig
                    {
                        WhlCode = GetValue(row, 0),
                        WhlDescription = GetValue(row, 1),
                        NextEFMId = int.TryParse(GetValue(row, 2), out nextId) ? nextId : 1,
                        NextEFMFromDate = CsvUtility.ParseDate(GetValue(row, 3)),
                        Debug = GetValue(row, 4),
                        EFMPrefix = GetValue(row, 5)
                    };
                });
        }
    }
}
