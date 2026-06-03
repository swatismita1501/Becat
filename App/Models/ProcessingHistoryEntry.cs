using System;

namespace EcatDesktop.Models
{
    public sealed class ProcessingHistoryEntry
    {
        public ProcessingHistoryEntry()
        {
            Process = string.Empty;
            CatMonth = string.Empty;
            TimeElapsed = string.Empty;
        }

        public string Process { get; set; }

        public DateTime DateProcessed { get; set; }

        public string CatMonth { get; set; }

        public DateTime TimeStarted { get; set; }

        public DateTime TimeComplete { get; set; }

        public string TimeElapsed { get; set; }

        public int InputRecords { get; set; }

        public int OutputRecords { get; set; }

        public int RecordsPerMinute { get; set; }
    }
}
